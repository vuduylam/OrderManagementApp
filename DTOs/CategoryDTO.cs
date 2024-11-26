using System.ComponentModel.DataAnnotations.Schema;
using OrderManagementApp.Models;
namespace OrderManagementApp.DTOs
{
    public class CategoryDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<ProductDTO>? Products;
    }
}
