using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class AccountController : Controller
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
            }
            return RedirectToAction("Login");
        }
    }
}
