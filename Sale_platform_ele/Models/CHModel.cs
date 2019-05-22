using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class DeliveryInfoModel
    {
        public int id { get; set; }
        public string deliveryUnit { get; set; }
        public string attn { get; set; }
        public string phone { get; set; }
        public string addr { get; set; }
    }

    public class StockTeamReport
    {
        public int chDetailId { get; set; }
        public string billDate { get; set; }
        public string sysNo { get; set; }
        public string k3StockNo { get; set; }
        public string customerName { get; set; }
        public string billName { get; set; }
        public string productType { get; set; }
        public string itemName { get; set; }
        public string itemModel { get; set; }
        public int? applyQty { get; set; }
        public string unitName { get; set; }
        public string orderNo { get; set; }
        public string orderEntryNo { get; set; }
        public int? packs { get; set; }
        public int? cardboardNum { get; set; }
        public string deliveryNumber { get; set; }
        public string cycle { get; set; }
        public string customerPO { get; set; }
        public string customerPN { get; set; }
        public string deliveryUnit { get; set; }
        public string deliveryAddr { get; set; }
        public string hasPrint { get; set; }
        public string itemComment { get; set; }
        public string contact { get; set; }
        public string contactPhone { get; set; }
        public string boxSize { get; set; }
        public decimal? totalGrossWeight { get; set; }
    }

}