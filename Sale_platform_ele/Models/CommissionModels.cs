using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class MCommission
    {
        public int id { get; set; }
        public string productType { get; set; }
        public string userName { get; set; }
        public string createDate { get; set; }
        public string updateDate { get; set; }
        public string beginDate { get; set; }
        public string endDate { get; set; }
    }

    public class MUAndCommissionModel
    {
        public decimal MU { get; set; }
        public decimal commissionRate { get; set; }
        public decimal commission { get; set; }
    }

}