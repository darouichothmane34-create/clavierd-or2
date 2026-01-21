from __future__ import annotations

from datetime import datetime
from pathlib import Path

from fpdf import FPDF


def export_scores(path: Path, scores: list[tuple[str, int, datetime]]) -> Path:
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font("Helvetica", size=16)
    pdf.cell(0, 10, "Classement - Clavier d'Or", ln=True)
    pdf.set_font("Helvetica", size=12)
    pdf.cell(0, 8, f"Exporté le {datetime.now():%d/%m/%Y %H:%M}", ln=True)
    pdf.ln(6)

    pdf.set_font("Helvetica", size=12)
    pdf.cell(80, 8, "Joueur", border=1)
    pdf.cell(40, 8, "Score", border=1)
    pdf.cell(60, 8, "Date", border=1, ln=True)

    for name, score, started_at in scores:
        pdf.cell(80, 8, name, border=1)
        pdf.cell(40, 8, str(score), border=1)
        pdf.cell(60, 8, f"{started_at:%d/%m/%Y}", border=1, ln=True)

    path.parent.mkdir(parents=True, exist_ok=True)
    pdf.output(str(path))
    return path
