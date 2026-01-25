using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlobalForge.Web.Data;
using GlobalForge.Web.Models;

namespace GlobalForge.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Order/MyOrders - Historia zamówień kupującego
        public async Task<IActionResult> MyOrders()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return NotFound();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Seller)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Order/Details/5 - Szczegóły zamówienia
        public async Task<IActionResult> Details(int id)
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Sprawdź czy zamówienie należy do zalogowanego użytkownika
            if (order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: /Order/SoldOrders - Panel Sprzedawcy: Lista sprzedanych towarów
        public async Task<IActionResult> SoldOrders()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var seller = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (seller == null)
            {
                return NotFound();
            }

            // Pobierz wszystkie zamówienia zawierające produkty tego sprzedawcy
            var soldOrders = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == seller.Id)
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

            return View(soldOrders);
        }

        // POST: /Order/UpdateStatus - Zmiana statusu zamówienia
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono zamówienia" });
                }

                // Walidacja statusu
                var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Nieprawidłowy status" });
                }

                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Zmieniono status zamówienia #{orderId} na {status}");

                return Json(new { success = true, message = "Status zamówienia został zaktualizowany" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji statusu zamówienia");
                return Json(new { success = false, message = "Wystąpił błąd podczas aktualizacji statusu" });
            }
        }
    }
}
