using System;
using System.Web.Mvc;
using Sale_platform_ele.Models;
using Sale_platform_ele.Services;

namespace Sale_platform_ele.Controllers
{
    public class BaseController : Controller
    {
        private UserInfo _currentUser;
        public UserInfo currentUser
        {
            get
            {
                _currentUser = (UserInfo)Session["currentUser"];
                if (_currentUser == null) {                    
                    int userId = Int32.Parse(Request.Cookies["order_ele_cookie"]["userid"]);
                    var ua = new UA(userId);
                    var user = ua.GetUser();
                    _currentUser = new UserInfo()
                    {
                        userId=user.id,
                        departmentName = ua.GetUserDepartmentName(),
                        email=user.email,
                        userName=user.username,
                        realName=user.real_name
                    };
                    Session["currentUser"] = _currentUser;
                }
                return _currentUser;
            }
        }
    }
}
