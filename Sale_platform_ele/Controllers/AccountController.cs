using System;
using System.Web.Mvc;

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
    }
}
