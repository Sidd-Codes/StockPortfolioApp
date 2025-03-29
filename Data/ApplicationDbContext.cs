using Microsoft.EntityFrameworkCore;
using StockPortfolioApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace StockPortfolioApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<Stock> Stocks { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Portfolio>()
                .HasOne(p => p.User)
                .WithMany()  // If you want each user to have many portfolios
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // Or whatever delete behavior you prefer
        }
    }
}
