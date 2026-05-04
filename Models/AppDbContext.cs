using Microsoft.EntityFrameworkCore;

namespace OfisYonetimSistemi.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Sistemde tum islemleri yapabilir." },
                new Role { Id = 2, Name = "Sekreter", Description = "Proje, evrak ve firma kayitlarini yonetir." },
                new Role { Id = 3, Name = "Muhasebeci", Description = "Fatura ve gider islemlerini yonetir." },
                new Role { Id = 4, Name = "Personel", Description = "Sinirli goruntuleme ve islem yetkisine sahiptir." },
                new Role { Id = 5, Name = "Mudur", Description = "Personel hesaplarini yonetir ve kayitlari kontrol eder." }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "Sistem",
                    LastName = "Admin",
                    FullName = "Sistem Admin",
                    Email = "admin@ofis.com",
                    PhoneNumber = "0000000000",
                    CompanyName = "Ofis Yonetim Sistemi",
                    CompanySize = 1,
                    Password = "Admin123!",
                    RoleId = 1,
                    CreatedAt = new DateTime(2026, 4, 30)
                }
            );
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
