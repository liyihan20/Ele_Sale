using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class SOListModel
    {
        public int orderId { get; set; }
        public string orderDate { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public string productName { get; set; }
        public string productModel { get; set; }
        public string qty { get; set; }
        public string dealPrice { get; set; }
        public string auditStatus { get; set; }
    }
}