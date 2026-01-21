using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClavierDor;

public enum RoleType
{
    Front,
    Back,
    Mobile
}

public static class RoleTypeExtensions
{
    public static string Label(this RoleType role) => role switch
    {
        RoleType.Front => "Développeur Front",
        RoleType.Back => "Développeur Back",
        RoleType.Mobile => "Développeur Mobile",
        _ => role.ToString()
    };
}

public sealed class Player
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public RoleType Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GameSession> Sessions { get; set; } = new();
}

public sealed class GameSession
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Player))]
    public int PlayerId { get; set; }

    public Player? Player { get; set; }

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
    [Key]
    public int Id { get; set; }

    [Required]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    public string ChoiceA { get; set; } = string.Empty;

    [Required]
    public string ChoiceB { get; set; } = string.Empty;

    [Required]
    public string ChoiceC { get; set; } = string.Empty;

    [Required]
    public string ChoiceD { get; set; } = string.Empty;

    [Required]
    [MaxLength(1)]
    public string CorrectChoice { get; set; } = "A";

    public int Stage { get; set; } = 1;

    public string Hint { get; set; } = string.Empty;
}

public sealed class AnswerLog
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(GameSession))]
    public int SessionId { get; set; }

    public GameSession? Session { get; set; }

    [ForeignKey(nameof(Question))]
    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    [Required]
    [MaxLength(1)]
    public string Selected { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

public sealed record RolePerk(string Label, string Description, string ActionLabel);

public static class RolePerks
{
    public static readonly Dictionary<RoleType, RolePerk> All = new()
    {
        {
            RoleType.Front,
            new RolePerk(
                "Changer de question",
                "Passe à la question suivante une fois par partie.",
                "Changer"
            )
        },
        {
            RoleType.Back,
            new RolePerk(
                "Rattrapage automatique",
                "Annule une mauvaise réponse une fois par partie.",
                "Rattrapage"
            )
        },
        {
            RoleType.Mobile,
            new RolePerk(
                "Indice",
                "Affiche un indice une fois par partie.",
                "Indice"
            )
        }
    };
}
