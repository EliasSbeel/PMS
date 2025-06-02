using PMS.Models;
using System.Collections.Generic;
using System.Linq;

namespace PMS.Data
{
    public static class ApplicationData
    {
        public static List<Product> Products { get; set; } = new List<Product>();
        public static List<User> Users { get; set; } = new List<User>();

        static ApplicationData()
        {
            // Seed initial data if needed
            if (!Users.Any())
            {
                Users.Add(new User { Id = 1, FullName = "Admin User", Email = "admin@example.com", Password = "password" }); // DEMO: Hash passwords in production!
            }
            if (!Products.Any())
            {
                Products.Add(new Product { Id = 1, ProductName = "Laptop", ProductCategory = "Electronics", StockQuantity = 100, Price = 1200.00m });
                Products.Add(new Product { Id = 2, ProductName = "Keyboard", ProductCategory = "Accessories", StockQuantity = 150, Price = 75.00m });
                Products.Add(new Product { Id = 3, ProductName = "Mouse", ProductCategory = "Accessories", StockQuantity = 200, Price = 25.00m });
                Products.Add(new Product { Id = 3, ProductName = "iPhone 16pro", ProductCategory = "Phone", StockQuantity = 39, Price = 225.00m });
            }
        }

        public static int GetNextProductId()
        {
            return Products.Any() ? Products.Max(p => p.Id) + 1 : 1;
        }
        public static int GetNextUserId()
        {
            return Users.Any() ? Users.Max(u => u.Id) + 1 : 1;
        }
    }
}