using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PMS.Models;
using PMS.Data;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic; 


namespace PMS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [Authorize]
    public IActionResult Home()
    {
        // Calculate dashboard metrics on the server
        var products = ApplicationData.Products;
        const int LOW_STOCK_THRESHOLD = 50;

        int totalProductsQuantity = 0;
        int inStockQuantity = 0;
        int outOfStockCount = 0;
        int lowStockCount = 0;
        HashSet<string> categories = new HashSet<string>();

        foreach (var product in products)
        {
            totalProductsQuantity += product.StockQuantity;
            if (product.StockQuantity > 0)
            {
                inStockQuantity += product.StockQuantity;
                if (product.StockQuantity <= LOW_STOCK_THRESHOLD)
                {
                    lowStockCount++;
                }
            }
            else
            {
                outOfStockCount++;
            }
            categories.Add(product.ProductCategory);
        }

        ViewBag.TotalProductsQuantity = totalProductsQuantity;
        ViewBag.InStockQuantity = inStockQuantity;
        ViewBag.OutOfStockCount = outOfStockCount;
        ViewBag.LowStockCount = lowStockCount;
        ViewBag.TotalCategories = categories.Count;

        return View(products);
    }

    [Authorize]
    public IActionResult Products(string searchTerm) 
    {
        IEnumerable<Product> products = ApplicationData.Products;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            products = products.Where(p =>
                p.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCategory.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        ViewBag.SearchTerm = searchTerm; 
        return View(products);
    }

    [Authorize]
    public IActionResult AddProduct(int? id)
    {
        Product product = null;
        if (id.HasValue)
        {
            product = ApplicationData.Products.FirstOrDefault(p => p.Id == id.Value);
        }
        return View(product);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken] // Ensures Anti-Forgery Token is present
    public IActionResult SaveProduct(Product product)
    {
        if (ModelState.IsValid)
        {
            if (product.Id == 0) 
            {
                product.Id = ApplicationData.GetNextProductId();
                ApplicationData.Products.Add(product);
                TempData["SuccessMessage"] = "Product added successfully!";
            }
            else // Existing product
            {
                var existingProduct = ApplicationData.Products.FirstOrDefault(p => p.Id == product.Id);
                if (existingProduct != null)
                {
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.ProductCategory = product.ProductCategory;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.Price = product.Price;
                    TempData["SuccessMessage"] = "Product updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found for update.";
                }
            }
            return RedirectToAction("Products");
        }
        TempData["ErrorMessage"] = "Please fill all fields correctly."; 
        return View("AddProduct", product);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken] 
    public IActionResult DeleteProduct(int id)
    {
        var productToRemove = ApplicationData.Products.FirstOrDefault(p => p.Id == id);
        if (productToRemove != null)
        {
            ApplicationData.Products.Remove(productToRemove);
            TempData["SuccessMessage"] = "Product deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Product not found.";
        }
        return RedirectToAction("Products");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken] 
    public IActionResult DeleteAllProducts()
    {
        ApplicationData.Products.Clear();
        TempData["SuccessMessage"] = "All products deleted successfully!";
        return RedirectToAction("Products");
    }

    [Authorize]
    public IActionResult ExportCsv()
    {
        var products = ApplicationData.Products;
        var csvContent = "Name,Category,Stock,Price\n";
        foreach (var product in products)
        {
            csvContent += $"{product.ProductName},{product.ProductCategory},{product.StockQuantity},{product.Price:F2}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", "products_data.csv");
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = ApplicationData.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Logged In! Welcome back!";
            return RedirectToAction("Home");
        }
        TempData["ErrorMessage"] = "Invalid email or password.";
        return View();
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(string fullName, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            TempData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }
        if (ApplicationData.Users.Any(u => u.Email == email))
        {
            TempData["ErrorMessage"] = "An account with this email already exists.";
            return View();
        }

        var newUser = new User { Id = ApplicationData.GetNextUserId(), FullName = fullName, Email = email, Password = password };
        ApplicationData.Users.Add(newUser);
        TempData["SuccessMessage"] = $"Account created for {newUser.FullName}! You can now log in.";
        return RedirectToAction("Login");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Logged Out!";
        return RedirectToAction("Login");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}