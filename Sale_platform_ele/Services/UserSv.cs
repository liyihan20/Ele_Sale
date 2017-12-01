using Sale_platform_ele.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Sale_platform_ele.Utils;

namespace Sale_platform_ele.Services
{
    public class UserSv:BaseSv
    {
        public List<UsersInfo> GetUsers(string searchValue)
        {
            var result = (from u in db.User
                          join d in db.Department on u.department_no equals d.dep_no
                          where d.dep_type=="部门"
                          && (u.real_name.Contains(searchValue)
                          || u.username.Contains(searchValue)
                          || d.name.Contains(searchValue))
                          select new UsersInfo()
                          {
                              id = u.id,
                              userName = u.username,
                              realName = u.real_name,
                              email = u.email,
                              depName = d.name,
                              isForbit = u.is_forbit ? "Y" : "N",
                              forbitDateDT = u.forbit_date,
                              registerDateDT = u.in_date,
                              forbitReason = u.forbit_reason,
                              lastLoginDateDT = u.last_login_date
                          }).ToList();
            return result;
        }

        public string SaveUser(UsersInfo uInfo)
        {
            if (db.User.Where(u => u.username == uInfo.userName).Count() > 0) {
                return "用户名已存在，保存失败";
            }
            try {
                User user = new User()
                    {
                        username = uInfo.userName,
                        real_name = uInfo.realName,
                        email = uInfo.email,
                        department_no = db.Department.Single(d => d.name == uInfo.depName && d.dep_type == "部门").dep_no,
                        is_forbit = false,
                        in_date = DateTime.Now,
                        error_times = 0,
                        password=SomeUtils.getMD5("000000")
                    };
                db.User.InsertOnSubmit(user);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string UpdateUser(UsersInfo uInfo)
        {
            if (db.User.Where(u => u.username == uInfo.userName && u.id != uInfo.id).Count() > 0) {
                return "用户名已存在，保存失败";
            }
            try {
                User user = db.User.Single(u => u.id == uInfo.id);
                user.username = uInfo.userName;
                user.real_name = uInfo.realName;
                user.email = uInfo.email;
                user.department_no = db.Department.Single(d => d.name == uInfo.depName && d.dep_type == "部门").dep_no;
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string ResetPassword(int userId,string newPassword)
        {
            try {
                var user = db.User.Single(u => u.id == userId);
                user.password = newPassword;
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string ToggleUser(int userId)
        {
            try {
                var user = db.User.Single(u => u.id == userId);
                if (user.is_forbit) {
                    user.is_forbit = false;
                    user.error_times = 0;
                    user.forbit_date = null;
                    user.forbit_reason = "";
                    user.last_login_date = DateTime.Now;
                }
                else {
                    user.is_forbit = true;
                    user.forbit_reason = "后台手工禁用";
                    user.forbit_date = DateTime.Now;
                }
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<ComboResult> GetUserForCombo()
        {
            var list = (from u in db.User
                        select new ComboResult()
                        {
                            value = u.id.ToString(),
                            name = u.real_name
                        }).ToList();
            return list;
        }
                        
    }
}