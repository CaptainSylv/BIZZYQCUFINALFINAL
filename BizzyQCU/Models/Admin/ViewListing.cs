using System;
using System.Collections.Generic;

namespace BizzyQCU.Models.Admin
{
    public class ViewListing
    {
        public int ProductId { get; set; }
        public int EnterpriseId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ProductImage { get; set; }
        public string Status { get; set; }
        public int ApprovalState { get; set; }
        public DateTime CreatedAt { get; set; }

        public string StoreName { get; set; }
        public string Email { get; set; }
        public decimal? RatingAvg { get; set; }
        public string EnterpriseStatus { get; set; }
    }
}
