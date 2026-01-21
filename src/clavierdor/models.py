from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from enum import Enum

from sqlalchemy import (
    Boolean,
    DateTime,
    Enum as SqlEnum,
    ForeignKey,
    Integer,
    String,
    Text,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


class RoleType(str, Enum):
    FRONT = "Développeur Front"
    BACK = "Développeur Back"
    MOBILE = "Développeur Mobile"


class Player(Base):
    __tablename__ = "players"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(120), unique=True, nullable=False)
    role: Mapped[RoleType] = mapped_column(SqlEnum(RoleType), nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    sessions: Mapped[list["GameSession"]] = relationship(
        back_populates="player", cascade="all, delete-orphan"
    )


class GameSession(Base):
    __tablename__ = "game_sessions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    player_id: Mapped[int] = mapped_column(ForeignKey("players.id"))
    started_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    completed: Mapped[bool] = mapped_column(Boolean, default=False)
    stage: Mapped[int] = mapped_column(Integer, default=1)
    score: Mapped[int] = mapped_column(Integer, default=0)
    streak: Mapped[int] = mapped_column(Integer, default=0)
    front_joker_used: Mapped[bool] = mapped_column(Boolean, default=False)
    back_joker_used: Mapped[bool] = mapped_column(Boolean, default=False)
    mobile_joker_used: Mapped[bool] = mapped_column(Boolean, default=False)

    player: Mapped[Player] = relationship(back_populates="sessions")
    answers: Mapped[list["AnswerLog"]] = relationship(
        back_populates="session", cascade="all, delete-orphan"
    )


class Question(Base):
    __tablename__ = "questions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    prompt: Mapped[str] = mapped_column(Text, nullable=False)
    choice_a: Mapped[str] = mapped_column(String(200), nullable=False)
    choice_b: Mapped[str] = mapped_column(String(200), nullable=False)
    choice_c: Mapped[str] = mapped_column(String(200), nullable=False)
    choice_d: Mapped[str] = mapped_column(String(200), nullable=False)
    correct_choice: Mapped[str] = mapped_column(String(1), nullable=False)
    stage: Mapped[int] = mapped_column(Integer, default=1)
    hint: Mapped[str] = mapped_column(String(200), default="")


class AnswerLog(Base):
    __tablename__ = "answer_logs"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    session_id: Mapped[int] = mapped_column(ForeignKey("game_sessions.id"))
    question_id: Mapped[int] = mapped_column(ForeignKey("questions.id"))
    selected: Mapped[str] = mapped_column(String(1), nullable=False)
    is_correct: Mapped[bool] = mapped_column(Boolean, default=False)
    answered_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    session: Mapped[GameSession] = relationship(back_populates="answers")


@dataclass(frozen=True)
class RolePerk:
    label: str
    description: str
    action_label: str


ROLE_PERKS = {
    RoleType.FRONT: RolePerk(
        label="Changer de question",
        description="Passe à la question suivante une fois par partie.",
        action_label="Changer",
    ),
    RoleType.BACK: RolePerk(
        label="Rattrapage automatique",
        description="Annule une mauvaise réponse une fois par partie.",
        action_label="Rattrapage",
    ),
    RoleType.MOBILE: RolePerk(
        label="Indice",
        description="Affiche un indice une fois par partie.",
        action_label="Indice",
    ),
}
