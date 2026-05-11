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

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Apartment>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Apartments)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApartmentSale>()
                .HasOne(s => s.Apartment)
                .WithOne(a => a.Sale)
                .HasForeignKey<ApartmentSale>(s => s.ApartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectImage>()
                .HasOne(i => i.Project)
                .WithMany(p => p.ProjectImages)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatBotLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActivityLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ChatBotLog> ChatBotLogs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectImage> ProjectImages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<ApartmentSale> ApartmentSales { get; set; }
    }
}
