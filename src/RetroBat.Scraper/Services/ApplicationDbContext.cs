using Microsoft.EntityFrameworkCore;
using RetroBatScraper.Models;

namespace  RetroBatScraper.Services;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Setting> Settings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=app.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Game.Configure(modelBuilder.Entity<Game>());
        Platform.Configure(modelBuilder.Entity<Platform>());
        Setting.Configure(modelBuilder.Entity<Setting>());
    }
}
