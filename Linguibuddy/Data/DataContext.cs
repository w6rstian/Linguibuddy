using Linguibuddy.Models;
using Linguibuddy.Helpers;
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
                    Id = 10,
                    Name = AppResources.Achievement10Name,
                    Description = AppResources.Achievement10Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 100
                },
                new Achievement
                {
                    Id = 11,
                    Name = AppResources.Achievement11Name,
                    Description = AppResources.Achievement11Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 500
                },
                new Achievement
                {
                    Id = 12,
                    Name = AppResources.Achievement12Name,
                    Description = AppResources.Achievement12Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 1000
                },
                new Achievement
                {
                    Id = 13,
                    Name = AppResources.Achievement13Name,
                    Description = AppResources.Achievement13Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 2500
                },
                new Achievement
                {
                    Id = 14,
                    Name = AppResources.Achievement14Name,
                    Description = AppResources.Achievement14Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 5000
                },
                new Achievement
                {
                    Id = 15,
                    Name = AppResources.Achievement15Name,
                    Description = AppResources.Achievement15Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.TotalPoints,
                    UnlockTargetValue = 10000
                },
                new Achievement
                {
                    Id = 20,
                    Name = AppResources.Achievement20Name,
                    Description = AppResources.Achievement20Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.LearningStreak,
                    UnlockTargetValue = 3
                },
                new Achievement
                {
                    Id = 21,
                    Name = AppResources.Achievement21Name,
                    Description = AppResources.Achievement21Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.LearningStreak,
                    UnlockTargetValue = 7
                },
                new Achievement
                {
                    Id = 22,
                    Name = AppResources.Achievement22Name,
                    Description = AppResources.Achievement22Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.LearningStreak,
                    UnlockTargetValue = 14
                },
                new Achievement
                {
                    Id = 23,
                    Name = AppResources.Achievement23Name,
                    Description = AppResources.Achievement23Description,
                    IconUrl = "trophy_100dp_light.png",
                    UnlockCondition = AchievementUnlockType.LearningStreak,
                    UnlockTargetValue = 30
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