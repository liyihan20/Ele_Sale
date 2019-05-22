﻿using Sale_platform_ele.Models;
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

        public int GetCustomerId(string customerNo)
        {
            var customers = db.getCustomer(customerNo).ToList();
            if (customers.Count() > 0) {
                return customers.First().customer_id;
            }
            return 0;
        }

        public int GetCurrencyId(string currencyNo)
        {
            var currencies=db.vwItems.Where(v=>v.what=="currency" && v.fid==currencyNo).ToList();
            if (currencies.Count() > 0) {
                return currencies.First().interid;
            }
            return 0;

        }

        //获取客户信用是否超过额度
        public ResultModel GetCustomerCreditInfo(int customerId, int currencyId)
        {
            var result = db.getCustomerCreditInfo(customerId, currencyId).First();
            return new ResultModel() { suc = (result.suc == 1 ? true : false), msg = result.msg };
        }

        //获取有权限的出货客户
        public List<ComboResult> GetClerkCHCustomers(int userId)
        {
            var result = (from v in db.vwClerkAndCustomer
                          where v.clerkId == userId 
                          orderby v.customerNumber
                          select new ComboResult()
                          {
                              value = v.customerNumber,
                              name = v.customerName
                          }).Distinct().ToList();
            return result;
        } 

        //获取出货客户的地址等信息
        public List<DeliveryInfoModel> GetDeliveryInfo(string customerNumber)
        {
            var result = (from d in db.getDeliveryUnitInfo(customerNumber)
                          orderby d.id descending
                          select new DeliveryInfoModel()
                          {
                              id = d.id,
                              deliveryUnit = d.delivery_unit,
                              addr = d.delivery_addr,
                              attn = d.attn,
                              phone = d.phone
                          }).ToList();
            return result;
        }

        //营业选择了产品之后，带出上一次下单的信息
        public ProductPriceModel GetProductPriceInfo(int userId, string itemNo)
        {
            var result = (from o in db.Order
                          join e in db.OrderDetail on o.id equals e.order_id
                          where o.original_id == userId
                          && e.item_no == itemNo
                          orderby e.id descending
                          select new ProductPriceModel()
                          {
                              unitPrice = e.unit_price,
                              dealPrice = e.deal_price,
                              cost = e.cost,
                              feeRate = e.fee_rate
                          }).FirstOrDefault();
            return result;
        }

    }
}