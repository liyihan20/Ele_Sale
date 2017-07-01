using Sale_platform_ele.Models;
using System.Collections.Generic;
using System.Linq;

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

        public List<ComboResult> GetCustomers(string v)
        {
            var result = (from c in db.getCustomer(v)
                          select new ComboResult()
                          {
                              name = c.customer_name,
                              value = c.customer_number
                          }).ToList();
            return result;
            
        }

        public List<ComboResult> GetClerks(string v)
        {
            var result = (from c in db.getClerk(v)
                          select new ComboResult()
                          {
                              name = c.name,
                              value = c.number
                          }).ToList();
            return result;
        }

        public decimal GetExchangeRate(string currencyNo)
        {
            decimal? rate = 0;
            db.getExchangeRate(currencyNo, ref rate);

            return (decimal)rate;
        }

        public List<ProductModel> GetProducts(string v)
        {
            var result = (from p in db.getProducts(v)
                          select new ProductModel()
                          {
                              number = p.item_no,
                              name = p.item_name,
                              model = p.item_model,
                              unitGroupId = (int)p.unit_group_id,
                              unitName = p.unit_name,
                              unitNo = p.unit_no
                          }).ToList();
            return result;
        }

        public List<ComboResult> GetUnitGroup(int groupId)
        {
            var result = (from v in db.vwProductUnits
                          where v.unit_group_id == groupId
                          select new ComboResult()
                          {
                              name = v.unit_name,
                              value = v.unit_no
                          }).ToList();
            return result;
        }
    }
}