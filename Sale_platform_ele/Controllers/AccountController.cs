using System;
using System.Web.Mvc;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;

namespace Sale_platform_ele.Controllers
{
    public class AccountController : BaseController
    {
        //
        // GET: /Account/

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult LogOut()
        {            
            Session.Clear();
            var cookie = Request.Cookies["order_ele_cookie"];
            if (cookie != null) {                
                cookie.Expires = DateTime.Now.AddSeconds(-1);
                Response.AppendCookie(cookie);
                Wlog("登出模块", "成功登出");
            }            
            return RedirectToAction("Login");
        }

        //跳转到光电和半导体CRM
        public ActionResult JumpToOtherCrm(string company, string url)
        {
            string userName = new UA(currentUser.userId).GetUser().username;
            string code = SomeUtils.getMD5(userName);
            url = Uri.UnescapeDataString(url);

            if ("op".Equals(company)) {
                url = Url.Content("~/../SaleOrder/") + url;
                url = Uri.EscapeDataString(url);
                return Redirect(Url.Content("~/../SaleOrder/Account/DirectFromEle?userName=") + userName + "&code=" + code + "&url=" + url);
            }
            else if ("semi".Equals(company)) {
                url = Url.Content("~/../SaleOrder_semi/") + url;
                url = Uri.EscapeDataString(url);
                return Redirect(Url.Content("~/../SaleOrder_semi/Account/DirectFromEle?userName=") + userName + "&code=" + code + "&url=" + url);
            }
            return View("Error");            

        }

    }
}
