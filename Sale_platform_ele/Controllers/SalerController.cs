using Newtonsoft.Json;
using Sale_platform_ele.Filters;
using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Web;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class SalerController : BaseController
    {
        BillSv bill;
        private const string TAG = "申请者模块";

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

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
            Wlog("新建单据，billType:" + billType);

            SetBillByType(billType);
            ViewData["bill"] = bill.GetNewBill(currentUser.userId);

            return View(bill.CreateViewName);
        }

        public JsonResult SaveBill(FormCollection fc)
        {
            string sysNo=fc.Get("sys_no");
            SetBillByType(new BillUtils().GetBillEnType(sysNo));

            string result = bill.SaveBill(fc, currentUser.userId);
            Wlog("保存单据，result:" + result, sysNo, string.IsNullOrEmpty(result) ? 0 : -100);

            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result },"text/html");
            }            

            return Json(new ResultModel() { suc = true },"text/html");

        }

        [SessionTimeOutFilter]
        public ActionResult CheckBillList(string billType)
        {
            Wlog("打开单据列表视图,billType:" + billType);

            SalerSearchParamModel pm;
            var queryData = Request.Cookies["ele_sa_" + billType + "_qd"];
            if (queryData != null) {
                pm = JsonConvert.DeserializeObject<SalerSearchParamModel>(SomeUtils.DecodeToUTF8(queryData.Value));
            }
            else {
                pm = new SalerSearchParamModel();
                pm.auditResult = 0;
                pm.billType = billType;
            }
            ViewData["queryParams"] = pm;

            SetBillByType(billType);
            return View(bill.CheckListViewName);
        }

        public JsonResult GetBillList(FormCollection fc)
        {
            SalerSearchParamModel pm = new SalerSearchParamModel();
            SomeUtils.SetFieldValueToModel(fc, pm);

            var queryData = Request.Cookies["ele_sa_" + pm.billType + "_qd"];
            if (queryData == null) {
                queryData = new HttpCookie("ele_sa_" + pm.billType + "_qd");
            }
            queryData.Expires = DateTime.Now.AddDays(20);
            queryData.Value = SomeUtils.EncodeToUTF8(JsonConvert.SerializeObject(pm));
            Response.AppendCookie(queryData);

            Wlog("获取列表数据:" + JsonConvert.SerializeObject(pm));

            SetBillByType(pm.billType);
            return Json(bill.GetBillList(pm, currentUser.userId), "text/html");
        }

        public JsonResult ApplyHasBegan(string sysNo)
        {
            return Json(new ResultModel() { suc=new ApplySv().ApplyHasBegan(sysNo) });
        }

        [SessionTimeOutFilter]
        public ActionResult ModifyBill(string sysNo,int stepVersion)
        {
            Wlog("进入单据修改视图", sysNo + ":" + stepVersion);

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
            Wlog("进入单据查看视图", sysNo);

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
            Wlog("从旧模板新增", sysNo);

            SetBillBySysNo(sysNo);
            ViewData["bill"] = bill.GetNewBillFromOld();
            return View(bill.CreateViewName);
        }

        [SessionTimeOutFilter]
        public JsonResult BeginApply(string sysNo)
        {
            SetBillByType(new BillUtils().GetBillEnType(sysNo));
            if (!bill.HasOrderSaved(sysNo)) {
                Wlog("没有保存单据就提交", sysNo, -10);
                return Json(new ResultModel() { suc = false, msg = "请先保存单据再提交" });
            }

            SetBillBySysNo(sysNo);
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
                Wlog("提交失败：" + ex.Message, sysNo, -10);
                return Json(new ResultModel() { suc = false, msg = "提交失败：" + ex.Message });
            }

            Wlog("提交成功", sysNo);
            return Json(new ResultModel() { suc = true, msg = "提交成功！等待跳转..." });
        }

    }
}
