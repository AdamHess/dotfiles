using AccountDeduplication.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.DAL.EF
{
    // ReSharper disable once IdentifierTypo
    public class AccountDedupeDb(string filename = "D:/MatchRate_3.db") : DbContext
    {
        public DbSet<MatchRate> MatchRates { get; set; }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<ProcessingStatus> ProcessingStatuses { get; set; }

        public DbSet<GroupPair> GroupPairs { get; set; }
        public string DbPath { get; } = filename;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}")
                .UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<MatchRate>();
            e.HasKey(m => new { m.AccountId1, m.AccountId2 });

            e.Property(m => m.AccountId1)
                .HasColumnOrder(1)
                .HasMaxLength(18)
                .IsRequired();
            e.Property(m => m.AccountId2)
                .HasColumnOrder(2)
                .HasMaxLength(18)
                .IsRequired();

            var a = modelBuilder.Entity<Account>();
            a.HasKey(a => a.Id);
            a.Property(a => a.Id)
            .HasMaxLength(18)
            .IsRequired();
            a.HasIndex(m => m.BillingCity);
            a.HasIndex(m => m.ShippingCity);
            a.HasIndex(m => m.GroupingCityState);
            a.HasMany(m => m.GroupPairs)
                .WithOne(m => m.Account)
                .HasForeignKey(m => m.AccountId);
            a.Ignore(m => m.IsPersonAccount);

            var gp = modelBuilder.Entity<GroupPair>();
            gp.HasKey(m => new { m.AccountId, m.Phase });

            var ps = modelBuilder.Entity<ProcessingStatus>();
            ps.HasKey(m => m.GroupId);







        }
    }
}
