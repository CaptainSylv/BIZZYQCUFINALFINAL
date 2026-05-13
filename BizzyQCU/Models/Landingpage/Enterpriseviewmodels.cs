using System.Collections.Generic;

namespace BizzyQCU.Models
{
    public class EnterpriseListItem
    {
        public int EnterpriseId { get; set; }
        public string StoreName { get; set; }
        public string EnterpriseType { get; set; }
        public string Description { get; set; }
        public string StoreLogo { get; set; }  // varchar path e.g. /Content/Images/logos/x.jpg
        public int ProductCount { get; set; }
        public double AvgRating { get; set; }
        public string Status { get; set; }
    }

    public class EnterpriseDetailViewModel
    {
        public int EnterpriseId { get; set; }
        public string StoreName { get; set; }
        public string EnterpriseType { get; set; }
        public string Description { get; set; }
        public string StoreLogo { get; set; }  // varchar path
        public string ContactNumber { get; set; }
        public string GcashNumber { get; set; }
        public double AvgRating { get; set; }
        public int RatingCount { get; set; }
        public string DeliveryOptions { get; set; }
        public string Status { get; set; }
        public List<EnterpriseProductItem> Products { get; set; }
    }

    public class EnterpriseProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public byte[] ProductImage { get; set; }  // products still use longblob
        public int PreparationTime { get; set; }
        public string CategoryName { get; set; }
    }
}