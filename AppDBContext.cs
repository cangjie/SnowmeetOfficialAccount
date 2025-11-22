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
            //modelBuilder.Entity<MemberSocialAccount>().HasOne<Member>()
            //    .WithMany(m => m.memberSocialAccounts).HasForeignKey(m => m.member_id);
        }
        public DbSet<SnowmeetOfficialAccount.Models.EfTest> EfTest { get; set; }
        public DbSet<OARecevie> oARecevie { get; set; }
        public DbSet<SnowmeetOfficialAccount.Models.OASent> oASent { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<MiniUser> miniUser { get; set; }
        public DbSet<ShopSaleInteract> shopSaleInteract { get; set; }
        public DbSet<OAUserInfo> oaUserInfo { get; set; }
        public DbSet<Member> member { get; set; }
        public DbSet<MemberSocialAccount> memberSocialAccount { get; set; }
        public DbSet<Ticket> ticket { get; set; }
        public DbSet<TicketTemplate> ticketTemplate { get; set; }
        public DbSet<RentAdditionalPayment> rentAdditionalPayment { get; set; }
        public DbSet<WebApiLog> webApiLog { get; set; }
        public DbSet<MaintainLive> maintainLive { get; set; }
        public DbSet<SocialAccountForJob> socialAccountForJob { get; set; }
        public DbSet<MiniSession> miniSession { get; set; }
        ///new season
        public DbSet<ScanQrCode> scanQrCode { get; set; }
        public DbSet<CoreDataModLog> dataLog { get; set; }
        public DbSet<Staff> staff { get; set; }
        public DbSet<StaffSocialAccount> staffSocialAccount { get; set; }
        
    }
}
