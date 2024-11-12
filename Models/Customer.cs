using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace OrderManagementApp.Models
{
    public class Customer
    {
        [Key] [Required] [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [Column("contact_name")]
        public string ContactName { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;
        
        [Column("city")]
        public string City { get; set; } = string.Empty;
        
        [Column("postal_code")] 
        public string PostalCode { get; set; } = string.Empty;
        
        [Column("country")]
        public string Country { get; set; } = string.Empty;

        //Navigation properties
        public ICollection<Order> Orders { get; } = new List<Order>();
    }
}
