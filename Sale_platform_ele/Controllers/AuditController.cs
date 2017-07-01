﻿using Newtonsoft.Json;
using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Web;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class AuditController : BaseController
    {
        private const string TAG = "审核者模块";
        public JsonResult CheckAuditStatus(string sysNo)
        {
            Wlog(TAG, "查看审核记录", sysNo);

            if (!new ApplySv().ApplyHasBegan(sysNo)) {
                return Json(new { suc = false });
            }
            ApplySv sv = new ApplySv(sysNo);
            return Json(new { suc = true, nextStepName = sv.GetNextStepName(), nextAuditors = sv.GetNextStepAudiors(),result=sv.GetAuditStatus() });
        }

        public ActionResult CheckAuditList()
        {
            Wlog(TAG, "打开审核列表视图");

            AuditSearchParamModel pm;
            var queryData = Request.Cookies["ele_au_qd"];
            if (queryData != null) {
                pm = JsonConvert.DeserializeObject<AuditSearchParamModel>(SomeUtils.DecodeToUTF8(queryData.Value));
            }
            else {
                pm = new AuditSearchParamModel();
                pm.auditResult = 0;
                pm.finalResult = 0;
            }
            ViewData["queryParams"] = pm;
            return View();
        }

        public JsonResult GetDefaultAuditList()
        {
            AuditSearchParamModel pm;
            var queryData = Request.Cookies["ele_au_qd"];
            if (queryData != null) {
                pm = JsonConvert.DeserializeObject<AuditSearchParamModel>(SomeUtils.DecodeToUTF8(queryData.Value));
            }
            else {
                pm = new AuditSearchParamModel();
                pm.auditResult = 0;
                pm.finalResult = 0;
            }
            Wlog(TAG, "自动获取默认数据：" + JsonConvert.SerializeObject(pm));

            return Json(new ApplySv().GetAuditList(currentUser.userId, pm));
        }

        public JsonResult SearchAuditList(FormCollection fc)
        {
            AuditSearchParamModel pm = new AuditSearchParamModel();
            SomeUtils.SetFieldValueToModel(fc, pm);

            var queryData = Request.Cookies["ele_au_qd"];
            if (queryData == null) {
                queryData = new HttpCookie("ele_au_qd");
            }
            queryData.Expires = DateTime.Now.AddDays(20);
            queryData.Value = SomeUtils.EncodeToUTF8(JsonConvert.SerializeObject(pm));
            Response.AppendCookie(queryData);

            Wlog(TAG, "审核列表查询：" + JsonConvert.SerializeObject(pm));

            return Json(new ApplySv().GetAuditList(currentUser.userId, pm), "text/html");
        }

        public ActionResult BeginAudit(int step, int applyId)
        {
            string info;
            try {
                info = new ApplySv().GetAuditInfo(step, applyId, currentUser.userId);
            }
            catch (Exception ex) {
                ViewBag.tip = ex.Message;
                return View("Error");
            }
            string[] infoArr=info.Split('|');

            ViewData["canEdit"] = infoArr[0];
            ViewData["sysNo"] = infoArr[1];
            ViewData["step"] = step;
            ViewData["applyId"] = applyId;

            Wlog(TAG, "进入审核页面，applyID:" + applyId + ";step:" + step + ";info:" + info);

            return View();
        }
        
        public JsonResult BlockOrder(FormCollection fc)
        {
            int applyId = int.Parse(fc.Get("applyId"));
            int step = int.Parse(fc.Get("step"));
            string comment = fc.Get("auditor_comment");

            string result = new ApplySv(applyId).BlockOrder(step, currentUser.userId, currentUser.realName, comment);
            if (!string.IsNullOrEmpty(result)) {
                
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            Wlog(TAG, string.Format("挂起操作，applyID:{0},step:{1},comment:{2},result:{3}", applyId, step, comment, result), "", string.IsNullOrEmpty(result) ? 0 : -100);

            return Json(new ResultModel() { suc = true, msg = "挂起成功" }, "text/html");

        }

        public ActionResult GetStatusResult(int step, int applyId)
        {
            return Json(new ApplySv(applyId).GetAuditResult(step, currentUser.userId));
        }

        public JsonResult HandleAudit(FormCollection fc)
        {
            int applyId = int.Parse(fc.Get("applyId"));
            int step = int.Parse(fc.Get("step"));
            string comment = fc.Get("auditor_comment");
            bool isPass = bool.Parse(fc.Get("okFlag"));

            string result = new ApplySv(applyId).HandleAudit(step, currentUser.userId, isPass, comment, GetIPAddr());
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            Wlog(TAG, string.Format("审批单据，applyID:{0},step:{1},comment:{2},isPass:{3},result:{4}", applyId, step, comment, isPass, result), "", string.IsNullOrEmpty(result) ? 0 : -100);

            return Json(new ResultModel() { suc = true, msg = "审批成功" });

        }

    }
}
