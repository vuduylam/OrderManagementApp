﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OrderManagementApp.Models
{
    public class OrderDetail
    {
        [Key] [Required] [Column("order_detail_id")]
        public int OrderDetailId { get; set; }

        [ForeignKey("Order")] [Column("order_id")]
        public int OrderId { get; set; }

        [ForeignKey("Product")] [Column("product_id")]
        public int ProductId { get; set; }
        
        [Column("quantity")]
        public int Quantity { get; set; }

        //Navigation properties
        [JsonIgnore]
        public Order? Order { get; set; }
        [JsonIgnore]
        public Product? Product { get; set; }
    }
}
