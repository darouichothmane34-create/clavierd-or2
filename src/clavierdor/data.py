from __future__ import annotations

from sqlalchemy import select

from .models import Question
from .orm import get_session

QUESTIONS = [
    {
        "prompt": "Quel est le résultat de 2 ** 3 en Python ?",
        "choice_a": "6",
        "choice_b": "8",
        "choice_c": "9",
        "choice_d": "12",
        "correct_choice": "B",
        "stage": 1,
        "hint": "L'opérateur ** signifie puissance.",
    },
    {
        "prompt": "Quelle commande Git permet de lister les branches locales ?",
        "choice_a": "git status",
        "choice_b": "git branch",
        "choice_c": "git log",
        "choice_d": "git checkout",
        "correct_choice": "B",
        "stage": 1,
        "hint": "C'est la commande la plus courte de la liste.",
    },
    {
        "prompt": "Quel protocole est utilisé par défaut pour sécuriser HTTP ?",
        "choice_a": "FTP",
        "choice_b": "SSH",
        "choice_c": "HTTPS",
        "choice_d": "SMTP",
        "correct_choice": "C",
        "stage": 2,
        "hint": "Ajoutez un 'S' à HTTP.",
    },
    {
        "prompt": "Quel langage est principalement utilisé pour le développement iOS natif ?",
        "choice_a": "Kotlin",
        "choice_b": "Swift",
        "choice_c": "Ruby",
        "choice_d": "Go",
        "correct_choice": "B",
        "stage": 2,
        "hint": "Un langage créé par Apple en 2014.",
    },
    {
        "prompt": "Quel design pattern favorise une instance unique et globale ?",
        "choice_a": "Singleton",
        "choice_b": "Observer",
        "choice_c": "Strategy",
        "choice_d": "Decorator",
        "correct_choice": "A",
        "stage": 3,
        "hint": "Il signifie littéralement 'unique'.",
    },
    {
        "prompt": "Quelle méthode HTTP est utilisée pour créer une ressource ?",
        "choice_a": "GET",
        "choice_b": "POST",
        "choice_c": "DELETE",
        "choice_d": "PATCH",
        "correct_choice": "B",
        "stage": 3,
        "hint": "On l'utilise souvent pour envoyer un formulaire.",
    },
]


def seed_questions() -> None:
    with get_session() as session:
        existing = session.scalar(select(Question.id))
        if existing:
            return
        session.add_all(Question(**item) for item in QUESTIONS)
        session.commit()
