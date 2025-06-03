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

        public string DbPath { get; }

        public AccountDedupeDb(string filename = "MatchRate.db")
        {
            if (filename.Contains('/') || filename.Contains('\\'))
            {
                DbPath = filename;
                return;
            }

            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, filename);
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=account_dedupe.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<MatchRate>();
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.AccountId1);
            e.HasIndex(m => m.AccountId2);
            e.Property(m => m.AccountId1).HasMaxLength(18).IsRequired();
            e.Property(m => m.AccountId2).HasMaxLength(18).IsRequired();



        }
    }
}
