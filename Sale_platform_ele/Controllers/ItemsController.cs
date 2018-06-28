using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class ItemsController : BaseController
    {
        public JsonResult GetDepTypes()
        {
            return Json(new DepSv().GetDepTypes());
        }

        public JsonResult GetDepNamesByType(string type)
        {
            return Json(new DepSv().GetDepsByType(type));
        }

        public JsonResult GetProcessStepName()
        {
            return Json(new ProcessSv().GetProcStepName());
        }

        public JsonResult GetUsersforCombo()
        {
            return Json(new UserSv().GetUserForCombo());
        }

        public JsonResult GetItems(string what)
        {
            return Json(new OtherSv().GetItems(what));
        }

        public JsonResult GetAuditorRelateTypes()
        {
            return Json(new ProcessSv().GetAuditorRelateTypes());
        }

        public JsonResult GetCustomers(string q)
        {
            return Json(new OtherSv().GetCustomers(q));
        }

        public JsonResult GetClerks(string q)
        {
            return Json(new OtherSv().GetClerks(q));
        }

        public JsonResult GetExchangeRate(string currencyNo)
        {
            return Json(new OtherSv().GetExchangeRate(currencyNo));
        }

        public JsonResult GetProducts(string q)
        {
            return Json(new OtherSv().GetProducts(q));
        }

        public JsonResult GetUnitGroup(int groupId)
        {
            return Json(new OtherSv().GetUnitGroup(groupId));
        }

        public JsonResult GetMUAndCommisson(decimal dealPrice, decimal cost,int taxRate, int feeRate, decimal exchangeRate, string productType, decimal qty)
        {
            MUAndCommissionModel mc = new MUAndCommissionModel();
            CommissionSv csv = new CommissionSv();
            mc.MU = csv.GetMU(dealPrice, cost,taxRate, feeRate, exchangeRate);
            mc.commissionRate = csv.GetCommissionRate(mc.MU, productType);

            if (mc.commissionRate == -1) {
                return Json(new ResultModel() { suc = false, msg = "佣金率没有维护，请联系电子市场部" });
            }

            mc.commission = csv.GetCommissionMoney(dealPrice, qty, mc.commissionRate);

            return Json(new ResultModel() { suc = true, extra = mc });
        }

        public JsonResult GetCustomerCreditInfo(string customerNo, string currencyNo)
        {
            //return Json(new { suc = false, msg = "信用额度：<span style='color:green'>8000</span>；已用额度：9000；超出额度：<span style='color:red'>1000</span>" });
            var ot = new OtherSv();
            return Json(ot.GetCustomerCreditInfo(ot.GetCustomerId(customerNo), ot.GetCurrencyId(currencyNo)));
        }

        public JsonResult GetCustomerCreditInfo2(string sysNo)
        {
            var order = (Order)new SOSv(sysNo).GetBill(0);
            return GetCustomerCreditInfo(order.customer_no, order.currency_no);
        }

        public JsonResult GetClerkCHCustomer()
        {
            return Json(new OtherSv().GetClerkCHCustomers(currentUser.userId));
        }

        public JsonResult GetDeliveryInfo(string customerNumber)
        {
            return Json(new OtherSv().GetDeliveryInfo(customerNumber));
        }

    }
}
