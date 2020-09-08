using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using System;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class BaseController : Controller
    {
        
        private UserInfo _currentUser;
        private string _currentAccount;
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
                        userId = user.id,
                        departmentName = ua.GetUserDepartmentName(),
                        email = user.email,
                        userName = user.username,
                        realName = user.real_name
                    };
                    Session["currentUser"] = _currentUser;
                }
                return _currentUser;
            }
        }

        public string currentAccount
        {
            get
            {
                _currentAccount = (string)Session["currentAccount"];
                if (string.IsNullOrEmpty(_currentAccount)) {
                    _currentAccount = Request.Cookies["order_ele_cookie"]["cop"];
                    Session["currentAccount"] = _currentAccount;
                }
                return _currentAccount;
            }
        }

        public string GetIPAddr()
        {
            return Request.UserHostAddress;
        }

        public void Wlog(string tag, string log , string sysNo="", int unusual = 0)
        {
            new BaseSv().WriteEventLog(new EventLog()
            {
                sysNum = sysNo,
                username = currentUser.realName,
                model = tag,
                ip = GetIPAddr(),
                @event = log,
                op_time = DateTime.Now,
                unusual = unusual
            });
        }

    }
}
