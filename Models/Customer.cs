using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace OrderManagementApp.Models
{
    public class Customer
    {
        [Key]
        [Required]
        public int customer_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string contact_name { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;

        //Navigation properties
        public ICollection<Order> orders { get; set; } = new List<Order>();
    }
}
