using System.ComponentModel.DataAnnotations;

namespace GlobalForge.Web.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public int AvailableStock { get; set; }

        public decimal TotalPrice => Price * Quantity;
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        
        public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
        
        public int TotalItems => Items.Sum(item => item.Quantity);

        public void AddItem(Product product, int quantity = 1)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    AvailableStock = product.Quantity
                });
            }
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }

        public void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        public void Clear()
        {
            Items.Clear();
        }
    }
}
