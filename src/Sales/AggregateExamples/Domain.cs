using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sales.AggregateExamples.Domain
{
    public class ShoppingCart
    {
        public ShoppingCart(Guid shoppingCartId, Guid customerId)
        {
            ShoppingCartId = shoppingCartId;
            CustomerId = customerId;
        }

        public Guid ShoppingCartId { get; private set; }
        public Guid CustomerId { get; private set; }
        public IList<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
    }

    public class ShoppingCartItem
    {
        public ShoppingCartItem(Guid shoppingCartId, Guid productId, int quantity, decimal price)
        {
            ShoppingCartId = shoppingCartId;
            ProductId = productId;
            Quantity = quantity;
            Price = price;
        }

        public Guid ShoppingCartId { get; set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; set; }
        public decimal Price { get; private set; }
    }

    public class ShoppingCartDomain
    {
        private readonly ShoppingCart _shoppingCart;

        public ShoppingCartDomain(ShoppingCart shoppingCart)
        {
            _shoppingCart = shoppingCart;
        }

        public void AddItem(Guid productId, int quantity, decimal price)
        {
            var existingItem = _shoppingCart.Items.SingleOrDefault(x => x.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _shoppingCart.Items.Add(new ShoppingCartItem(_shoppingCart.ShoppingCartId, productId, quantity, price));
            }
        }

        public void RemoveItem(Guid productId)
        {
            var product = _shoppingCart.Items.SingleOrDefault(x => x.ProductId == productId);
            if (product != null)
            {
                _shoppingCart.Items.Remove(product);
            }
        }
    }

    public class ShoppingCartDomainRepository
    {
        private readonly SalesDbContext _dbContext;

        public ShoppingCartDomainRepository(SalesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ShoppingCartDomain> GetShoppingCart(Guid shoppingCartId)
        {
            var data = await _dbContext.ShoppingCarts
                .Include(x => x.Items)
                .SingleAsync(x => x.ShoppingCartId == shoppingCartId);

            return new ShoppingCartDomain(data);
        }

        public async Task Save()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}