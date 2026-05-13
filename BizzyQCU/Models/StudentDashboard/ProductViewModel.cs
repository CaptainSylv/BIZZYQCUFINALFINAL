using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizzyQCU.Models.StudentDashboard
{
        public class ProductViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public string StoreName { get; set; }
            public string CategoryName { get; set; }   // from product_categories
            public int OrderCount { get; set; }        // for popularity sorting
            public byte[] ProductImage { get; set; }   // longblob from DB
        }
    }
