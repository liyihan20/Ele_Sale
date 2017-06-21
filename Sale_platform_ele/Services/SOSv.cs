using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;

namespace Sale_platform_ele.Services
{
    public class SOSv:BillSv
    {
        private SaleDBDataContext db = new SaleDBDataContext();        

        public override string GetBillType()
        {            
            return "销售订单";
        }
    }
}