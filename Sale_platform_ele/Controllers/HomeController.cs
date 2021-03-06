﻿using Sale_platform_ele.Filters;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Web;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class HomeController : BaseController
    {
        [SessionTimeOutFilter]
        public ActionResult Main(string url)
        {
            Session.Clear();
            var ua = new UA(currentUser.userId);
            ViewData["url"] = string.IsNullOrEmpty(url) ? "" : SomeUtils.MyUrlDecoder(url);
            ViewData["powers"] = ua.GetUserPowers();
            ViewData["username"] = currentUser.realName;
            ViewData["depName"] = ua.GetUserDepartmentName();
            ViewData["copName"] = currentAccount.Equals("ele") ? "信利电子有限公司" : "信利仪器/工业有限公司";
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ChangeLang(string lang)
        {
            /*记录语言设置到cookies*/
            HttpCookie cookie = new HttpCookie("CoolCode_Lang", lang);
            cookie.Expires = DateTime.Now.AddMonths(1);
            Response.AppendCookie(cookie);
            /*重定向到上一个Action*/
            return new RedirectResult(this.Request.ServerVariables["HTTP_REFERER"]);
            // return RedirectToAction("Index");
        }

        //public string Test(string sn)
        //{            
        //    new CHSv(sn).DoWhenBeforeAudit(1, "cc", true, 0);

        //    return "ok";
        //}

    }
}
