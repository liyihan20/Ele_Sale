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

    public class CHListModel
    {
        public int billId { get; set; }
        public string billDate { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public string productType { get; set; }
        public string productName { get; set; }
        public string productModel { get; set; }
        public string qty { get; set; }
        public string realQty { get; set; }
        public string orderNo { get; set; }
        public int? orderEntryNo { get; set; }
        public string auditStatus { get; set; }
        public string k3AuditDate { get; set; }
        public string k3StockNo { get; set; }
    }

    public class ClerkAndCustomerModel
    {
        public int id { get; set; }
        public string agency { get; set; }
        public int? clerkId { get; set; }
        public string clerkName { get; set; }
        public string clerkNumber { get; set; }
        public string customerNumber { get; set; }
        public string customerName { get; set; }
    }

    public class EBListModel
    {
        public int billId { get; set; }
        public string billDate { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public string productName { get; set; }
        public string productModel { get; set; }
        public int qty { get; set; }
        public decimal dealPrice { get; set; }
        public string auditStatus { get; set; }
    }

    public class EOListModel
    {
        public string account { get; set; }
        public int billId { get; set; }
        public string applyDate { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public string productName { get; set; }
        public string productModel { get; set; }
        public int qty { get; set; }
        public decimal taxedPrice { get; set; }
        public string auditStatus { get; set; }
    }

    public class BLListModel
    {
        public int billId { get; set; }
        public string billDate { get; set; }
        public string sysNo { get; set; }
        public string customerName { get; set; }
        public string productName { get; set; }
        public string productModel { get; set; }
        public int qty { get; set; }
        public string auditStatus { get; set; }
    }



}