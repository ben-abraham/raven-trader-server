using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server
{
    public class RTDbContext : DbContext
    {
        public RTDbContext(DbContextOptions<RTDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.ListingEntry>().ToTable("Listings");
            modelBuilder.Entity<Models.RC_Asset>().ToTable("RC_Assets");
            modelBuilder.Entity<Models.RC_Block>().ToTable("RC_Blocks");
            modelBuilder.Entity<Models.RC_Swap>().ToTable("RC_Swaps");
            modelBuilder.Entity<Models.RC_Transaction>().ToTable("RC_Transactions");
            modelBuilder.Entity<Models.RC_AssetVolume>().ToTable("RC_AssetVolume");
        }

        public DbSet<Models.ListingEntry> Listings { get; set; }
        public DbSet<Models.RC_Asset> Assets { get; set; }
        public DbSet<Models.RC_Block> Blocks { get; set; }
        public DbSet<Models.RC_Swap> Swaps { get; set; }
        public DbSet<Models.RC_Transaction> Transactions { get; set; }
        public DbSet<Models.RC_AssetVolume> AssetVolume { get; set; }
    }
}
