using Microsoft.EntityFrameworkCore;
using Sales.AggregateExamples.Domain;

namespace Sales
{
    public class SalesDbContext : DbContext
    {
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("LooselyCoupledMonolith_Sales");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShoppingCart>().HasKey(x => x.ShoppingCartId);
            modelBuilder.Entity<ShoppingCartItem>().HasKey(x => new {x.ShoppingCartId, x.ProductId});
            base.OnModelCreating(modelBuilder);

        }
    }
}