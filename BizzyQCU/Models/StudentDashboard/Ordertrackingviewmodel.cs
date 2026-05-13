using System;
using System.Collections.Generic;

namespace BizzyQCU.Models
{
    public class OrderTrackingViewModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public string DeliveryOption { get; set; }
        public string CustomerLocation { get; set; }
        public string OrderNote { get; set; }
        public DateTime OrderDate { get; set; }
        public string EstimatedTime { get; set; }
        public string StoreName { get; set; }
        public List<OrderTrackingItem> Items { get; set; }
    }

    public class OrderTrackingItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}