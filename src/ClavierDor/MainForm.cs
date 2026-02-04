using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClavierDor;

public sealed class MainForm : Form
{
    private readonly GameService _service;
    private SessionState? _state;

    private readonly TextBox _nameInput = new();
    private readonly ComboBox _roleInput = new();
    private readonly Label _statusLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Label _progressLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _scoreLabel = new();
    private readonly Label _stageLabel = new();
    private readonly Label _promptLabel = new();
    private readonly Button _perkButton = new();
    private readonly Button[] _answerButtons = new Button[4];

    public MainForm(GameService service)
    {
        _service = service;
        Text = "Clavier d'Or";
        Size = new Size(940, 680);
        BackColor = Color.FromArgb(245, 247, 251);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(31, 42, 68)
        };
        var title = new Label
        {
            Text = "Clavier d'Or",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };
        var subtitle = new Label
        {
            Text = "Compétition de programmation",
            ForeColor = Color.FromArgb(205, 215, 244),
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Location = new Point(200, 20),
            AutoSize = true
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };
        Controls.Add(content);

        var leftPanel = new Panel { Dock = DockStyle.Fill };
        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 230, Padding = new Padding(10) };
        content.Controls.Add(leftPanel);
        content.Controls.Add(rightPanel);

        BuildPlayerPanel(leftPanel);
        BuildQuestionPanel(leftPanel);
        BuildStatusPanel(leftPanel);
        BuildSidePanel(rightPanel);

        UpdateUi();
    }

    private void BuildPlayerPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Joueur",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Height = 110,
            Dock = DockStyle.Top
        };

        var nameLabel = new Label { Text = "Nom", Location = new Point(16, 30), AutoSize = true };
        _nameInput.Location = new Point(70, 26);
        _nameInput.Width = 160;

        var roleLabel = new Label { Text = "Rôle", Location = new Point(250, 30), AutoSize = true };
        _roleInput.Location = new Point(300, 26);
        _roleInput.Width = 200;
        _roleInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleInput.Items.AddRange(Enum.GetNames(typeof(RoleType)));
        _roleInput.SelectedIndex = 0;

        var newGame = new Button { Text = "Nouvelle partie", Location = new Point(16, 65), Width = 150 };
        newGame.Click += (_, _) => StartGame();
        var resume = new Button { Text = "Reprendre", Location = new Point(180, 65), Width = 120 };
        resume.Click += (_, _) => ResumeGame();

        group.Controls.Add(nameLabel);
        group.Controls.Add(_nameInput);
        group.Controls.Add(roleLabel);
        group.Controls.Add(_roleInput);
        group.Controls.Add(newGame);
        group.Controls.Add(resume);
        parent.Controls.Add(group);
    }

    private void BuildQuestionPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Épreuve",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 290
        };

        _stageLabel.Location = new Point(16, 30);
        _stageLabel.AutoSize = true;
        _stageLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        _promptLabel.Location = new Point(16, 60);
        _promptLabel.AutoSize = false;
        _promptLabel.Size = new Size(610, 60);

        for (var i = 0; i < 4; i++)
        {
            var button = new Button
            {
                Width = 600,
                Height = 30,
                Location = new Point(16, 130 + i * 35)
            };
            var choice = ((char)('A' + i)).ToString();
            button.Click += (_, _) => SubmitAnswer(choice);
            _answerButtons[i] = button;
            group.Controls.Add(button);
        }

        group.Controls.Add(_stageLabel);
        group.Controls.Add(_promptLabel);
        parent.Controls.Add(group);
    }

    private void BuildStatusPanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Statut",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 150
        };

        _statusLabel.Location = new Point(16, 30);
        _statusLabel.AutoSize = false;
        _statusLabel.Size = new Size(650, 30);

        _hintLabel.Location = new Point(16, 60);
        _hintLabel.AutoSize = true;
        _hintLabel.ForeColor = Color.FromArgb(51, 84, 255);

        _progressLabel.Location = new Point(16, 85);
        _progressLabel.AutoSize = true;

        _progressBar.Location = new Point(230, 85);
        _progressBar.Width = 200;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = GameService.StageMax;

        group.Controls.Add(_statusLabel);
        group.Controls.Add(_hintLabel);
        group.Controls.Add(_progressLabel);
        group.Controls.Add(_progressBar);
        parent.Controls.Add(group);
    }

    private void BuildSidePanel(Control parent)
    {
        var group = new GroupBox
        {
            Text = "Actions",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Fill
        };

        var historyButton = new Button { Text = "Historique", Width = 180, Location = new Point(20, 30) };
        historyButton.Click += (_, _) => ShowHistory();
        var leaderboardButton = new Button { Text = "Classement", Width = 180, Location = new Point(20, 70) };
        leaderboardButton.Click += (_, _) => ShowLeaderboard();
        var exportButton = new Button { Text = "Exporter PDF", Width = 180, Location = new Point(20, 110) };
        exportButton.Click += (_, _) => ExportPdf();
        var saveButton = new Button { Text = "Enregistrer", Width = 180, Location = new Point(20, 150) };
        saveButton.Click += (_, _) => SaveGame();

        var perkLabel = new Label { Text = "Joker", Location = new Point(20, 200), AutoSize = true };
        _perkButton.Text = "Activer";
        _perkButton.Width = 180;
        _perkButton.Location = new Point(20, 260);
        _perkButton.Click += (_, _) => UsePerk();

        _scoreLabel.Location = new Point(20, 320);
        _scoreLabel.AutoSize = true;

        group.Controls.Add(historyButton);
        group.Controls.Add(leaderboardButton);
        group.Controls.Add(exportButton);
        group.Controls.Add(saveButton);
        group.Controls.Add(perkLabel);
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

        var role = Enum.Parse<RoleType>(_roleInput.SelectedItem!.ToString()!);
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
        if (_state == null)
        {
            MessageBox.Show("Aucune partie sauvegardée pour ce joueur.", "Info");
            return;
        }

        _statusLabel.Text = "Partie reprise.";
        UpdateUi();
    }

    private void SubmitAnswer(string choice)
    {
        if (_state?.CurrentQuestion == null)
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
        if (_state == null)
        {
            return;
        }

        if (_state.Role == RoleType.Front)
        {
            var questionId = _state.CurrentQuestion?.Id;
            _state = _service.UseFrontJoker(_state.SessionId, questionId);
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
        if (!scores.Any())
        {
            MessageBox.Show("Aucun score à exporter.", "Export");
            return;
        }

        var path = PdfExporter.ExportScores(scores);
        MessageBox.Show($"PDF exporté vers {path}", "Export");
    }

    private void SaveGame()
    {
        if (_state == null)
        {
            MessageBox.Show("Aucune partie en cours.", "Enregistrer");
            return;
        }

        _statusLabel.Text = "Partie enregistrée.";
        MessageBox.Show("La partie est sauvegardée automatiquement.", "Enregistrer");
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
        if (!history.Any())
        {
            MessageBox.Show("Aucune partie enregistrée.", "Historique");
            return;
        }

        var lines = history.Select(item =>
            $"{item.StartedAt:dd/MM/yyyy HH:mm} - Score {item.Score} - Étape {item.Stage}");
        MessageBox.Show(string.Join(Environment.NewLine, lines), "Historique");
    }

    private void ShowLeaderboard()
    {
        var scores = _service.ListScores();
        if (!scores.Any())
        {
            MessageBox.Show("Aucun score enregistré.", "Classement");
            return;
        }

        var lines = scores.Take(10)
            .Select((score, index) =>
                $"{index + 1}. {score.Name} - {score.Score} pts ({score.StartedAt:dd/MM/yyyy})");
        MessageBox.Show(string.Join(Environment.NewLine, lines), "Classement");
    }

    private void UpdateUi()
    {
        if (_state == null)
        {
            _stageLabel.Text = string.Empty;
            _promptLabel.Text = "Aucune partie en cours.";
            foreach (var button in _answerButtons)
            {
                button.Enabled = false;
                button.Text = string.Empty;
            }

            _perkButton.Enabled = false;
            _progressLabel.Text = $"Progression : 0/{GameService.StageMax}";
            _progressBar.Value = 0;
            _scoreLabel.Text = "Score : 0";
            return;
        }

        var stageName = GameService.StageLabels.GetValueOrDefault(_state.Stage, "Épreuve");
        _stageLabel.Text = $"Étape {_state.Stage} - {stageName}";
        _scoreLabel.Text = $"Score : {_state.Score}";
        _progressLabel.Text = $"Progression : {_state.Stage}/{GameService.StageMax}";
        _progressBar.Value = _state.Stage;

        if (_state.Completed)
        {
            _promptLabel.Text = "Bravo ! Vous avez obtenu le Clavier d'Or 🎉";
            foreach (var button in _answerButtons)
            {
                button.Enabled = false;
                button.Text = string.Empty;
            }

            _perkButton.Enabled = false;
            return;
        }

        if (_state.CurrentQuestion == null)
        {
            _promptLabel.Text = "Plus de questions disponibles.";
            foreach (var button in _answerButtons)
            {
                button.Enabled = false;
                button.Text = string.Empty;
            }

            _perkButton.Enabled = false;
            return;
        }

        _promptLabel.Text = _state.CurrentQuestion.Prompt;
        for (var i = 0; i < _answerButtons.Length; i++)
        {
            var key = ((char)('A' + i)).ToString();
            _answerButtons[i].Text = $"{key}. {_state.CurrentQuestion.Choices[key]}";
            _answerButtons[i].Enabled = true;
        }

        var perk = GameService.RolePerks[_state.Role];
        _perkButton.Text = perk.ActionLabel;
        _perkButton.Enabled = !_state.PerkUsed;
    }
}
