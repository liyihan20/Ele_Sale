using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_platform_ele.Services;
using Sale_platform_ele.Filters;

namespace Sale_platform_ele.Controllers
{
    public class CHSPController : BaseController
    {

        private const string TAG = "额外出货模块";

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

        [SessionTimeOutFilter]
        public ActionResult ClerkAndCustomer()
        {
            return View();
        }

        public ActionResult GetCleckAndCustomerList(int page, int rows, string value="")
        {
            Wlog("查询营业客户对应关系：" + value);
            var list = new CHSv().GetCleckAndCustomerList(value);
            int total = list.Count();

            list = list.Skip((page - 1) * rows).Take(rows).ToList();
            return Json(new { rows = list, total = total });
        }

        public JsonResult SaveClerkAndCustomer(FormCollection fc)
        {
            string clerkId = fc.Get("clerk_id");
            string customerNumber = fc.Get("customer_number");
            string customerName = fc.Get("customer_name");
            int clerkIdInt;
            if (!Int32.TryParse(clerkId, out clerkIdInt)) {
                return Json(new { suc = false, msg = "营业不存在" },"text/html");
            }

            if (string.IsNullOrEmpty(clerkId) || string.IsNullOrEmpty(customerNumber)) {
                return Json(new { suc = false, msg = "请完善信息后再保存" }, "text/html");
            }
            string result = new CHSv().SaveClerkAndCustomer(clerkIdInt, customerName, customerNumber);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new { suc = false, msg = result }, "text/html");
            }
            Wlog("保存客户与营业的关系：" + clerkId + ";" + customerName);
            return Json(new { suc = true }, "text/html");
        }

        public JsonResult UpdateClerkAndCustomer(int id, FormCollection fc)
        {
            string clerkId = fc.Get("clerk_id");
            string customerNumber = fc.Get("customer_number");
            string customerName = fc.Get("customer_name");
            int clerkIdInt;
            if (!Int32.TryParse(clerkId, out clerkIdInt)) {
                return Json(new { suc = false, msg = "营业不存在" }, "text/html");
            }
            if (string.IsNullOrEmpty(clerkId) || string.IsNullOrEmpty(customerNumber)) {
                return Json(new { suc = false, msg = "请完善信息后再保存" }, "text/html");
            }
            string result = new CHSv().UpdateClerkAndCustomer(id, clerkIdInt, customerName, customerNumber);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new { suc = false, msg = result }, "text/html");
            }
            Wlog("更新客户与营业的关系:" + clerkId + ";" + customerName);
            return Json(new { suc = true }, "text/html");
        }

        public JsonResult RemoveClerkAndCustomer(int id)
        {
            string result = new CHSv().RemoveClerkAndCustomer(id);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new { suc = false, msg = result });
            }
            Wlog("删除客户与营业关系：" + id);
            return Json(new { suc = true, msg = "删除成功" });
        }

        public JsonResult CheckOrderStockInfo(FormCollection fc)
        {
            string customerNumber = fc.Get("customerNumber");
            string saleStyle = fc.Get("saleStyle") ?? "";
            string productType = fc.Get("productType") ?? "";
            string fromDate = fc.Get("fromDate");
            string toDate = fc.Get("toDate");
            string orderNumber = fc.Get("orderNumber") ?? "";
            string productModel = fc.Get("productModel") ?? "";
            string hasStockQty = fc.Get("hasStockQty") ?? "10"; //是否有库存，1表示有库存，0表示无库存，其它表示不筛选
            string isClosed = fc.Get("isClosed") ?? "0"; //是否关闭，1表示已关闭，0表示没关闭，其他表示不筛选

            DateTime fromDateDT, toDateDT;
            if (!DateTime.TryParse(fromDate, out fromDateDT)) {
                fromDateDT = DateTime.Parse("1990-9-9");
            }
            if (!DateTime.TryParse(toDate, out toDateDT)) {
                toDateDT = DateTime.Parse("2099-9-9");
            }
            Wlog("查询订单库存信息");
            return Json(new CHSv().GetOrderStockInfo(customerNumber, saleStyle, productType, fromDateDT, toDateDT, orderNumber, productModel, hasStockQty, isClosed));
        }

        [SessionTimeOutFilter]
        public ActionResult K3OrderInfo()
        {
            return View();
        }

        public JsonResult GetK3StockInfo(int orderId, int orderEntryId)
        {
            return Json(new CHSv().GetK3StockInfo(orderId, orderEntryId));
        }

        [SessionTimeOutFilter]
        public ActionResult StockTeamReport()
        {
            return View();
        }

        public JsonResult GetStockTeamReport(FormCollection fc)
        {
            string sysNo = fc.Get("sysNo") ?? "";
            string customer = fc.Get("customer") ?? "";
            string stockNo = fc.Get("stockNo") ?? "";
            string orderNo = fc.Get("orderNo") ?? "";
            string fromDateStr = fc.Get("fromDate");
            string toDateStr = fc.Get("toDate");
            string model = fc.Get("productModel") ?? "";

            DateTime fromDate, toDate;
            if (!DateTime.TryParse(fromDateStr, out fromDate)) {
                return Json(new { suc = false, msg = "出库开始日期不合法" }, "text/html");
            }
            if (!DateTime.TryParse(toDateStr, out toDate)) {
                return Json(new { suc = false, msg = "出库结束日期不合法" }, "text/html");
            }
            toDate = toDate.AddDays(1);

            if (fromDate > toDate) {
                return Json(new { suc = false, msg = "出库开始日期不能大于结束日期" }, "text/html");
            }

            var result=new CHSv().GetStockTeamReport(sysNo, stockNo, orderNo, customer,model, fromDate, toDate, currentUser.userId);
            if (result.Count() == 0) {
                return Json(new { suc = false, msg = "查不到符合条件的记录" }, "text/html");
            }

            Wlog("查看出货组报表");
            return Json(new { suc = true, result = result }, "text/html");

        }

        //更新件数/叉板数/快递单号等信息
        public JsonResult UpdateStockPackInfo(int detailId, string deliveryNumber, int cardboardNum, int packs, string cycle, string itemComment,string boxSize,decimal totalGrossWeight)
        {
            var result = new CHSv().UpdateCHPackInfo(detailId, deliveryNumber, cardboardNum, packs, cycle, itemComment, boxSize, totalGrossWeight);
            if (string.IsNullOrEmpty(result)) {
                Wlog("更新快递单号等额外信息:" + detailId);
                return Json(new { suc = true });
            }
            else {
                return Json(new { suc = false, msg = result });
            }
        }

        

    }
}
