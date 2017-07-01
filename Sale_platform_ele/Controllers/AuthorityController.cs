using Newtonsoft.Json;
using Sale_platform_ele.Filters;
using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class AuthorityController : BaseController
    {        
        #region 部门管理
        [SessionTimeOutFilter]
        public ActionResult Departments()
        {
            return View();
        }

        public JsonResult GetDepartments(int page, int rows, string value = "")
        {
            var list = new DepSv().GetDepartments(value);
            int total = list.Count();
            list = list.Skip((page - 1) * rows).Take(rows).ToList();
            return Json(new { rows = list, total = total });
        }

        public JsonResult SaveDepartment(FormCollection col)
        {
            string depName = col.Get("depName");
            string depType = col.Get("depType");
            string result = new DepSv().SaveDepartment(depType, depName);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            return Json(new ResultModel() { suc = true }, "text/html");

        }

        public JsonResult UpdateDepartment(int id, FormCollection col)
        {
            string depName = col.Get("depName");
            string depType = col.Get("depType");

            string result = new DepSv().UpdateDepartment(id, depType, depName);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            return Json(new ResultModel() { suc = true }, "text/html");
        }               

        public JsonResult RemoveDep(int depId)
        {
            string result = new DepSv().RemoveDepartment(depId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            return Json(new ResultModel() { suc = true, msg = "删除成功" });
        }

        #endregion

        #region 用户管理

        [SessionTimeOutFilter]
        public ActionResult Users()
        {
            return View();
        }

        public JsonResult GetUsers(int page, int rows, string searchValue = "")
        {
            var users = new UserSv().GetUsers(searchValue);
            int total = users.Count();
            users = users.Skip((page - 1) * rows).Take(rows).ToList();
            return Json(new { rows = users, total = total });
        }

        public JsonResult SaveUser(FormCollection col)
        {
            UsersInfo uInfo = new UsersInfo();
            SomeUtils.SetFieldValueToModel(col, uInfo);
            uInfo.password = SomeUtils.getMD5("000000");
            string result = new UserSv().SaveUser(uInfo);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }

            return Json(new ResultModel() { suc = true }, "text/html");
        }

        public JsonResult UpdateUser(int id, FormCollection col)
        {
            UsersInfo uInfo = new UsersInfo();
            SomeUtils.SetFieldValueToModel(col, uInfo);
            uInfo.id = id;
            string result = new UserSv().UpdateUser(uInfo);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            return Json(new ResultModel() { suc = true }, "text/html");
        }

        public JsonResult ResetPassword(int userId)
        {
            string result = new UserSv().ResetPassword(userId, SomeUtils.getMD5("000000"));
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel() { suc = true });
        }

        public JsonResult ToggleUser(int userId)
        {
            string result = new UserSv().ToggleUser(userId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel(){ suc = true });
        }

        #endregion

        #region 组管理

        [SessionTimeOutFilter]
        public ActionResult Groups()
        {
            return View();
        }

        public JsonResult GetGroups()
        {
            return Json(new { rows = new GroupSv().GetAllGroups() });
        }

        public JsonResult SaveGroup(FormCollection col)
        {
            string name = col.Get("groupName");
            string description = col.Get("description");

            string result = new GroupSv().SaveGroup(name, description);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }

            return Json(new ResultModel() { suc = true }, "text/html");
        }

        public JsonResult UpdateGroup(int id, FormCollection col)
        {
            string name = col.Get("groupName");
            string description = col.Get("description");

            string result = new GroupSv().UpdateGroup(id,name, description);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }
            return Json(new ResultModel() { suc = true }, "text/html");
        }

        public JsonResult GetUserInGroups(int groupId)
        {
            return Json(new { rows = new GroupSv().GetUserInGroup(groupId) });
        }

        public JsonResult GetUserForDepTree()
        {
            int depId = 0;
            string id = Request.Form.Get("id");
            if (!string.IsNullOrEmpty(id)) {
                depId = Int32.Parse(id);
            }
            return Json(new GroupSv().GetUserInDepForTree(depId));
        }

        public JsonResult AddUserInGroup(int groupId, string users)
        {
            string[] userArr = users.Split(',');
            string result = "";
            foreach (string user in userArr) {
                int userId = Int32.Parse(user);
                result = new GroupSv().AddUserInGroup(groupId, userId);
                if (!string.IsNullOrEmpty(result)) {
                    return Json(new ResultModel() { suc = false, msg = result });
                }
            }

            return Json(new ResultModel() { suc = true });
        }

        public JsonResult RemoveUserInGroup(int groupUserId)
        {
            string result = new GroupSv().RemoveUserInGroup(groupUserId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel() { suc = true });
        }

        public JsonResult GetAuthsInGroup(int groupId)
        {
            return Json(new { rows = new GroupSv().GetAuthInGroup(groupId) });
        }

        public JsonResult GetAllAuths()
        {
            return Json(new { rows = new GroupSv().GetAllAuths() });
        }

        public JsonResult AddAuthInGroup(int groupId, string auth)
        {
            string[] auths = auth.Split(',');
            string result = "";
            foreach (string au in auths) {
                int autId = Int32.Parse(au);
                result = new GroupSv().AddAuthInGroup(groupId, autId);
                if (!string.IsNullOrEmpty(result)) {
                    return Json(new ResultModel() { suc = false, msg = result });
                }
            }
            return Json(new ResultModel() { suc = true });
        }

        public JsonResult RemoveAuthInGroup(int groupAuthId)
        {
            string result = new GroupSv().RemoveAuthInGroup(groupAuthId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel() { suc = true });
        }

        #endregion

        #region 流程管理

        //流程管理
        [SessionTimeOutFilter]
        public ActionResult Processes()
        {
            return View();
        }

        public JsonResult GetProcesses()
        {
            return Json(new ProcessSv().GetProcesses());
        }

        //启用-禁用流程
        public JsonResult ToggleProc(int id)
        {
            string result = new ProcessSv().ToggleProc(id);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel() { suc = true });
        }

        //获取流程明细
        public JsonResult GetProDets(int id)
        {            
            return Json(new ProcessSv().GetProcessDetail(id));
        }

        //保存流程的修改
        public JsonResult SaveProcess(FormCollection col)
        {
            Process pro=new Process();
            SomeUtils.SetFieldValueToModel(col, pro);
            pro.ProcessDetail.AddRange(JsonConvert.DeserializeObject<List<ProcessDetail>>(col.Get("proDetails")));

            string result = new ProcessSv().SaveProcess(pro);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }

            return Json(new ResultModel() { suc = true });
        }

        #endregion

        #region 步骤审核人管理
        [SessionTimeOutFilter]
        public ActionResult StepAuditor()
        {
            return View();
        }

        public JsonResult GetAuditorRelations(string value = "")
        {
            return Json(new ProcessSv().GetStepAuditors(value.Trim()));
        }

        public JsonResult SaveAuditorRelation(FormCollection fcl)
        {
            MStepAuditor sa = new MStepAuditor();
            SomeUtils.SetFieldValueToModel(fcl, sa);

            string result = new ProcessSv().SaveStepAuditor(sa);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }

            return Json(new ResultModel() { suc = true, msg = "保存成功" }, "text/html");

        }

        public JsonResult UpdateAuditorRelation(int id, FormCollection fcl)
        {
            MStepAuditor sa = new MStepAuditor();
            SomeUtils.SetFieldValueToModel(fcl, sa);

            string result = new ProcessSv().SaveStepAuditor(sa, id);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }

            return Json(new ResultModel() { suc = true, msg = "保存成功" }, "text/html");
        }

        public JsonResult RemoveAuditorRelation(int id)
        {
            string result = new ProcessSv().RemoveStepAuditor(id);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }

            return Json(new ResultModel() { suc = true, msg = "删除成功" });
        }

        #endregion

        #region 佣金率维护

        [SessionTimeOutFilter]
        public ActionResult Commission()
        {
            return View();
        }

        public JsonResult GetCommissionList()
        {
            return Json(new CommissionSv().GetCommissionList());
        }

        public ActionResult CreateCommission()
        {
            CommissionRate cr = new CommissionRate();
            cr.id = 0;
            cr.user_name = currentUser.realName;
            cr.create_date = DateTime.Now;

            ViewData["commission"] = cr;
            ViewData["commissionDetails"] = new List<CommissionRateDetail>();

            return View();
        }

        public ActionResult UpdateCommission(int id)
        {
            CommissionRate cr = new CommissionSv().GetSinggleCommission(id);

            ViewData["commission"] = cr;
            ViewData["commissionDetails"] = cr.CommissionRateDetail.OrderBy(c => c.MU).ToList();

            return View("CreateCommission");
        }

        public JsonResult RemoveCommission(int id)
        {
            string result = new CommissionSv().RemoveCommission(id,currentUser.userId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result });
            }
            return Json(new ResultModel() { suc = true });
        }

        public JsonResult SaveCommission(FormCollection fc)
        {
            CommissionRate cr = new CommissionRate();
            string details = fc.Get("commissionRateDetails");

            SomeUtils.SetFieldValueToModel(fc, cr);
            cr.CommissionRateDetail.AddRange(JsonConvert.DeserializeObject<List<CommissionRateDetail>>(fc.Get("commissionRateDetails")));

            if (cr.id == 0) cr.create_date = DateTime.Now;
            cr.update_date = DateTime.Now;
            cr.user_name = currentUser.realName;

            string result = new CommissionSv().SaveCommission(cr, currentUser.userId);
            if (!string.IsNullOrEmpty(result)) {
                return Json(new ResultModel() { suc = false, msg = result }, "text/html");
            }

            return Json(new ResultModel() { suc = true, msg = "保存成功" }, "text/html");
        }

        #endregion


    }
}
