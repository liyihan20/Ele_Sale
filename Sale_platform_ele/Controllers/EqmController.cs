using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_platform_ele.Services;
using Sale_platform_ele.Models;
using Sale_platform_ele.Filters;

namespace Sale_platform_ele.Controllers
{
    public class EqmController : BaseController
    {
        private const string TAG = "工业/仪器送货单";

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

        [SessionTimeOutFilter]
        public ActionResult CheckDeliveryBills(string account)
        {
            ViewData["account"] = account;
            return View();
        }

        public JsonResult SearchDeliveryBills(FormCollection fc)
        {
            string account = fc.Get("account");
            string fromDate = fc.Get("fromDate");
            string toDate = fc.Get("toDate");
            string itemModel = fc.Get("itemModel");
            string stockNo = fc.Get("stockNo");
            string customer = fc.Get("customer");

            DateTime fromDateDt, toDateDt;
            if (!DateTime.TryParse(fromDate, out fromDateDt)) {
                fromDateDt = DateTime.Now.AddDays(-3);
            }
            if (!DateTime.TryParse(toDate, out toDateDt)) {
                toDateDt = DateTime.Now.AddDays(1);
            }
            else {
                toDateDt = toDateDt.AddDays(1);
            }

            Wlog(string.Format("account:{0},fromDate:{1:yyyy-MM-dd},toDate:{2:yyyy-MM-dd},model:{3},stockNo:{4},customer:{5}", account, fromDateDt, toDateDt, itemModel, stockNo, customer));

            try {
                var result = new EqmSv().SearchDeliveryBills(account, stockNo, fromDateDt, toDateDt, customer, itemModel);
                return Json(new { suc = true, result = result });
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }

        }

        [SessionTimeOutFilter]
        public ActionResult AddDeliveryBill(string account)
        {
            ViewData["account"] = account;
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult ModifyDeliveryBill(string sysNo)
        {
            var bill = new EqmSv().GetDeliveryBill(sysNo);
            if (bill == null) {
                ViewBag.tip = "此流水号不存在";
                return View("Error");
            }
            ViewData["bill"] = bill;
            ViewData["account"] = bill.FAccount;
            return View("AddDeliveryBill");
        }

        public JsonResult GetK3StockBill(string account, string billNo)
        {
            var sv = new EqmSv();
            List<getK3EqmChDataResult> list;
            try {
                list = sv.GetK3StockBill(account, billNo);                
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }
            if (list.Count() < 1) {
                return Json(new { suc = false, msg = "在K3找不到此销售出库单号" });
            }

            var h = list.First();
            var bill = new Sale_eqm_ch_bill();
            bill.FAccount = account;
            bill.FBillNo = billNo;
            bill.FDate = h.FDate;
            bill.FCustomerName = h.FCustomerName;
            bill.FCustomerNumber = h.FCustomerNumber;
            bill.FUserName = currentUser.realName;

            var details = new List<Sale_eqm_ch_bill_detail>();
            foreach (var l in list) {
                var d = new Sale_eqm_ch_bill_detail();
                d.FAmount = l.FConsignAmount;
                d.FIndex = l.FEntryID;
                d.FItemModel = l.FModel;
                d.FItemName = l.FName;
                d.FItemNumber = l.FNumber;
                d.FPrice = l.FConsignPrice;
                d.FQty = (int)l.FAuxQty;
                d.FUnitName = l.FUnitName;

                details.Add(d);
            }

            var deliveryInfo = sv.GetDeliveryInfo(bill.FCustomerNumber);

            Wlog(string.Format("获取出库单信息，account:{0},billNo:{1}", account, billNo));

            return Json(new { suc = true, bill = bill, details = details, deliveryInfo = deliveryInfo });

        }

        public JsonResult GetDeliveryInfo(string customerNumber)
        {
            var deliveryInfo = new EqmSv().GetDeliveryInfo(customerNumber);
            return Json(new { suc = true, deliveryInfo = deliveryInfo });
        }

        public JsonResult SaveDeliveryBill(FormCollection fc)
        {
            try {
                new EqmSv().SaveDeliveryBill(fc,currentUser);
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }
            Wlog("保存送货单");
            return Json(new { suc = true });
        }

        [SessionTimeOutFilter]
        public ActionResult PrintDeliveryBill(string sysNo,bool seePrice)
        {
            var bill = new EqmSv().GetDeliveryBill(sysNo);
            if (bill == null) {
                ViewBag.tip = "此流水号不存在";
                return View("Error");
            }
            Wlog("打印送货单,可看金额：" + seePrice.ToString(), sysNo);

            ViewData["bill"] = bill;
            ViewData["seePrice"] = seePrice;            
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult CheckADeliveryBill(string sysNo)
        {
            var bill = new EqmSv().GetDeliveryBill(sysNo);
            if (bill == null) {
                ViewBag.tip = "此流水号不存在";
                return View("Error");
            }            
            ViewData["bill"] = bill;

            Wlog("查看送货单", sysNo);

            return View();
        }

        public void ExportDeliveryExcel(string FIds)
        {
            Wlog("导出送货单excel：" + FIds);
            int[] idArr = FIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(v => int.Parse(v)).Distinct().ToArray();
            new EqmSv().ExportExcelData(idArr);

        }

        public JsonResult DeletedDeliveryBill(string sysNo)
        {
            Wlog("删除送货单", sysNo);
            try {
                new EqmSv().DeletedDeliveryBill(sysNo);
                return Json(new { suc = true, msg = "删除成功" });
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }
        }

        public JsonResult SaveDeliveryNum(string sysNo, string deliveryNum)
        {
            try {
                new EqmSv().SaveDeliveryNum(sysNo, deliveryNum);
            }
            catch (Exception ex) {
                return Json(new { suc = false, msg = ex.Message });
            }
            return Json(new { suc = true });
        }

    }
}
