from __future__ import annotations

import tkinter as tk
from pathlib import Path
from tkinter import messagebox, ttk

from .models import ROLE_PERKS, RoleType
from .pdf_export import export_scores
from .services import GameService, SessionState


class ClavierDorApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title("Clavier d'Or")
        self.geometry("900x650")
        self.configure(bg="#f5f7fb")

        self.service = GameService()
        self.state: SessionState | None = None
        self.player_name = tk.StringVar()
        self.role_choice = tk.StringVar(value=RoleType.FRONT.value)
        self.status_text = tk.StringVar(value="Bienvenue dans Clavier d'Or")
        self.hint_text = tk.StringVar(value="")
        self.accuracy_text = tk.StringVar(value="Précision : 0%")
        self.progress_text = tk.StringVar(value="Progression : 0/4")
        self.theme = tk.StringVar(value="clair")

        self._build_layout()

    def _build_layout(self) -> None:
        header = tk.Frame(self, bg="#1f2a44", pady=12)
        header.pack(fill=tk.X)
        tk.Label(
            header,
            text="Clavier d'Or",
            fg="white",
            bg="#1f2a44",
            font=("Helvetica", 20, "bold"),
        ).pack(side=tk.LEFT, padx=20)
        tk.Label(
            header,
            text="Compétition de programmation",
            fg="#cdd7f4",
            bg="#1f2a44",
            font=("Helvetica", 12),
        ).pack(side=tk.LEFT)

        content = tk.Frame(self, bg="#f5f7fb")
        content.pack(fill=tk.BOTH, expand=True, padx=20, pady=20)

        left = tk.Frame(content, bg="#f5f7fb")
        left.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        right = tk.Frame(content, bg="#f5f7fb", padx=20)
        right.pack(side=tk.RIGHT, fill=tk.Y)

        self._build_player_panel(left)
        self._build_question_panel(left)
        self._build_status_panel(left)
        self._build_side_panel(right)

    def _build_player_panel(self, parent: tk.Frame) -> None:
        frame = tk.LabelFrame(
            parent, text="Joueur", bg="#f5f7fb", font=("Helvetica", 11, "bold")
        )
        frame.pack(fill=tk.X, pady=8)

        tk.Label(frame, text="Nom", bg="#f5f7fb").grid(row=0, column=0, padx=10, pady=8)
        tk.Entry(frame, textvariable=self.player_name, width=25).grid(
            row=0, column=1, padx=10, pady=8
        )

        tk.Label(frame, text="Rôle", bg="#f5f7fb").grid(row=0, column=2, padx=10, pady=8)
        roles = [role.value for role in RoleType]
        ttk.Combobox(frame, textvariable=self.role_choice, values=roles, state="readonly").grid(
            row=0, column=3, padx=10, pady=8
        )

        tk.Button(frame, text="Nouvelle partie", command=self.start_game).grid(
            row=1, column=0, padx=10, pady=8
        )
        tk.Button(frame, text="Reprendre", command=self.resume_game).grid(
            row=1, column=1, padx=10, pady=8
        )

    def _build_question_panel(self, parent: tk.Frame) -> None:
        self.question_frame = tk.LabelFrame(
            parent, text="Épreuve", bg="#f5f7fb", font=("Helvetica", 11, "bold")
        )
        self.question_frame.pack(fill=tk.BOTH, expand=True, pady=8)

        self.stage_label = tk.Label(
            self.question_frame,
            text="",
            bg="#f5f7fb",
            font=("Helvetica", 12, "bold"),
        )
        self.stage_label.pack(anchor="w", padx=10, pady=6)

        self.prompt_label = tk.Label(
            self.question_frame,
            text="",
            wraplength=520,
            bg="#f5f7fb",
            font=("Helvetica", 12),
            justify="left",
        )
        self.prompt_label.pack(anchor="w", padx=10)

        self.answer_buttons: dict[str, tk.Button] = {}
        for choice in ["A", "B", "C", "D"]:
            button = tk.Button(
                self.question_frame,
                text="",
                width=50,
                command=lambda c=choice: self.submit_answer(c),
            )
            button.pack(padx=10, pady=4, anchor="w")
            self.answer_buttons[choice] = button

    def _build_status_panel(self, parent: tk.Frame) -> None:
        frame = tk.LabelFrame(
            parent,
            text="Statut",
            bg="#f5f7fb",
            font=("Helvetica", 11, "bold"),
        )
        frame.pack(fill=tk.X, pady=8)

        tk.Label(frame, textvariable=self.status_text, bg="#f5f7fb", wraplength=650).pack(
            anchor="w", padx=10, pady=6
        )
        tk.Label(frame, textvariable=self.hint_text, bg="#f5f7fb", fg="#3354ff").pack(
            anchor="w", padx=10, pady=4
        )
        tk.Label(frame, textvariable=self.accuracy_text, bg="#f5f7fb").pack(
            anchor="w", padx=10, pady=4
        )
        tk.Label(frame, textvariable=self.progress_text, bg="#f5f7fb").pack(
            anchor="w", padx=10, pady=4
        )

        self.progress_bar = ttk.Progressbar(frame, maximum=self.service.STAGE_MAX, length=260)
        self.progress_bar.pack(anchor="w", padx=10, pady=6)

    def _build_side_panel(self, parent: tk.Frame) -> None:
        panel = tk.LabelFrame(
            parent,
            text="Actions",
            bg="#f5f7fb",
            font=("Helvetica", 11, "bold"),
        )
        panel.pack(fill=tk.BOTH, expand=True)

        tk.Button(panel, text="Historique", command=self.show_history).pack(
            fill=tk.X, padx=10, pady=6
        )
        tk.Button(panel, text="Classement", command=self.show_leaderboard).pack(
            fill=tk.X, padx=10, pady=6
        )
        tk.Button(panel, text="Exporter PDF", command=self.export_pdf).pack(
            fill=tk.X, padx=10, pady=6
        )
        tk.Button(panel, text="Enregistrer", command=self.save_game).pack(
            fill=tk.X, padx=10, pady=6
        )
        tk.Button(panel, text="Changer thème", command=self.toggle_theme).pack(
            fill=tk.X, padx=10, pady=6
        )

        self.perk_label = tk.Label(panel, text="", bg="#f5f7fb", wraplength=180)
        self.perk_label.pack(padx=10, pady=10)
        self.perk_button = tk.Button(panel, text="Activer", command=self.use_perk)
        self.perk_button.pack(fill=tk.X, padx=10, pady=6)

        self.score_label = tk.Label(panel, text="Score : 0", bg="#f5f7fb")
        self.score_label.pack(padx=10, pady=10)

    def _update_ui(self) -> None:
        if self.state is None:
            self.stage_label.config(text="")
            self.prompt_label.config(text="Aucune partie en cours.")
            for button in self.answer_buttons.values():
                button.config(text="", state=tk.DISABLED)
            self.perk_button.config(state=tk.DISABLED)
            self.accuracy_text.set("Précision : 0%")
            self.progress_text.set(f"Progression : 0/{self.service.STAGE_MAX}")
            self.progress_bar["value"] = 0
            return

        stage_name = self.service.STAGE_LABELS.get(self.state.stage, "Épreuve")
        self.stage_label.config(text=f"Étape {self.state.stage} - {stage_name}")
        self.score_label.config(text=f"Score : {self.state.score}")
        self.accuracy_text.set(
            f"Précision : {self.state.accuracy:.0f}% ({self.state.correct_answers}/"
            f"{self.state.total_answers})"
        )
        self.progress_text.set(
            f"Progression : {self.state.stage}/{self.service.STAGE_MAX}"
        )
        self.progress_bar["value"] = self.state.stage

        if self.state.completed:
            self.prompt_label.config(text="Bravo ! Vous avez obtenu le Clavier d'Or 🎉")
            for button in self.answer_buttons.values():
                button.config(text="", state=tk.DISABLED)
            self.perk_button.config(state=tk.DISABLED)
            return

        question = self.state.current_question
        if question is None:
            self.prompt_label.config(text="Plus de questions disponibles.")
            for button in self.answer_buttons.values():
                button.config(text="", state=tk.DISABLED)
            self.perk_button.config(state=tk.DISABLED)
            return

        self.prompt_label.config(text=question.prompt)
        for key, button in self.answer_buttons.items():
            button.config(text=f"{key}. {question.choices[key]}", state=tk.NORMAL)

        perk = ROLE_PERKS[self.state.role]
        self.perk_label.config(text=f"Joker : {perk.label}\n{perk.description}")
        self.perk_button.config(
            text=perk.action_label,
            state=tk.DISABLED if self.state.perk_used else tk.NORMAL,
        )

    def start_game(self) -> None:
        name = self.player_name.get().strip()
        if not name:
            messagebox.showwarning("Nom manquant", "Veuillez renseigner votre nom.")
            return
        role = RoleType(self.role_choice.get())
        self.state = self.service.start_new_game(name, role)
        self.status_text.set(f"Bonne chance {name} !")
        self.hint_text.set("")
        self._update_ui()

    def resume_game(self) -> None:
        name = self.player_name.get().strip()
        if not name:
            messagebox.showwarning("Nom manquant", "Veuillez renseigner votre nom.")
            return
        state = self.service.resume_last_game(name)
        if state is None:
            messagebox.showinfo("Info", "Aucune partie sauvegardée pour ce joueur.")
            return
        self.state = state
        self.status_text.set("Partie reprise.")
        self._update_ui()

    def submit_answer(self, choice: str) -> None:
        if self.state is None or self.state.current_question is None:
            return
        self.state = self.service.submit_answer(
            self.state.session_id, self.state.current_question.id, choice
        )
        feedback = "Bonne réponse !" if self.state.streak > 0 else "Réponse enregistrée."
        self.status_text.set(feedback)
        self.hint_text.set("")
        self._update_ui()

    def use_perk(self) -> None:
        if self.state is None:
            return
        if self.state.role == RoleType.FRONT:
            question_id = (
                self.state.current_question.id if self.state.current_question else None
            )
            self.state = self.service.use_front_joker(self.state.session_id, question_id)
            self.status_text.set("Joker Front utilisé : question changée !")
        elif self.state.role == RoleType.BACK:
            self.state = self.service.use_back_joker(self.state.session_id)
            self.status_text.set("Joker Back utilisé : rattrapage appliqué.")
        else:
            hint = self.service.use_mobile_joker(self.state.session_id)
            self.hint_text.set(f"Indice : {hint}")
            self.status_text.set("Joker Mobile utilisé.")
        self._update_ui()

    def export_pdf(self) -> None:
        scores = self.service.list_scores()
        if not scores:
            messagebox.showinfo("Export", "Aucun score à exporter.")
            return
        path = Path.home() / ".clavierdor" / "classement.pdf"
        export_scores(path, scores)
        messagebox.showinfo("Export", f"PDF exporté vers {path}")

    def save_game(self) -> None:
        if self.state is None:
            messagebox.showinfo("Enregistrer", "Aucune partie en cours.")
            return
        self.status_text.set("Partie enregistrée.")
        messagebox.showinfo("Enregistrer", "La partie est déjà sauvegardée automatiquement.")

    def show_history(self) -> None:
        name = self.player_name.get().strip()
        if not name:
            messagebox.showwarning("Nom manquant", "Veuillez renseigner votre nom.")
            return
        history = self.service.list_history(name)
        if not history:
            messagebox.showinfo("Historique", "Aucune partie enregistrée.")
            return
        lines = [
            f"{item.started_at:%d/%m/%Y %H:%M} - Score {item.score} - Étape {item.stage}"
            for item in history
        ]
        messagebox.showinfo("Historique", "\n".join(lines))

    def show_leaderboard(self) -> None:
        scores = self.service.list_scores()
        if not scores:
            messagebox.showinfo("Classement", "Aucun score enregistré.")
            return
        lines = [
            f"{index + 1}. {name} - {score} pts ({started_at:%d/%m/%Y})"
            for index, (name, score, started_at) in enumerate(scores[:10])
        ]
        messagebox.showinfo("Classement", "\n".join(lines))

    def toggle_theme(self) -> None:
        if self.theme.get() == "clair":
            self.theme.set("sombre")
            self.configure(bg="#0f172a")
            self.status_text.set("Thème sombre activé.")
        else:
            self.theme.set("clair")
            self.configure(bg="#f5f7fb")
            self.status_text.set("Thème clair activé.")


def run() -> None:
    app = ClavierDorApp()
    app.mainloop()
