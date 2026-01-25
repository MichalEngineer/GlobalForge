using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlobalForge.Web.Data;
using GlobalForge.Web.Services;
using GlobalForge.Web.Models;

namespace GlobalForge.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<CartController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: /Cart/Index
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
            {
                return Json(new { success = false, message = "Produkt nie został znaleziony." });
            }

            // Walidacja stanów magazynowych
            var cart = _cartService.GetCart();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            var currentCartQuantity = existingItem?.Quantity ?? 0;
            var newTotalQuantity = currentCartQuantity + quantity;

            if (newTotalQuantity > product.Quantity)
            {
                return Json(new 
                { 
                    success = false, 
                    message = $"Przepraszamy, dostępne tylko {product.Quantity} sztuk." 
                });
            }

            _cartService.AddToCart(product, quantity);
            
            _logger.LogInformation("Dodano do koszyka: {ProductName}, ilość: {Quantity}", product.Name, quantity);

            return Json(new 
            { 
                success = true, 
                message = $"Dodano {product.Name} do koszyka",
                cartCount = _cartService.GetCartItemCount()
            });
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 0)
            {
                return Json(new { success = false, message = "Nieprawidłowa ilość." });
            }

            // Walidacja dostępności
            var product = await _context.Products.FindAsync(productId);
            if (product != null && quantity > product.Quantity)
            {
                return Json(new 
                { 
                    success = false, 
                    message = $"Dostępne tylko {product.Quantity} sztuk." 
                });
            }

            _cartService.UpdateQuantity(productId, quantity);

            var cart = _cartService.GetCart();
            return Json(new 
            { 
                success = true,
                cartTotal = cart.TotalAmount,
                cartCount = cart.TotalItems
            });
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public IActionResult RemoveItem(int productId)
        {
            _cartService.RemoveFromCart(productId);
            
            var cart = _cartService.GetCart();
            return Json(new 
            { 
                success = true,
                cartTotal = cart.TotalAmount,
                cartCount = cart.TotalItems
            });
        }

        // POST: /Cart/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            _cartService.ClearCart();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/GetCount - dla AJAX
        [HttpGet]
        public IActionResult GetCount()
        {
            return Json(new { count = _cartService.GetCartItemCount() });
        }
    }
}
