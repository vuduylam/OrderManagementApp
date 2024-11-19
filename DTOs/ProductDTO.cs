using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagementApp.DTOs
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } =string.Empty;
        public int CategoryId { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
