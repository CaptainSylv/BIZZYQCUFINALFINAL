using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BizzyQCU.Models.Admin
{
    public class ViewEnterprise
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
    }

    public class ChartData
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }
}