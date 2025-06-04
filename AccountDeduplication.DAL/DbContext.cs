using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.DAL
{
    // ReSharper disable once IdentifierTypo
    public class AccountDedupeDb : DbContext
    {
        public DbSet<MatchRate> MatchRates { get; set; }

        public DbSet<Account> Accounts { get; set; }
        public string DbPath { get; }

        public AccountDedupeDb(string filename = "MatchRate.db")
        {
            DbPath = filename;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<MatchRate>();
            e.HasKey(m => new { m.AccountId1, m.AccountId2});
            
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
            


        }
    }
}
