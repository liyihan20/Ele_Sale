using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class ComboResult
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ResultModel
    {
        public bool suc { get; set; }
        public string msg { get; set; }
        public object extra { get; set; }
    }

    public class TreeModel
    {
        public int id { get; set; }
        public string text { get; set; }
        public string state { get; set; }
        public string iconCls { get; set; }
    }

    public class ProductModel
    {
        public string number { get; set; }
        public string name { get; set; }
        public string model { get; set; }
        public string unitNo { get; set; }
        public string unitName { get; set; }
        public int unitGroupId { get; set; }

    }

    public class AuditSearchParamModel
    {
        public string sysNo { get; set; }
        public string saler { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string proModel { get; set; }
        public int auditResult { get; set; }
        public int finalResult { get; set; }
    }

    public class SalerSearchParamModel
    {
        public string billType { get; set; }
        public string searchValue { get; set; }
        public string itemModel { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int auditResult { get; set; }
        public string customerName { get; set; }
        public string productType { get; set; }
        public string saleRange { get; set; }
        
    }

    public class ProductPriceModel
    {
        public decimal? unitPrice { get; set; }
        public decimal? dealPrice { get; set; }
        public decimal? cost { get; set; }
        public int? feeRate { get; set; }

    }

    public class StepNameAndAuditorModel
    {
        public string stepName { get; set; }
        public string auditorName { get; set; }
        public DateTime? auditTime { get; set; }
    }
}