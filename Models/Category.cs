using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementApp.Models
{
    public class Category
    {
        [Key] [Required] [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;
        
        //Navigation properties
        public ICollection<Product> Products { get; } = new List<Product>();
    }
}
