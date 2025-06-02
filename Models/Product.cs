namespace PMS.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCategory { get; set; }
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
    }
}