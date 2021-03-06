﻿using System;
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

    public class EqmChListModel
    {
        public int FId { get; set; }
        public int FDetailId { get; set; }
        public string FDate { get; set; }
        public string FSysNo { get; set; }
        public string FBillNo { get; set; }
        public string FCustomerName { get; set; }
        public string FDeliveryUnit { get; set; }
        public string FItemName { get; set; }
        public string FItemModel { get; set; }
        public int? FQty { get; set; }
        public string FUnitName { get; set; }
        public decimal? FPrice { get; set; }
        public decimal? FAmount { get; set; }
        public string FUserName { get; set; }
        public string FDeliveryNum { get; set; }
    }

    public class ApplierConfirmCHInfo
    {
        public string sys_no { get; set; }
        public string delivery_unit { get; set; }
        public string delivery_attn { get; set; }
        public string delivery_phone { get; set; }
        public string delivery_addr { get; set; }
        public List<UpdateCHRowInfo> rows { get; set; }
    }

    public class UpdateCHRowInfo
    {
        public int detail_id { get; set; }
        public int real_qty { get; set; }

    }
}