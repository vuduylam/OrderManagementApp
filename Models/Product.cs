using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementApp.Models
{
    public class Product
    {
        [Key] [Required] [Column("product_id")]
        public int ProductId { get; set; }

        [Column("product_name")]
        public string? ProductName { get; set; }
             
        [ForeignKey("Category")] [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("unit")]
        public string? Unit {  get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        //Navigation properties
        public ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();
        public Category? Category { get; set; } = null!;
    }
}
