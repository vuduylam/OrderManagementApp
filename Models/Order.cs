using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace OrderManagementApp.Models
{
    public class Order
    {
        [Key]
        public int order_id { get; set; }
        //[ForeignKey("Customer")]
        public int customer_id1 { get; set; }
        public DateOnly order_date { get; set;  }

        //Navigation properties
        //public Customer? customer { get; set; }
    }
}
