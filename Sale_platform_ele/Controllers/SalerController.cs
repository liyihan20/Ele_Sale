using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using Sale_platform_ele.Services;
using Sale_platform_ele.Filters;
using Newtonsoft.Json;

namespace Sale_platform_ele.Controllers
{
    public class SalerController : BaseController
    {
        BillSv bill;

        /// <summary>
        /// 根据单据类型设置单据空的对象实例
        /// </summary>
        /// <param name="billType">单据类型</param>
        private void SetBillByType(string billType)
        {
            bill = (BillSv)new BillUtils().GetBillSvInstance(billType);
        }

        /// <summary>
        /// 根据流水号设置单据对象实例
        /// </summary>
        /// <param name="sysNo">流水号</param>
        private void SetBillBySysNo(string sysNo)
        {            
            bill = (BillSv)new BillUtils().GetBillSvInstanceBySysNo(sysNo);
        }

        [SessionTimeOutFilter]
        public ActionResult CreateBill(string billType)
        {
            SetBillByType(billType);
            ViewData["bill"] = bill.GetNewBill(currentUser.userId);

            return View(bill.CreateViewName);
        }

        public JsonResult SaveBill(FormCollection fc)
        {
            SetBillByType(new BillUtils().GetBillEnType(fc.Get("sys_no")));

            string result = bill.SaveBill(fc, currentUser.userId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result },"text/html");
            }
            return Json(new ResultModel() { suc = true },"text/html");

        }

        [SessionTimeOutFilter]
        public ActionResult CheckBillList(string billType)
        {
            
            SalerSearchParamModel pm;
            var queryData = Request.Cookies["ele_sa_qd"];
            if (queryData != null) {
                pm = JsonConvert.DeserializeObject<SalerSearchParamModel>(SomeUtils.DecodeToUTF8(queryData.Value));
            }
            else {
                pm = new SalerSearchParamModel();
                pm.auditResult = 0;
            }
            ViewData["queryParams"] = pm;
            ViewData["billType"] = billType;

            SetBillByType(billType);
            return View(bill.CheckListViewName);
        }

        public JsonResult GetBillList(FormCollection fc)
        {
            SalerSearchParamModel pm = new SalerSearchParamModel();
            SomeUtils.SetFieldValueToModel(fc, pm);

            var queryData = Request.Cookies["ele_sa_qd"];
            if (queryData == null) {
                queryData = new HttpCookie("ele_sa_qd");
            }
            queryData.Expires = DateTime.Now.AddDays(20);
            queryData.Value = SomeUtils.EncodeToUTF8(JsonConvert.SerializeObject(pm));
            Response.AppendCookie(queryData);            

            SetBillByType(fc.Get("bill_type"));
            return Json(bill.GetBillList(pm, currentUser.userId),"text/html");
        }

        public JsonResult ApplyHasBegan(string sysNo)
        {
            return Json(new ResultModel() { suc=new ApplySv().ApplyHasBegan(sysNo) });
        }

        [SessionTimeOutFilter]
        public ActionResult ModifyBill(string sysNo,int stepVersion)
        {
            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetBill(stepVersion);
            if (new ApplySv().ApplyHasBegan(sysNo)) {
                ViewData["blockInfo"] = new ApplySv(sysNo).GetBlockInfo();
            }
            return View(bill.CreateViewName);
        }

        [SessionTimeOutFilter]
        public ActionResult CheckBill(string sysNo)
        {
            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetBill(0);
            if (new ApplySv().ApplyHasBegan(sysNo)) {
                ViewData["blockInfo"] = new ApplySv(sysNo).GetBlockInfo();
            }
            return View(bill.CheckViewName);
        }

        [SessionTimeOutFilter]
        public ActionResult CreateNewBillFromOld(string sysNo)
        {
            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetNewBillFromOld();
            return View(bill.CreateViewName);
        }

        [SessionTimeOutFilter]
        public JsonResult BeginApply(string sysNo)
        {
            SetBillBySysNo(sysNo);
            if (!bill.HasOrderSaved(sysNo)) {
                return Json(new ResultModel() { suc = false, msg = "请先保存单据再提交" });
            }
            
            try {
                new ApplySv().BeginApply(
                        bill.BillType,
                        currentUser.userId,
                        currentUser.realName,
                        GetIPAddr(),
                        sysNo,
                        bill.GetProductModel(),
                        new ProcessSv().GetProcessByNo(bill.GetProcessNo()),
                        bill.GetProcessDic()
                        );
            }
            catch (Exception ex) {
                return Json(new ResultModel() { suc = false, msg = "提交失败：" + ex.Message });
            }

            return Json(new ResultModel() { suc = true, msg = "提交成功！等待跳转..." });
        }

    }
}
