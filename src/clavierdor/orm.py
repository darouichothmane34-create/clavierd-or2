from __future__ import annotations

from pathlib import Path

from sqlalchemy import create_engine
from sqlalchemy.orm import Session, sessionmaker

from .models import Base

DB_PATH = Path.home() / ".clavierdor" / "clavierdor.db"
DB_PATH.parent.mkdir(parents=True, exist_ok=True)

ENGINE = create_engine(f"sqlite:///{DB_PATH}", future=True)
SessionLocal = sessionmaker(bind=ENGINE, autoflush=False, future=True)


def init_db() -> None:
    Base.metadata.create_all(ENGINE)


def get_session() -> Session:
    return SessionLocal()
