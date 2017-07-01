using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;

namespace Sale_platform_ele.Services
{
    public class BaseSv
    {
        public SaleDBDataContext db;

        public BaseSv()
        {
            db = new SaleDBDataContext();
        }

    }
}