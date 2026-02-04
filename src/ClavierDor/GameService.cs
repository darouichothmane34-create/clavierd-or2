using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ClavierDor;

public sealed class GameService
{
    public const int StageMax = 4;
    public static readonly IReadOnlyDictionary<int, string> StageLabels = new Dictionary<int, string>
    {
        [1] = "Qualification",
        [2] = "Demi-finale",
        [3] = "Boss",
        [4] = "Clavier d'or"
    };

    public static readonly IReadOnlyDictionary<RoleType, RolePerk> RolePerks =
        new Dictionary<RoleType, RolePerk>
        {
            [RoleType.Front] = new RolePerk(
                "Changer de question",
                "Passe à la question suivante une fois par partie.",
                "Changer"),
            [RoleType.Back] = new RolePerk(
                "Rattrapage automatique",
                "Annule une mauvaise réponse une fois par partie.",
                "Rattrapage"),
            [RoleType.Mobile] = new RolePerk(
                "Indice",
                "Affiche un indice une fois par partie.",
                "Indice")
        };

    private readonly ClavierDorContext _context;

    public GameService(ClavierDorContext context)
    {
        _context = context;
    }

    public SessionState StartNewGame(string name, RoleType role)
    {
        var player = _context.Players.SingleOrDefault(p => p.Name == name);
        if (player == null)
        {
            player = new Player { Name = name, Role = role };
            _context.Players.Add(player);
        }
        else
        {
            player.Role = role;
        }

        var newSession = new GameSession { Player = player };
        _context.GameSessions.Add(newSession);
        _context.SaveChanges();
        return BuildState(newSession.Id);
    }

    public SessionState? ResumeLastGame(string name)
    {
        var player = _context.Players.SingleOrDefault(p => p.Name == name);
        if (player == null)
        {
            return null;
        }

        var session = _context.GameSessions
            .Where(s => s.PlayerId == player.Id)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();

        return session == null ? null : BuildState(session.Id);
    }

    public SessionState SubmitAnswer(int sessionId, int questionId, string selected)
    {
        var session = _context.GameSessions
            .Include(s => s.Player)
            .Single(s => s.Id == sessionId);
        var question = _context.Questions.Single(q => q.Id == questionId);

        var isCorrect = selected == question.CorrectChoice;
        if (isCorrect)
        {
            session.Streak += 1;
            var scoreGain = session.Streak >= 3 ? 15 : 10;
            session.Score += scoreGain;
            if (session.Stage < StageMax)
            {
                session.Stage += 1;
            }
            else
            {
                session.Completed = true;
            }
        }
        else
        {
            session.Streak = 0;
        }

        _context.AnswerLogs.Add(new AnswerLog
        {
            GameSession = session,
            Question = question,
            Selected = selected,
            IsCorrect = isCorrect
        });
        _context.SaveChanges();
        return BuildState(sessionId);
    }

    public SessionState UseFrontJoker(int sessionId, int? currentQuestionId)
    {
        var session = _context.GameSessions.Include(s => s.Player).Single(s => s.Id == sessionId);
        if (session.FrontJokerUsed)
        {
            return BuildState(sessionId);
        }

        session.FrontJokerUsed = true;
        _context.SaveChanges();
        return BuildState(sessionId, currentQuestionId);
    }

    public SessionState UseBackJoker(int sessionId)
    {
        var session = _context.GameSessions.Include(s => s.Player).Single(s => s.Id == sessionId);
        if (session.BackJokerUsed)
        {
            return BuildState(sessionId);
        }

        var lastAnswer = _context.AnswerLogs
            .Where(l => l.GameSessionId == sessionId)
            .OrderByDescending(l => l.AnsweredAt)
            .FirstOrDefault();

        if (lastAnswer != null && !lastAnswer.IsCorrect)
        {
            session.Score += 5;
            session.Streak = 1;
        }

        session.BackJokerUsed = true;
        _context.SaveChanges();
        return BuildState(sessionId);
    }

    public string UseMobileJoker(int sessionId)
    {
        var session = _context.GameSessions.Single(s => s.Id == sessionId);
        if (session.MobileJokerUsed)
        {
            return "Indice déjà utilisé.";
        }

        session.MobileJokerUsed = true;
        _context.SaveChanges();
        var question = GetQuestionForStage(session.Stage, null);
        return question?.Hint ?? "Pas d'indice disponible.";
    }

    public List<(string Name, int Score, DateTime StartedAt)> ListScores()
    {
        return _context.GameSessions
            .Include(s => s.Player)
            .OrderByDescending(s => s.Score)
            .Select(s => new ValueTuple<string, int, DateTime>(s.Player.Name, s.Score, s.StartedAt))
            .ToList();
    }

    public List<GameSession> ListHistory(string name)
    {
        var player = _context.Players.SingleOrDefault(p => p.Name == name);
        if (player == null)
        {
            return new List<GameSession>();
        }

        return _context.GameSessions
            .Where(s => s.PlayerId == player.Id)
            .OrderByDescending(s => s.StartedAt)
            .ToList();
    }

    private SessionState BuildState(int sessionId, int? excludeQuestionId = null)
    {
        var session = _context.GameSessions
            .Include(s => s.Player)
            .Single(s => s.Id == sessionId);

        var question = session.Completed
            ? null
            : GetQuestionForStage(session.Stage, sessionId, excludeQuestionId);

        return new SessionState(
            session.Id,
            session.Player.Name,
            session.Player.Role,
            session.Stage,
            session.Score,
            session.Streak,
            question,
            session.Completed,
            session.FrontJokerUsed || session.BackJokerUsed || session.MobileJokerUsed);
    }

    private QuestionView? GetQuestionForStage(int stage, int? sessionId, int? excludeQuestionId = null)
    {
        var candidates = _context.Questions.Where(q => q.Stage == stage).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var rnd = new Random();
        var question = candidates[rnd.Next(candidates.Count)];
        if (excludeQuestionId.HasValue)
        {
            candidates = candidates.Where(q => q.Id != excludeQuestionId.Value).ToList();
            if (candidates.Count > 0)
            {
                question = candidates[rnd.Next(candidates.Count)];
            }
        }
        if (sessionId.HasValue)
        {
            var seen = _context.AnswerLogs
                .Where(l => l.GameSessionId == sessionId)
                .Select(l => l.QuestionId)
                .ToHashSet();

            var unseen = candidates.Where(q => !seen.Contains(q.Id)).ToList();
            if (unseen.Count > 0)
            {
                question = unseen[rnd.Next(unseen.Count)];
            }
        }

        return new QuestionView(
            question.Id,
            question.Prompt,
            new Dictionary<string, string>
            {
                ["A"] = question.ChoiceA,
                ["B"] = question.ChoiceB,
                ["C"] = question.ChoiceC,
                ["D"] = question.ChoiceD
            },
            question.CorrectChoice,
            question.Hint,
            question.Stage);
    }
}

public sealed record QuestionView(
    int Id,
    string Prompt,
    Dictionary<string, string> Choices,
    string CorrectChoice,
    string Hint,
    int Stage);

public sealed record SessionState(
    int SessionId,
    string PlayerName,
    RoleType Role,
    int Stage,
    int Score,
    int Streak,
    QuestionView? CurrentQuestion,
    bool Completed,
    bool PerkUsed);
