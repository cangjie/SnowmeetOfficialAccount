using System;
using Microsoft.EntityFrameworkCore;
using SnowmeetOfficialAccount.Models;
namespace SnowmeetOfficialAccount
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        public DbSet<SnowmeetOfficialAccount.Models.EfTest> EfTest { get; set; }

        public DbSet<OARecevie> oARecevie { get; set; }

        public DbSet<SnowmeetOfficialAccount.Models.OASent> oASent { get; set; }

        public DbSet<User> user { get; set; }

        public DbSet<MiniUser> miniUser { get; set; }

        public DbSet<ShopSaleInteract> shopSaleInteract { get; set; }
    }
}
