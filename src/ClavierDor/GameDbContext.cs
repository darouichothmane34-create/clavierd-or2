using Microsoft.EntityFrameworkCore;

namespace ClavierDor;

public sealed class GameDbContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerLog> AnswerLogs => Set<AnswerLog>();

    public string DbPath { get; }

    public GameDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClavierDor"
        );
        Directory.CreateDirectory(folder);
        DbPath = Path.Combine(folder, "clavierdor.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasIndex(player => player.Name)
            .IsUnique();
    }
}
