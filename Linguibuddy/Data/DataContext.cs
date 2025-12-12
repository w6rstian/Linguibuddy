using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<Flashcard> Flashcards { get; set; }
        public DbSet<DictionaryWord> DictionaryWords { get; set; }
        //public DbSet<Phonetic> Phonetics { get; set; }
        //public DbSet<Meaning> Meanings { get; set; }
        //public DbSet<Definition> Definitions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
