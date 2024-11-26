using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagementApp.DTOs
{
    public class OrderDTO
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public DateOnly OrderDate { get; set; }

    }
}
