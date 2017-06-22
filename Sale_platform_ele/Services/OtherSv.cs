using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;

namespace Sale_platform_ele.Services
{    
    public class OtherSv:BaseSv
    {
        public List<ComboResult> GetItems(string what)
        {
            var result = (from i in db.vwItems
                          where i.what == what
                          select new ComboResult()
                          {
                              name = i.fname,
                              value = i.fid
                          }).ToList();
            return result;
        }
                
    }
}