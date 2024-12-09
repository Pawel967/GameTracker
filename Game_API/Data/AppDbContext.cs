using Game_API.Models.Auth;
using Game_API.Models.Library;
using Game_API.Models.Notification;
using Game_API.Models.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Game_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Auth
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        // Profile
        public DbSet<UserFollowing> UserFollowings { get; set; }

        // Library
        public DbSet<Game> Games { get; set; }
        public DbSet<UserGameLibrary> UserGameLibraries { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<GameGenre> GameGenres { get; set; }
        public DbSet<GameTheme> GameThemes { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureAuth(modelBuilder);
            ConfigureProfile(modelBuilder);
            ConfigureLibrary(modelBuilder);
            ConfigureNotifications(modelBuilder);
        }

        private void ConfigureAuth(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasMany(u => u.Roles)
                    .WithMany(r => r.Users)
                    .UsingEntity(j => j.ToTable("UserRoles"));

                entity.Property(u => u.Username).IsRequired();
                entity.Property(u => u.Email).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.IsProfilePrivate).HasDefaultValue(false);
            });

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = Guid.NewGuid(), Name = Role.RoleNames.Admin },
                new Role { Id = Guid.NewGuid(), Name = Role.RoleNames.User }
            );
        }

        private void ConfigureProfile(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserFollowing>(entity =>
            {
                entity.HasKey(uf => new { uf.FollowerId, uf.FollowedId });

                entity.HasOne(uf => uf.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(uf => uf.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(uf => uf.Followed)
                    .WithMany(u => u.FollowedBy)
                    .HasForeignKey(uf => uf.FollowedId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureLibrary(ModelBuilder modelBuilder)
        {
            // Configure Game
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Rating).HasPrecision(4, 2);

                // Configure relationships
                entity.HasMany(g => g.UserGameLibraries)
                    .WithOne(ugl => ugl.Game)
                    .HasForeignKey(ugl => ugl.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(g => g.GameGenres)
                    .WithOne(gg => gg.Game)
                    .HasForeignKey(gg => gg.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(g => g.GameThemes)
                    .WithOne(gt => gt.Game)
                    .HasForeignKey(gt => gt.GameId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserGameLibrary
            modelBuilder.Entity<UserGameLibrary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();
                entity.Property(e => e.DateAdded).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Status)
                    .HasDefaultValue(GameStatus.Playing)
                    .HasConversion<string>();

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.DateAdded);
                entity.HasIndex(e => e.IsFavorite);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserGameLibraries)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Genre
            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();

                entity.HasMany(g => g.GameGenres)
                    .WithOne(gg => gg.Genre)
                    .HasForeignKey(gg => gg.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Theme
            modelBuilder.Entity<Theme>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();

                entity.HasMany(t => t.GameThemes)
                    .WithOne(gt => gt.Theme)
                    .HasForeignKey(gt => gt.ThemeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure GameGenre
            modelBuilder.Entity<GameGenre>(entity =>
            {
                entity.HasKey(e => new { e.GameId, e.GenreId });
            });

            // Configure GameTheme
            modelBuilder.Entity<GameTheme>(entity =>
            {
                entity.HasKey(e => new { e.GameId, e.ThemeId });
            });
        }

        private void ConfigureNotifications(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsRead).HasDefaultValue(false);

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.TriggerUser)
                    .WithMany()
                    .HasForeignKey(n => n.TriggerUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
            });
        }
    }
}
