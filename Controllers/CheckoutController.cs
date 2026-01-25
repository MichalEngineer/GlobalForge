using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GlobalForge.Web.Services;
using GlobalForge.Web.Data;
using GlobalForge.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GlobalForge.Web.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            
            if (cart.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Twój koszyk jest pusty. Dodaj produkty przed przejściem do kasy.";
                return RedirectToAction("Index", "Product");
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string deliveryAddress, string paymentMethod)
        {
            try
            {
                var cart = _cartService.GetCart();
                
                if (cart.Items.Count == 0)
                {
                    return Json(new { success = false, message = "Koszyk jest pusty" });
                }

                var userName = User.Identity?.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    return Json(new { success = false, message = "Użytkownik nie jest zalogowany" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                if (user == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono użytkownika" });
                }

                // Walidacja dostępności produktów i stanów magazynowych
                foreach (var cartItem in cart.Items)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null)
                    {
                        return Json(new { success = false, message = $"Produkt {cartItem.ProductName} nie istnieje" });
                    }

                    if (!product.IsActive)
                    {
                        return Json(new { success = false, message = $"Produkt {cartItem.ProductName} nie jest już dostępny" });
                    }

                    if (product.Quantity < cartItem.Quantity)
                    {
                        return Json(new { success = false, message = $"Niewystarczająca ilość produktu {cartItem.ProductName}. Dostępne: {product.Quantity} szt." });
                    }
                }

                // Tworzenie zamówienia
                var order = new Order
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.Now,
                    TotalAmount = cart.TotalAmount,
                    Status = "Pending",
                    DeliveryAddress = deliveryAddress,
                    PaymentMethod = paymentMethod,
                    DeliveryMethod = "Courier"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Tworzenie pozycji zamówienia i aktualizacja stanów magazynowych
                foreach (var cartItem in cart.Items)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.Price,
                            TotalPrice = cartItem.TotalPrice
                        };

                        _context.OrderItems.Add(orderItem);

                        // Zmniejszenie stanu magazynowego
                        product.Quantity -= cartItem.Quantity;
                        _context.Products.Update(product);
                    }
                }

                await _context.SaveChangesAsync();

                // Wyczyszczenie koszyka
                _cartService.ClearCart();

                _logger.LogInformation($"Utworzono zamówienie #{order.Id} dla użytkownika {userName}");

                return Json(new { success = true, orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas składania zamówienia");
                return Json(new { success = false, message = "Wystąpił błąd podczas składania zamówienia. Spróbuj ponownie." });
            }
        }

        public IActionResult OrderConfirmation(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Sprawdź czy zamówienie należy do zalogowanego użytkownika
            var userName = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
            
            if (user == null || order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }
    }
}
