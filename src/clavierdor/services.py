from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from random import shuffle

from sqlalchemy import select
from sqlalchemy import func

from .data import seed_questions
from .models import AnswerLog, GameSession, Player, Question, RoleType
from .orm import get_session, init_db


@dataclass
class QuestionView:
    id: int
    prompt: str
    choices: dict[str, str]
    correct_choice: str
    hint: str
    stage: int


@dataclass
class SessionState:
    session_id: int
    player_name: str
    role: RoleType
    stage: int
    score: int
    streak: int
    correct_answers: int
    total_answers: int
    accuracy: float
    current_question: QuestionView | None
    completed: bool
    perk_used: bool


class GameService:
    STAGE_MAX = 4
    STAGE_LABELS = {
        1: "Qualification",
        2: "Demi-finale",
        3: "Boss",
        4: "Clavier d'or",
    }

    def __init__(self) -> None:
        init_db()
        seed_questions()
        self._question_cache: list[Question] = []

    def _load_questions(self) -> list[Question]:
        with get_session() as session:
            questions = session.scalars(select(Question)).all()
            shuffle(questions)
            self._question_cache = questions
            return questions

    def _get_question_for_stage(self, stage: int) -> Question | None:
        if not self._question_cache:
            self._load_questions()
        for question in self._question_cache:
            if question.stage == stage:
                return question
        return None

    def _question_view(self, question: Question) -> QuestionView:
        return QuestionView(
            id=question.id,
            prompt=question.prompt,
            choices={
                "A": question.choice_a,
                "B": question.choice_b,
                "C": question.choice_c,
                "D": question.choice_d,
            },
            correct_choice=question.correct_choice,
            hint=question.hint,
            stage=question.stage,
        )

    def start_new_game(self, name: str, role: RoleType) -> SessionState:
        with get_session() as session:
            player = session.scalar(select(Player).where(Player.name == name))
            if player is None:
                player = Player(name=name, role=role)
                session.add(player)
                session.flush()
            else:
                player.role = role
            new_session = GameSession(player=player)
            session.add(new_session)
            session.commit()
            return self._build_state(new_session.id)

    def resume_last_game(self, name: str) -> SessionState | None:
        with get_session() as session:
            player = session.scalar(select(Player).where(Player.name == name))
            if player is None:
                return None
            session_obj = (
                session.query(GameSession)
                .where(GameSession.player_id == player.id)
                .order_by(GameSession.started_at.desc())
                .first()
            )
            if session_obj is None:
                return None
            return self._build_state(session_obj.id)

    def _build_state(self, session_id: int) -> SessionState:
        with get_session() as session:
            session_obj = session.get(GameSession, session_id)
            if session_obj is None:
                raise ValueError("Session introuvable")
            total_answers = session.scalar(
                select(func.count(AnswerLog.id)).where(AnswerLog.session_id == session_id)
            )
            correct_answers = session.scalar(
                select(func.count(AnswerLog.id)).where(
                    AnswerLog.session_id == session_id, AnswerLog.is_correct.is_(True)
                )
            )
            total_answers = total_answers or 0
            correct_answers = correct_answers or 0
            accuracy = (correct_answers / total_answers * 100) if total_answers else 0.0
            question = None
            if not session_obj.completed:
                question_obj = self._get_question_for_stage(session_obj.stage)
                if question_obj:
                    question = self._question_view(question_obj)
            perk_used = self._perk_used(session_obj)
            return SessionState(
                session_id=session_obj.id,
                player_name=session_obj.player.name,
                role=session_obj.player.role,
                stage=session_obj.stage,
                score=session_obj.score,
                streak=session_obj.streak,
                correct_answers=correct_answers,
                total_answers=total_answers,
                accuracy=accuracy,
                current_question=question,
                completed=session_obj.completed,
                perk_used=perk_used,
            )

    def submit_answer(self, session_id: int, question_id: int, selected: str) -> SessionState:
        with get_session() as session:
            session_obj = session.get(GameSession, session_id)
            question = session.get(Question, question_id)
            if session_obj is None or question is None:
                raise ValueError("Session ou question inconnue")
            is_correct = selected == question.correct_choice
            if is_correct:
                session_obj.streak += 1
                score_gain = 10
                if session_obj.streak >= 3:
                    score_gain += 5
                session_obj.score += score_gain
                if session_obj.stage < self.STAGE_MAX:
                    session_obj.stage += 1
                else:
                    session_obj.completed = True
            else:
                session_obj.streak = 0
            session.add(
                AnswerLog(
                    session=session_obj,
                    question_id=question.id,
                    selected=selected,
                    is_correct=is_correct,
                )
            )
            session.commit()
            return self._build_state(session_id)

    def use_front_joker(self, session_id: int, current_question_id: int | None) -> SessionState:
        with get_session() as session:
            session_obj = session.get(GameSession, session_id)
            if session_obj is None:
                raise ValueError("Session inconnue")
            if session_obj.front_joker_used:
                return self._build_state(session_id)
            if not self._question_cache:
                self._load_questions()
            if current_question_id is not None:
                self._question_cache = [
                    question
                    for question in self._question_cache
                    if question.id != current_question_id
                ]
            session_obj.front_joker_used = True
            session.commit()
            return self._build_state(session_id)

    def use_back_joker(self, session_id: int) -> SessionState:
        with get_session() as session:
            session_obj = session.get(GameSession, session_id)
            if session_obj is None:
                raise ValueError("Session inconnue")
            if session_obj.back_joker_used:
                return self._build_state(session_id)
            last_answer = (
                session.query(AnswerLog)
                .where(AnswerLog.session_id == session_id)
                .order_by(AnswerLog.answered_at.desc())
                .first()
            )
            if last_answer and not last_answer.is_correct:
                session_obj.score += 5
                session_obj.streak = 1
            session_obj.back_joker_used = True
            session.commit()
            return self._build_state(session_id)

    def use_mobile_joker(self, session_id: int) -> str:
        with get_session() as session:
            session_obj = session.get(GameSession, session_id)
            if session_obj is None:
                raise ValueError("Session inconnue")
            if session_obj.mobile_joker_used:
                return "Indice déjà utilisé."
            session_obj.mobile_joker_used = True
            question = self._get_question_for_stage(session_obj.stage)
            session.commit()
            if question:
                return question.hint or "Pas d'indice disponible."
            return "Pas d'indice disponible."

    def list_scores(self) -> list[tuple[str, int, datetime]]:
        with get_session() as session:
            results = (
                session.query(Player.name, GameSession.score, GameSession.started_at)
                .join(GameSession)
                .order_by(GameSession.score.desc())
                .all()
            )
            return [(name, score, started_at) for name, score, started_at in results]

    def list_history(self, name: str) -> list[GameSession]:
        with get_session() as session:
            player = session.scalar(select(Player).where(Player.name == name))
            if player is None:
                return []
            return (
                session.query(GameSession)
                .where(GameSession.player_id == player.id)
                .order_by(GameSession.started_at.desc())
                .all()
            )

    def _perk_used(self, session_obj: GameSession) -> bool:
        return any(
            [
                session_obj.front_joker_used,
                session_obj.back_joker_used,
                session_obj.mobile_joker_used,
            ]
        )
