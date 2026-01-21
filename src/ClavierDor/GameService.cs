using Microsoft.EntityFrameworkCore;

namespace ClavierDor;

public sealed record QuestionView(
    int Id,
    string Prompt,
    Dictionary<string, string> Choices,
    string CorrectChoice,
    string Hint,
    int Stage
);

public sealed record SessionState(
    int SessionId,
    string PlayerName,
    RoleType Role,
    int Stage,
    int Score,
    int Streak,
    QuestionView? CurrentQuestion,
    bool Completed,
    bool PerkUsed
);

public sealed class GameService
{
    public const int StageMax = 4;

    public static readonly Dictionary<int, string> StageLabels = new()
    {
        { 1, "Qualification" },
        { 2, "Demi-finale" },
        { 3, "Boss final" },
        { 4, "Clavier d'or" }
    };

    private List<Question> _questionCache = new();

    public GameService()
    {
        using var context = new GameDbContext();
        context.Database.EnsureCreated();
        SeedData.EnsureSeeded(context);
    }

    private void LoadQuestions(GameDbContext context)
    {
        _questionCache = context.Questions.AsNoTracking().ToList();
        _questionCache = _questionCache.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    private Question? GetQuestionForStage(GameDbContext context, int stage)
    {
        if (_questionCache.Count == 0)
        {
            LoadQuestions(context);
        }

        return _questionCache.FirstOrDefault(question => question.Stage == stage);
    }

    private static QuestionView MapQuestion(Question question) => new(
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
        question.Stage
    );

    public SessionState StartNewGame(string name, RoleType role)
    {
        using var context = new GameDbContext();
        var player = context.Players.FirstOrDefault(p => p.Name == name);
        if (player is null)
        {
            player = new Player { Name = name, Role = role };
            context.Players.Add(player);
        }
        else
        {
            player.Role = role;
        }

        var session = new GameSession { Player = player };
        context.GameSessions.Add(session);
        context.SaveChanges();
        return BuildState(session.Id);
    }

    public SessionState? ResumeLastGame(string name)
    {
        using var context = new GameDbContext();
        var player = context.Players.FirstOrDefault(p => p.Name == name);
        if (player is null)
        {
            return null;
        }

        var session = context.GameSessions
            .Where(gs => gs.PlayerId == player.Id)
            .OrderByDescending(gs => gs.StartedAt)
            .FirstOrDefault();

        return session is null ? null : BuildState(session.Id);
    }

    public SessionState SubmitAnswer(int sessionId, int questionId, string selected)
    {
        using var context = new GameDbContext();
        var session = context.GameSessions.Include(gs => gs.Player).FirstOrDefault(gs => gs.Id == sessionId);
        var question = context.Questions.FirstOrDefault(q => q.Id == questionId);
        if (session is null || question is null)
        {
            throw new InvalidOperationException("Session ou question inconnue.");
        }

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

        context.AnswerLogs.Add(new AnswerLog
        {
            Session = session,
            QuestionId = question.Id,
            Selected = selected,
            IsCorrect = isCorrect
        });

        context.SaveChanges();
        return BuildState(sessionId);
    }

    public SessionState UseFrontJoker(int sessionId)
    {
        using var context = new GameDbContext();
        var session = context.GameSessions.Include(gs => gs.Player).FirstOrDefault(gs => gs.Id == sessionId);
        if (session is null)
        {
            throw new InvalidOperationException("Session inconnue.");
        }

        if (session.FrontJokerUsed)
        {
            return BuildState(sessionId);
        }

        session.FrontJokerUsed = true;
        session.Stage = Math.Min(session.Stage + 1, StageMax);
        context.SaveChanges();
        return BuildState(sessionId);
    }

    public SessionState UseBackJoker(int sessionId)
    {
        using var context = new GameDbContext();
        var session = context.GameSessions.Include(gs => gs.Player).FirstOrDefault(gs => gs.Id == sessionId);
        if (session is null)
        {
            throw new InvalidOperationException("Session inconnue.");
        }

        if (session.BackJokerUsed)
        {
            return BuildState(sessionId);
        }

        var lastAnswer = context.AnswerLogs
            .Where(answer => answer.SessionId == sessionId)
            .OrderByDescending(answer => answer.AnsweredAt)
            .FirstOrDefault();

        if (lastAnswer is not null && !lastAnswer.IsCorrect)
        {
            session.Score += 5;
            session.Streak = 1;
        }

        session.BackJokerUsed = true;
        context.SaveChanges();
        return BuildState(sessionId);
    }

    public string UseMobileJoker(int sessionId)
    {
        using var context = new GameDbContext();
        var session = context.GameSessions.FirstOrDefault(gs => gs.Id == sessionId);
        if (session is null)
        {
            throw new InvalidOperationException("Session inconnue.");
        }

        if (session.MobileJokerUsed)
        {
            return "Indice déjà utilisé.";
        }

        session.MobileJokerUsed = true;
        var question = GetQuestionForStage(context, session.Stage);
        context.SaveChanges();
        return question?.Hint ?? "Pas d'indice disponible.";
    }

    public List<(string Name, int Score, DateTime StartedAt)> ListScores()
    {
        using var context = new GameDbContext();
        return context.GameSessions
            .Include(gs => gs.Player)
            .OrderByDescending(gs => gs.Score)
            .Select(gs => new ValueTuple<string, int, DateTime>(
                gs.Player!.Name,
                gs.Score,
                gs.StartedAt
            ))
            .ToList();
    }

    public List<GameSession> ListHistory(string name)
    {
        using var context = new GameDbContext();
        var player = context.Players.FirstOrDefault(p => p.Name == name);
        if (player is null)
        {
            return new List<GameSession>();
        }

        return context.GameSessions
            .Where(gs => gs.PlayerId == player.Id)
            .OrderByDescending(gs => gs.StartedAt)
            .ToList();
    }

    public SessionState BuildState(int sessionId)
    {
        using var context = new GameDbContext();
        var session = context.GameSessions
            .Include(gs => gs.Player)
            .FirstOrDefault(gs => gs.Id == sessionId);
        if (session is null)
        {
            throw new InvalidOperationException("Session introuvable.");
        }

        QuestionView? questionView = null;
        if (!session.Completed)
        {
            var question = GetQuestionForStage(context, session.Stage);
            if (question is not null)
            {
                questionView = MapQuestion(question);
            }
        }

        var perkUsed = session.FrontJokerUsed || session.BackJokerUsed || session.MobileJokerUsed;

        return new SessionState(
            session.Id,
            session.Player?.Name ?? string.Empty,
            session.Player?.Role ?? RoleType.Front,
            session.Stage,
            session.Score,
            session.Streak,
            questionView,
            session.Completed,
            perkUsed
        );
    }
}
