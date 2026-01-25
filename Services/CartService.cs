using System.Text.Json;
using GlobalForge.Web.Models;

namespace GlobalForge.Web.Services
{
    public interface ICartService
    {
        Cart GetCart();
        void AddToCart(Product product, int quantity = 1);
        void UpdateQuantity(int productId, int quantity);
        void RemoveFromCart(int productId);
        void ClearCart();
        int GetCartItemCount();
    }

    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "ShoppingCart";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Cart GetCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return new Cart();

            var cartJson = session.GetString(CartSessionKey);
            
            if (string.IsNullOrEmpty(cartJson))
            {
                return new Cart();
            }

            return JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
        }

        public void AddToCart(Product product, int quantity = 1)
        {
            var cart = GetCart();
            cart.AddItem(product, quantity);
            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            cart.UpdateQuantity(productId, quantity);
            SaveCart(cart);
        }

        public void RemoveFromCart(int productId)
        {
            var cart = GetCart();
            cart.RemoveItem(productId);
            SaveCart(cart);
        }

        public void ClearCart()
        {
            var cart = new Cart();
            SaveCart(cart);
        }

        public int GetCartItemCount()
        {
            var cart = GetCart();
            return cart.TotalItems;
        }

        private void SaveCart(Cart cart)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString(CartSessionKey, cartJson);
            }
        }
    }
}
