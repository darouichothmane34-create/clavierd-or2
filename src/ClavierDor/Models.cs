using System;
using System.Collections.Generic;

namespace ClavierDor;

public enum RoleType
{
    Front,
    Back,
    Mobile
}

public sealed class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoleType Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<GameSession> Sessions { get; set; } = new();
}

public sealed class GameSession
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public bool Completed { get; set; }
    public int Stage { get; set; } = 1;
    public int Score { get; set; }
    public int Streak { get; set; }
    public bool FrontJokerUsed { get; set; }
    public bool BackJokerUsed { get; set; }
    public bool MobileJokerUsed { get; set; }
    public List<AnswerLog> Answers { get; set; } = new();
}

public sealed class Question
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string ChoiceA { get; set; } = string.Empty;
    public string ChoiceB { get; set; } = string.Empty;
    public string ChoiceC { get; set; } = string.Empty;
    public string ChoiceD { get; set; } = string.Empty;
    public string CorrectChoice { get; set; } = string.Empty;
    public int Stage { get; set; }
    public string Hint { get; set; } = string.Empty;
}

public sealed class AnswerLog
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public string Selected { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

public sealed record RolePerk(string Label, string Description, string ActionLabel);
