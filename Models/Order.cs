using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
        [JsonIgnore]
        public Customer? Customer { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
