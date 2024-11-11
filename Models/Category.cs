using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OrderManagementApp.Models
{
    public class Category
    {
        [Key]
        [Required]
        public int category_id { get; set; }
        public string category_name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        
    }
}
