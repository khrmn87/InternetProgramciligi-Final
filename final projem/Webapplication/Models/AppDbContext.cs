using Webapplication.Models;
using AspNetCoreHero.ToastNotification.Notyf.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NETCore.Encrypt.Extensions;

namespace Webapplication.Models
{
    public class AppDbContext : IdentityDbContext <AppUser,AppRole,string>
    {
        private readonly IConfiguration _config;
        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration config) : base(options)
        {
            _config = config;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<File> Files { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var adminRolId = Guid.NewGuid().ToString(); // Admin rolü ID
            var adminUserId = Guid.NewGuid().ToString(); // Admin kullanıcı ID



            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id=1,
                    Name = "PDF",
                    Description = "pdF DOSYALARI",

                },
                new Category
                {
                    Id=2,
                      Name = "RAR",
                      Description = "RAR DOSYALARI",

                }
                );

            modelBuilder.Entity<AppRole>().HasData(
                new AppRole
                {
                    Id = adminRolId,
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new AppRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Kullanici",
                    NormalizedName = "KULLANICI"
                });

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = adminUserId,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<AppUser>().HashPassword(null, "AliAdmin123."),
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                    FirstName = "Admin",
                    LastName = "User"
                });

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = adminUserId,
                    RoleId = adminRolId
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
