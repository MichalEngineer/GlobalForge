using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlobalForge.Web.Data;
using GlobalForge.Web.Models;

namespace GlobalForge.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Index - Panel główny administratora
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                UsersCount = await _context.Users.CountAsync(),
                ProductsCount = await _context.Products.CountAsync(),
                OrdersCount = await _context.Orders.CountAsync(),
                ActiveProductsCount = await _context.Products.CountAsync(p => p.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }

        // GET: /Admin/Users - Lista użytkowników
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        // POST: /Admin/ToggleUserStatus - Blokowanie/Odblokowywanie użytkownika
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono użytkownika" });
                }

                // Toggle LockoutEnd - jeśli zablokowany, odblokuj; jeśli nie, zablokuj na 100 lat
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now)
                {
                    user.LockoutEnd = null;
                    _logger.LogInformation($"Odblokowano użytkownika {user.UserName}");
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Użytkownik został odblokowany", isLocked = false });
                }
                else
                {
                    user.LockoutEnd = DateTimeOffset.Now.AddYears(100);
                    _logger.LogInformation($"Zablokowano użytkownika {user.UserName}");
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Użytkownik został zablokowany", isLocked = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zmiany statusu użytkownika");
                return Json(new { success = false, message = "Wystąpił błąd podczas zmiany statusu użytkownika" });
            }
        }

        // POST: /Admin/DeleteUser - Usuwanie użytkownika
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono użytkownika" });
                }

                // Sprawdź czy użytkownik nie ma aktywnych zamówień
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
                if (hasOrders)
                {
                    return Json(new { success = false, message = "Nie można usunąć użytkownika z historią zamówień. Użyj blokady konta." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usunięto użytkownika {user.UserName}");
                return Json(new { success = true, message = "Użytkownik został usunięty" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania użytkownika");
                return Json(new { success = false, message = "Wystąpił błąd podczas usuwania użytkownika" });
            }
        }

        // GET: /Admin/Products - Lista wszystkich produktów
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // POST: /Admin/DeleteProduct - Usuwanie produktu
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono produktu" });
                }

                // Sprawdź czy produkt nie jest w zamówieniach
                var isInOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
                if (isInOrders)
                {
                    // Jeśli produkt jest w zamówieniach, tylko dezaktywuj
                    product.IsActive = false;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Dezaktywowano produkt {product.Name}");
                    return Json(new { success = true, message = "Produkt został dezaktywowany (nie można usunąć - istnieją zamówienia)" });
                }

                // Usuń zdjęcie jeśli istnieje
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imagePath = product.ImageUrl.Replace("/images/products/", "");
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", imagePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usunięto produkt {product.Name}");
                return Json(new { success = true, message = "Produkt został usunięty" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania produktu");
                return Json(new { success = false, message = "Wystąpił błąd podczas usuwania produktu" });
            }
        }

        // POST: /Admin/ToggleProductStatus - Aktywacja/Dezaktywacja produktu
        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono produktu" });
                }

                product.IsActive = !product.IsActive;
                await _context.SaveChangesAsync();

                var status = product.IsActive ? "aktywowany" : "dezaktywowany";
                _logger.LogInformation($"Produkt {product.Name} został {status}");

                return Json(new { success = true, message = $"Produkt został {status}", isActive = product.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zmiany statusu produktu");
                return Json(new { success = false, message = "Wystąpił błąd podczas zmiany statusu produktu" });
            }
        }

        // GET: /Admin/Orders - Lista wszystkich zamówień
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }
    }
}
