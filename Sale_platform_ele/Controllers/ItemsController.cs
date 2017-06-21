using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sale_platform_ele.Services;

namespace Sale_platform_ele.BaseControllers
{
    public class ItemsController : Controller
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

    }
}
