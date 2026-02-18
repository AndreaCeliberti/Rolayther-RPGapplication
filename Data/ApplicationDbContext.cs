using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rolayther.Models.Entities;
using Rolayther.Models.Entities.Bridges;

namespace Rolayther.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Master> Masters { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<SessionStateHistory> SessionStateHistories { get; set; }
        public DbSet<MasterGame> MasterGames { get; set; }
        public DbSet<MasterPlatform> MasterPlatforms { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Chiamata fondamentale per Identity
            base.OnModelCreating(modelBuilder);

            // Filtri per Soft Delete
            modelBuilder.Entity<Game>().HasQueryFilter(g => !g.IsDeleted);
            modelBuilder.Entity<Genre>().HasQueryFilter(g => !g.IsDeleted);
            modelBuilder.Entity<Master>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Platform>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Player>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Session>().HasQueryFilter(s => !s.IsDeleted);

           

            // Risoluzione errore "Genre.Sessions" (se EF continua a cercarlo)
            modelBuilder.Entity<Genre>().Ignore("Sessions");

            // Configurazione Relazioni per evitare Cicli Cascade

            // Relazione Session -> Master
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Master)
                .WithMany(m => m.Sessions)
                .HasForeignKey(s => s.MasterId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relazione Session -> Genre
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Genre)
                .WithMany()
                .HasForeignKey(s => s.GenreId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relazione Session -> Game
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Game)
                .WithMany()
                .HasForeignKey(s => s.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relazione Genre -> Game (esplicita per evitare comportamento di default a cascata)
            //modelBuilder.Entity<Genre>()
            //.HasOne(g => g.Game)
            //.WithMany(gm => gm.Genres)
            //.HasForeignKey(g => g.GameId)
            //.OnDelete(DeleteBehavior.NoAction);

            // Relazione Game -> Master (esplicita, usando la FK nel model Game)
            //modelBuilder.Entity<Game>()
            //HasOne(g => g.Master)
            //.WithMany(m => m.Games)
            //.HasForeignKey(g => g.MasterId)
            //.OnDelete(DeleteBehavior.NoAction);

            // Relazione Platform -> Master
            //modelBuilder.Entity<Platform>()
            //.HasOne(p => p.Master)
            //.WithMany(m => m.Platform)
            //.HasForeignKey(p => p.MasterId)
            //.OnDelete(DeleteBehavior.NoAction);

            //  Relazione many-to-many Session <-> Player: join table "SessionPlayer" + chiave composta (univocità)
            modelBuilder.Entity<Session>()
                .HasMany(s => s.Players)
                .WithMany(p => p.Sessions)
                .UsingEntity<Dictionary<string, object>>(
                    "SessionPlayer",
                    j => j.HasOne<Player>()
                          .WithMany()
                          .HasForeignKey("PlayerId")
                          .OnDelete(DeleteBehavior.NoAction),
                    j => j.HasOne<Session>()
                          .WithMany()
                          .HasForeignKey("SessionId")
                          .OnDelete(DeleteBehavior.NoAction),
                    j =>
                    {
                        //  impedisce duplicati SessionId+PlayerId e rende più pulito il comportamento in race
                        j.HasKey("SessionId", "PlayerId");
                    }
                );


            // Relazione SessionStateHistory -> Session
            modelBuilder.Entity<SessionStateHistory>()
                .HasOne(h => h.Session)
                .WithMany(s => s.StateHistory)
                .HasForeignKey(h => h.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurazione relazione many-to-many Master <-> Game tramite MasterGame
            modelBuilder.Entity<MasterGame>()
                .HasKey(mg => new { mg.MasterId, mg.GameId });

            modelBuilder.Entity<MasterGame>()
                .HasOne(mg => mg.Master)
                .WithMany(m => m.MasterGames)
                .HasForeignKey(mg => mg.MasterId);

            modelBuilder.Entity<MasterGame>()
                .HasOne(mg => mg.Game)
                .WithMany(g => g.MasterGames)
                .HasForeignKey(mg => mg.GameId);

            // Configurazione relazione many-to-many Master <-> Platform tramite MasterPlatform
            modelBuilder.Entity<MasterPlatform>()
                .HasKey(mp => new { mp.MasterId, mp.PlatformId });

            modelBuilder.Entity<MasterPlatform>()
                .HasOne(mp => mp.Master)
                .WithMany(m => m.MasterPlatforms)
                .HasForeignKey(mp => mp.MasterId);

            modelBuilder.Entity<MasterPlatform>()
                .HasOne(mp => mp.Platform)
                .WithMany(p => p.MasterPlatforms)
                .HasForeignKey(mp => mp.PlatformId);

            

        }
    }
}