using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sales.AggregateExamples.Domain;

namespace Sales.AggregateExamples.Entity
{
    public class ShoppingCartRepository
    {
        private readonly SalesDbContext _dbContext;

        public ShoppingCartRepository(SalesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ShoppingCart> GetShoppingCart(Guid shoppingCartId)
        {
            return await _dbContext.ShoppingCarts
                .Include(x => x.Items)
                .SingleAsync(x => x.ShoppingCartId == shoppingCartId);
        }

        public async Task Save()
        {
            await _dbContext.SaveChangesAsync();
        }
    }


}