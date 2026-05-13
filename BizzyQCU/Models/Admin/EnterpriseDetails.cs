using System;
using System.Collections.Generic;

namespace BizzyQCU.Models.Admin
{
    public class EnterpriseDetails
    {
        public int EnterpriseId { get; set; }
        public int UserId { get; set; }
        public string StoreName { get; set; }
        public string StoreDescription { get; set; }
        public string ContactNumber { get; set; }
        public decimal? RatingAvg { get; set; }
        public string EnterpriseType { get; set; }
        public string GcashNumber { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }  // For store logo
    }

    public class EnterpriseProduct
    {
        public int ProductId { get; set; }
        public int EnterpriseId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ProductImage { get; set; }
        public string Status { get; set; }
    }
}