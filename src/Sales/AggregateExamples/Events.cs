using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Sales.AggregateExamples.Events
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
        public ShoppingCartItem(Guid shoppingCartId, Guid productId)
        {
            ShoppingCartId = shoppingCartId;
            ProductId = productId;
        }

        public Guid ShoppingCartId { get; set; }
        public Guid ProductId { get; set; }
    }



    public class ShoppingCartEventDomain
    {
        private List<object> _events = new List<object>();
        private readonly ShoppingCart _shoppingCart;

        public ShoppingCartEventDomain(ShoppingCart shoppingCart)
        {
            _shoppingCart = shoppingCart;
        }

        public List<object> GetEvents()
        {
            return _events;
        }

        public void AddItem(Guid productId, int quantity, decimal price)
        {
            var existingItem = _shoppingCart.Items.SingleOrDefault(x => x.ProductId == productId);
            if (existingItem != null)
            {
                _events.Add(new QuantityIncremented
                {
                    ShoppingCartId = _shoppingCart.ShoppingCartId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                _shoppingCart.Items.Add(new ShoppingCartItem(_shoppingCart.ShoppingCartId, productId));

                _events.Add(new ItemAdded
                {
                    ShoppingCartId = _shoppingCart.ShoppingCartId,
                    ProductId = productId,
                    Quantity = quantity,
                    Price = price
                });
            }
        }

        public void RemoveItem(Guid productId)
        {
            var product = _shoppingCart.Items.SingleOrDefault(x => x.ProductId == productId);
            if (product != null)
            {
                _events.Add(new ItemRemoved
                {
                    ShoppingCartId = _shoppingCart.ShoppingCartId,
                    ProductId = productId,
                });
            }
        }
    }

    public class ItemAdded
    {
        public Guid ShoppingCartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ItemRemoved
    {
        public Guid ShoppingCartId { get; set; }
        public Guid ProductId { get; set; }
    }

    public class QuantityIncremented
    {
        public Guid ShoppingCartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ShoppingCartRepository
    {
        private readonly IDbConnection _connection;

        public ShoppingCartRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<ShoppingCartEventDomain> GetShoppingCart(Guid shoppingCartId)
        {
            var shoppingCart = await _connection.QueryFirstAsync<ShoppingCart>("SELECT CustomerId FROM ShoppingCarts WHERE ShoppingCartId=@ShopingCartId",
                new {ShoppingCartId = shoppingCartId});

            var items = await _connection.QueryAsync<ShoppingCartItem>("SELECT ProductId, Quantity FROM ShoppingCartItems WHERE ShoppingCartId=@ShopingCartId",
                new {ShoppingCartId = shoppingCartId});
            shoppingCart.Items = items.ToList();

            return new ShoppingCartEventDomain(shoppingCart);
        }

        public async Task Save(ShoppingCartEventDomain shoppingCart)
        {
            var trx = _connection.BeginTransaction();
            var evnts = shoppingCart.GetEvents();
            foreach (var evnt in evnts)
            {
                if (evnt is ItemAdded itemAdded)
                {
                    await ItemAdded(itemAdded, trx);
                }
                else if (evnt is QuantityIncremented quantityIncremented)
                {
                    await QuantityIncremented(quantityIncremented, trx);
                }
                else if (evnt is ItemRemoved itemRemoved)
                {
                    await ItemRemoved(itemRemoved, trx);
                }
            }
            trx.Commit();
        }

        private async Task ItemAdded(ItemAdded evnt, IDbTransaction trx)
        {
            await _connection.ExecuteAsync("INSERT INTO ShoppingCartItems (ShoppingCartId, ProductId, Quantity, Price) VALUES (@ShoppingCartId, @ProductId, @Quantity, @Price)",
                new
                {
                    ShoppingCartId = evnt.ShoppingCartId,
                    ProductID = evnt.ProductId,
                    Quantity = evnt.Quantity,
                    Price = evnt.Price
                }, trx);
        }

        private async Task QuantityIncremented(QuantityIncremented evnt, IDbTransaction trx)
        {
            await _connection.ExecuteAsync("UPDATE ShoppingCartItems SET Quantity=Quantity+@Quantity WHERE ShoppingCartId=@ShoppingCartId AND ProductID=@ProductId",
                new
                {
                    ShoppingCartId = evnt.ShoppingCartId,
                    ProductID = evnt.ProductId,
                    Quantity = evnt.Quantity,
                }, trx);
        }

        private async Task ItemRemoved(ItemRemoved evnt, IDbTransaction trx)
        {
            await _connection.ExecuteAsync("DELETE FROM ShoppingCartItems WHERE ShoppingCartId=@ShoppingCartId AND ProductId=@ProductId",
                new
                {
                    ShoppingCartId = evnt.ShoppingCartId,
                    ProductID = evnt.ProductId
                }, trx);
        }
    }
}