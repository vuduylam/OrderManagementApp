using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace OrderManagementApp.Models
{
    public class Order
    {
        [Key] [Column("order_id")]
        public int OrderId { get; set; }
        
        [ForeignKey("Customer")] [Column("customer_id")]
        public int CustomerId { get; set; }
        
        [Column("order_date")]
        public DateOnly OrderDate { get; set;  }

        //Navigation properties
        public Customer? Customer { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();
    }
}
