using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Linguibuddy.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<DictionaryWord> DictionaryWords { get; set; }
    public DbSet<WordCollection> WordCollections { get; set; }
    public DbSet<CollectionItem> CollectionItems { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<UserLearningDay> UserLearningDays { get; set; }

    public DbSet<Flashcard> Flashcards { get; set; }
    //public DbSet<Phonetic> Phonetics { get; set; }
    //public DbSet<Meaning> Meanings { get; set; }
    //public DbSet<Definition> Definitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // usuniecie słowa z bazy usunie powiązane znaczenia i fonetyki
        modelBuilder.Entity<DictionaryWord>()
            .HasMany(w => w.Meanings)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DictionaryWord>()
            .HasMany(w => w.Phonetics)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Meaning>()
            .HasMany(m => m.Definitions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Definition>()
            .Property(e => e.Synonyms)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<string>>(v) ?? new List<string>());

        modelBuilder.Entity<Definition>()
            .Property(e => e.Antonyms)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<string>>(v) ?? new List<string>());

        // usunięcie kolekcji usunie elementy kolekcji z bazy
        modelBuilder.Entity<WordCollection>()
            .HasMany(c => c.Items)
            .WithOne(f => f.Collection)
            .HasForeignKey(f => f.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Achievement>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<Achievement>()
            .HasData(
                new Achievement
                {
                    Id = 1,
                    Name = AppResources.Achievement1Name,
                    Description = AppResources.Achievement1Description,
                    IconUrl = "trophy_100dp_light.png"
                }
            );

        modelBuilder.Entity<UserAchievement>()
            .HasKey(ua => ua.Id);

        modelBuilder.Entity<UserAchievement>()
            .HasOne(ua => ua.AppUser)
            .WithMany(u => u.UserAchievements)
            .HasForeignKey(ua => ua.AppUserId);

        modelBuilder.Entity<UserAchievement>()
            .HasOne(ua => ua.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(ua => ua.AchievementId);

        modelBuilder.Entity<UserLearningDay>()
            .HasKey(uld => uld.Id);

        modelBuilder.Entity<UserLearningDay>()
            .HasOne(uld => uld.AppUser)
            .WithMany(au => au.LearningDays)
            .HasForeignKey(uld => uld.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserLearningDay>()
            .Property(x => x.AppUserId)
            .IsRequired();

        modelBuilder.Entity<UserLearningDay>()
            .HasIndex(x => new { x.AppUserId, x.Date })
            .IsUnique();

        modelBuilder.Entity<UserLearningDay>()
            .Property(x => x.Date)
            .HasColumnType("date");
    }
}