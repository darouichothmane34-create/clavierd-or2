using System.Text;

namespace ClavierDor;

public sealed class MainForm : Form
{
    private readonly GameService _service = new();
    private SessionState? _state;

    private readonly TextBox _nameInput = new();
    private readonly ComboBox _roleSelect = new();
    private readonly Label _stageLabel = new();
    private readonly Label _promptLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Label _scoreLabel = new();
    private readonly Label _perkLabel = new();
    private readonly Button _perkButton = new();
    private readonly Dictionary<string, Button> _answerButtons = new();

    public MainForm()
    {
        Text = "Clavier d'Or";
        Width = 980;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 251);

        BuildLayout();
        UpdateUi();
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(31, 42, 68)
        };

        var title = new Label
        {
            Text = "Clavier d'Or",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 18)
        };

        var subtitle = new Label
        {
            Text = "Compétition de programmation",
            ForeColor = Color.FromArgb(205, 215, 244),
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(220, 24)
        };

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var rightPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 240
        };

        content.Controls.Add(leftPanel);
        content.Controls.Add(rightPanel);
        Controls.Add(content);

        BuildPlayerPanel(leftPanel);
        BuildQuestionPanel(leftPanel);
        BuildStatusPanel(leftPanel);
        BuildSidePanel(rightPanel);
    }

    private void BuildPlayerPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Joueur",
            Dock = DockStyle.Top,
            Height = 110,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var nameLabel = new Label { Text = "Nom", Location = new Point(15, 32), AutoSize = true };
        _nameInput.Location = new Point(70, 28);
        _nameInput.Width = 160;

        var roleLabel = new Label { Text = "Rôle", Location = new Point(250, 32), AutoSize = true };
        _roleSelect.Location = new Point(300, 28);
        _roleSelect.Width = 180;
        _roleSelect.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleSelect.Items.AddRange(Enum.GetValues<RoleType>().Select(role => role.Label()).ToArray());
        _roleSelect.SelectedIndex = 0;

        var newGameButton = new Button
        {
            Text = "Nouvelle partie",
            Location = new Point(70, 65),
            Width = 150
        };
        newGameButton.Click += (_, _) => StartGame();

        var resumeButton = new Button
        {
            Text = "Reprendre",
            Location = new Point(230, 65),
            Width = 100
        };
        resumeButton.Click += (_, _) => ResumeGame();

        group.Controls.Add(nameLabel);
        group.Controls.Add(_nameInput);
        group.Controls.Add(roleLabel);
        group.Controls.Add(_roleSelect);
        group.Controls.Add(newGameButton);
        group.Controls.Add(resumeButton);
        parent.Controls.Add(group);
    }

    private void BuildQuestionPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Épreuve",
            Dock = DockStyle.Top,
            Height = 320,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _stageLabel.Location = new Point(15, 25);
        _stageLabel.AutoSize = true;
        _stageLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        _promptLabel.Location = new Point(15, 50);
        _promptLabel.Size = new Size(640, 60);
        _promptLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        group.Controls.Add(_stageLabel);
        group.Controls.Add(_promptLabel);

        var answersPanel = new FlowLayoutPanel
        {
            Location = new Point(15, 120),
            Size = new Size(650, 170),
            FlowDirection = FlowDirection.TopDown
        };

        foreach (var choice in new[] { "A", "B", "C", "D" })
        {
            var button = new Button
            {
                Width = 620,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft
            };
            button.Click += (_, _) => SubmitAnswer(choice);
            _answerButtons[choice] = button;
            answersPanel.Controls.Add(button);
        }

        group.Controls.Add(answersPanel);
        parent.Controls.Add(group);
    }

    private void BuildStatusPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Statut",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _statusLabel.Location = new Point(15, 30);
        _statusLabel.Size = new Size(640, 40);
        _statusLabel.Text = "Bienvenue dans Clavier d'Or";

        _hintLabel.Location = new Point(15, 70);
        _hintLabel.Size = new Size(640, 40);
        _hintLabel.ForeColor = Color.FromArgb(51, 84, 255);

        group.Controls.Add(_statusLabel);
        group.Controls.Add(_hintLabel);
        parent.Controls.Add(group);
    }

    private void BuildSidePanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Actions",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var historyButton = new Button { Text = "Historique", Width = 180, Location = new Point(25, 35) };
        historyButton.Click += (_, _) => ShowHistory();

        var exportButton = new Button { Text = "Exporter PDF", Width = 180, Location = new Point(25, 75) };
        exportButton.Click += (_, _) => ExportPdf();

        var themeButton = new Button { Text = "Changer thème", Width = 180, Location = new Point(25, 115) };
        themeButton.Click += (_, _) => ToggleTheme();

        _perkLabel.Location = new Point(25, 160);
        _perkLabel.Size = new Size(180, 60);

        _perkButton.Text = "Activer";
        _perkButton.Width = 180;
        _perkButton.Location = new Point(25, 225);
        _perkButton.Click += (_, _) => UsePerk();

        _scoreLabel.Location = new Point(25, 270);
        _scoreLabel.AutoSize = true;
        _scoreLabel.Text = "Score : 0";

        group.Controls.Add(historyButton);
        group.Controls.Add(exportButton);
        group.Controls.Add(themeButton);
        group.Controls.Add(_perkLabel);
        group.Controls.Add(_perkButton);
        group.Controls.Add(_scoreLabel);
        parent.Controls.Add(group);
    }

    private void StartGame()
    {
        var name = _nameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Veuillez renseigner votre nom.", "Nom manquant");
            return;
        }

        var role = (RoleType)_roleSelect.SelectedIndex;
        _state = _service.StartNewGame(name, role);
        _statusLabel.Text = $"Bonne chance {name} !";
        _hintLabel.Text = string.Empty;
        UpdateUi();
    }

    private void ResumeGame()
    {
        var name = _nameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Veuillez renseigner votre nom.", "Nom manquant");
            return;
        }

        _state = _service.ResumeLastGame(name);
        if (_state is null)
        {
            MessageBox.Show("Aucune partie sauvegardée pour ce joueur.", "Information");
            return;
        }

        _statusLabel.Text = "Partie reprise.";
        UpdateUi();
    }

    private void SubmitAnswer(string choice)
    {
        if (_state?.CurrentQuestion is null)
        {
            return;
        }

        _state = _service.SubmitAnswer(_state.SessionId, _state.CurrentQuestion.Id, choice);
        _statusLabel.Text = _state.Streak > 0 ? "Bonne réponse !" : "Réponse enregistrée.";
        _hintLabel.Text = string.Empty;
        UpdateUi();
    }

    private void UsePerk()
    {
        if (_state is null)
        {
            return;
        }

        if (_state.Role == RoleType.Front)
        {
            _state = _service.UseFrontJoker(_state.SessionId);
            _statusLabel.Text = "Joker Front utilisé : question changée !";
        }
        else if (_state.Role == RoleType.Back)
        {
            _state = _service.UseBackJoker(_state.SessionId);
            _statusLabel.Text = "Joker Back utilisé : rattrapage appliqué.";
        }
        else
        {
            var hint = _service.UseMobileJoker(_state.SessionId);
            _hintLabel.Text = $"Indice : {hint}";
            _statusLabel.Text = "Joker Mobile utilisé.";
        }

        UpdateUi();
    }

    private void ExportPdf()
    {
        var scores = _service.ListScores();
        if (scores.Count == 0)
        {
            MessageBox.Show("Aucun score à exporter.", "Export PDF");
            return;
        }

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ClavierDor"
        );
        var path = Path.Combine(folder, "classement.pdf");
        PdfExporter.ExportScores(path, scores);
        MessageBox.Show($"PDF exporté vers {path}", "Export PDF");
    }

    private void ShowHistory()
    {
        var name = _nameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Veuillez renseigner votre nom.", "Nom manquant");
            return;
        }

        var history = _service.ListHistory(name);
        if (history.Count == 0)
        {
            MessageBox.Show("Aucune partie enregistrée.", "Historique");
            return;
        }

        var builder = new StringBuilder();
        foreach (var entry in history)
        {
            builder.AppendLine($"{entry.StartedAt:dd/MM/yyyy HH:mm} - Score {entry.Score} - Étape {entry.Stage}");
        }

        MessageBox.Show(builder.ToString(), "Historique");
    }

    private void ToggleTheme()
    {
        if (BackColor == Color.FromArgb(245, 247, 251))
        {
            BackColor = Color.FromArgb(15, 23, 42);
            _statusLabel.Text = "Thème sombre activé.";
        }
        else
        {
            BackColor = Color.FromArgb(245, 247, 251);
            _statusLabel.Text = "Thème clair activé.";
        }
    }

    private void UpdateUi()
    {
        if (_state is null)
        {
            _stageLabel.Text = string.Empty;
            _promptLabel.Text = "Aucune partie en cours.";
            foreach (var button in _answerButtons.Values)
            {
                button.Text = string.Empty;
                button.Enabled = false;
            }

            _perkButton.Enabled = false;
            return;
        }

        var stageLabel = GameService.StageLabels.TryGetValue(_state.Stage, out var label)
            ? label
            : "Épreuve";
        _stageLabel.Text = $"Étape {_state.Stage} - {stageLabel}";
        _scoreLabel.Text = $"Score : {_state.Score}";

        if (_state.Completed)
        {
            _promptLabel.Text = "Bravo ! Vous avez obtenu le Clavier d'Or 🎉";
            foreach (var button in _answerButtons.Values)
            {
                button.Text = string.Empty;
                button.Enabled = false;
            }

            _perkButton.Enabled = false;
            return;
        }

        if (_state.CurrentQuestion is null)
        {
            _promptLabel.Text = "Plus de questions disponibles.";
            foreach (var button in _answerButtons.Values)
            {
                button.Text = string.Empty;
                button.Enabled = false;
            }

            _perkButton.Enabled = false;
            return;
        }

        _promptLabel.Text = _state.CurrentQuestion.Prompt;
        foreach (var (choice, button) in _answerButtons)
        {
            button.Text = $"{choice}. {_state.CurrentQuestion.Choices[choice]}";
            button.Enabled = true;
        }

        var perk = RolePerks.All[_state.Role];
        _perkLabel.Text = $"Joker : {perk.Label}\n{perk.Description}";
        _perkButton.Text = perk.ActionLabel;
        _perkButton.Enabled = !_state.PerkUsed;
    }
}
