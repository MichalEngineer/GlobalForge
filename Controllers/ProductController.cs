using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GlobalForge.Web.Data;
using GlobalForge.Web.Models;
using GlobalForge.Web.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace GlobalForge.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment environment,
            ILogger<ProductController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: /Product/Index - Katalog produktów
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string? searchQuery, decimal? minPrice, decimal? maxPrice, List<string>? condition)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.IsActive);

            // Filtrowanie po kategorii
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Wyszukiwanie
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                productsQuery = productsQuery.Where(p => 
                    p.Name.Contains(searchQuery) || 
                    p.Description.Contains(searchQuery));
            }

            // Filtrowanie po cenie
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            // Filtrowanie po stanie (Nowy/Używany)
            if (condition != null && condition.Any())
            {
                productsQuery = productsQuery.Where(p => condition.Contains(p.Condition));
            }

            var products = await productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.ConditionNew = condition?.Contains("New") ?? false;
            ViewBag.ConditionUsed = condition?.Contains("Used") ?? false;

            return View(products);
        }

        // GET: /Product/Details/5 - Szczegóły produktu
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Produkt nie został znaleziony.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: /Product/MyProducts - Panel sprzedawcy
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyProducts()
        {
            var userId = _userManager.GetUserId(User);
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // GET: /Product/Create - Dodaj produkt
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    Condition = model.Condition,
                    CategoryId = model.CategoryId,
                    SellerId = userId ?? string.Empty,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Obsługa uploadu zdjęcia
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var imageUrl = await SaveProductImage(model.ImageFile);
                    product.ImageUrl = imageUrl;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Utworzono nowy produkt: {ProductName} przez {UserId}", product.Name, userId);
                TempData["SuccessMessage"] = "Produkt został pomyślnie dodany.";
                return RedirectToAction(nameof(MyProducts));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        // GET: /Product/Edit/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Produkt nie został znaleziony lub nie masz do niego dostępu.";
                return RedirectToAction(nameof(MyProducts));
            }

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                Condition = product.Condition,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive
            };

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Produkt nie został znaleziony lub nie masz do niego dostępu.";
                    return RedirectToAction(nameof(MyProducts));
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Quantity = model.Quantity;
                product.Condition = model.Condition;
                product.CategoryId = model.CategoryId;
                product.IsActive = model.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                // Obsługa nowego zdjęcia
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Usuń stare zdjęcie
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        DeleteProductImage(product.ImageUrl);
                    }

                    var imageUrl = await SaveProductImage(model.ImageFile);
                    product.ImageUrl = imageUrl;
                }

                _context.Update(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Zaktualizowano produkt: {ProductId} przez {UserId}", product.Id, userId);
                TempData["SuccessMessage"] = "Produkt został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(MyProducts));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        // POST: /Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Produkt nie został znaleziony lub nie masz do niego dostępu.";
                return RedirectToAction(nameof(MyProducts));
            }

            // Usuń zdjęcie
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                DeleteProductImage(product.ImageUrl);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usunięto produkt: {ProductId} przez {UserId}", product.Id, userId);
            TempData["SuccessMessage"] = "Produkt został pomyślnie usunięty.";
            return RedirectToAction(nameof(MyProducts));
        }

        // Prywatna metoda - zapis zdjęcia
        private async Task<string> SaveProductImage(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/products/{uniqueFileName}";
        }

        // Prywatna metoda - usuwanie zdjęcia
        private void DeleteProductImage(string imageUrl)
        {
            var imagePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        // POST: /Product/SubmitReview - Dodawanie recenzji
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            try
            {
                var userName = User.Identity?.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    return Json(new { success = false, message = "Musisz być zalogowany, aby dodać recenzję" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono użytkownika" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Nie znaleziono produktu" });
                }

                // Sprawdź czy użytkownik kupił ten produkt
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == productId && oi.Order.UserId == user.Id);

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Możesz wystawić recenzję tylko dla zakupionych produktów" });
                }

                // Sprawdź czy użytkownik już nie wystawił recenzji
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Już wystawiłeś recenzję dla tego produktu" });
                }

                var review = new Review
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Dodano recenzję dla produktu {productId} przez użytkownika {userName}");

                return Json(new { success = true, message = "Dziękujemy za wystawienie recenzji!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania recenzji");
                return Json(new { success = false, message = "Wystąpił błąd podczas dodawania recenzji" });
            }
        }
    }
}
