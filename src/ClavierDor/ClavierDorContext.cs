using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace ClavierDor;

public sealed class ClavierDorContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerLog> AnswerLogs => Set<AnswerLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".clavierdor",
            "clavierdor.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasIndex(player => player.Name)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .Property(player => player.Role)
            .HasConversion<string>();

        modelBuilder.Entity<GameSession>()
            .HasOne(session => session.Player)
            .WithMany(player => player.Sessions)
            .HasForeignKey(session => session.PlayerId);

        modelBuilder.Entity<AnswerLog>()
            .HasOne(log => log.GameSession)
            .WithMany(session => session.Answers)
            .HasForeignKey(log => log.GameSessionId);

        modelBuilder.Entity<AnswerLog>()
            .HasOne(log => log.Question)
            .WithMany();
    }
}
