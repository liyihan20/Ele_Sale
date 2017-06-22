using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using Newtonsoft.Json;

namespace Sale_platform_ele.Services
{
    
    public class CommissionSv:BaseSv
    {        
        public List<MCommission> GetCommissionList()
        {
            var list = (from c in db.CommissionRate
                        orderby c.end_date descending
                        select new MCommission()
                        {
                            id = c.id,
                            userName = c.user_name,
                            productType = c.product_type,
                            beginDate = DateTime.Parse(c.begin_date.ToString()).ToString("yyyy-MM-dd"),
                            endDate = DateTime.Parse(c.end_date.ToString()).ToString("yyyy-MM-dd"),
                            createDate = DateTime.Parse(c.create_date.ToString()).ToString("yyyy-MM-dd HH:mm"),
                            updateDate = DateTime.Parse(c.update_date.ToString()).ToString("yyyy-MM-dd HH:mm")
                        }).ToList();
            return list;
        }

        public CommissionRate GetSinggleCommission(int id)
        {
            return db.CommissionRate.Single(c => c.id == id);
        }

        public string SaveCommission(CommissionRate cr,int userId)
        {
            if (db.CommissionRate.Where(c => c.id != cr.id && c.end_date > cr.begin_date && c.begin_date < cr.end_date && c.product_type == cr.product_type).Count() > 0) {
                return "此时间段与之前设置的时间段有重叠，保存失败";
            }
            try {
                if (cr.id != 0) {
                    //更新，将旧的删除
                    CommissionRate existed = db.CommissionRate.Single(c => c.id == cr.id);
                    BackupData bd = new BackupData();
                    bd.user_id = userId;
                    bd.sys_no = "佣金率维护";
                    bd.op_date = DateTime.Now;
                    bd.main_data = SomeUtils.ModelToString<CommissionRate>(existed);
                    bd.secondary_data = SomeUtils.ModelsToString<CommissionRateDetail>(existed.CommissionRateDetail.ToList());
                    db.BackupData.InsertOnSubmit(bd);

                    db.CommissionRateDetail.DeleteAllOnSubmit(existed.CommissionRateDetail);
                    db.CommissionRate.DeleteOnSubmit(existed);
                }

                db.CommissionRate.InsertOnSubmit(cr);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }
            
            return "";
        }

        public string RemoveCommission(int id,int userId)
        {
            try {
                CommissionRate existed = db.CommissionRate.Single(c => c.id == id);
                BackupData bd = new BackupData();
                bd.user_id = userId;
                bd.sys_no = "删除佣金率";
                bd.op_date = DateTime.Now;
                bd.main_data = SomeUtils.ModelToString<CommissionRate>(existed);
                bd.secondary_data = SomeUtils.ModelsToString<CommissionRateDetail>(existed.CommissionRateDetail.ToList());
                db.BackupData.InsertOnSubmit(bd);

                db.CommissionRateDetail.DeleteAllOnSubmit(existed.CommissionRateDetail);
                db.CommissionRate.DeleteOnSubmit(existed);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public decimal GetMU(decimal price, decimal cost, int feeRate, decimal exchangeRate)
        {
            if (price == 0 || exchangeRate == 0) return 0;
            return 100 * (1 - cost / (price * exchangeRate)) - feeRate;
        }

        public decimal GetCommissionRate(decimal MU, string productType)
        {
            var result = (from cr in db.CommissionRate
                          from crd in cr.CommissionRateDetail
                          where cr.product_type.Contains(productType)
                          && cr.begin_date <= DateTime.Now
                          && cr.end_date > DateTime.Now.AddDays(-1)
                          && crd.MU <= MU
                          orderby crd.MU descending
                          select crd).ToList();
            if (result.Count() == 0) {
                return 0;
            }
            else {
                return (decimal)result.First().rate_value/100;
            }
        }

        public decimal GetCommissionMoney(decimal price,decimal qty, int commissionRate)
        {
            return price * qty * commissionRate;
        }

        
    }


}