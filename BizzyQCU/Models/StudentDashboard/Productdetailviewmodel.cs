namespace BizzyQCU.Models
{
    public class ProductDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PreparationTime { get; set; }
        public byte[] ProductImage { get; set; }  // products use longblob

        public int EnterpriseId { get; set; }
        public string EnterpriseName { get; set; }
        public string EnterpriseLogo { get; set; }  // enterprises use varchar path
        public string DeliveryOptions { get; set; }
    }
}