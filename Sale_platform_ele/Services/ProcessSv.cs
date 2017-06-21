using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;

namespace Sale_platform_ele.Services
{
    public class ProcessSv
    {
        SaleDBDataContext db = new SaleDBDataContext();
        public List<ProInfo> GetProcesses()
        {
            var list = (from p in db.Process
                        orderby p.isUsing, p.billType
                        select new ProInfo()
                        {
                            id = p.id,
                            orderType = p.billType,
                            orderTypeName = new BillUtils().GetBillType(p.billType),
                            isUsing = p.isUsing == true ? "true" : "false",
                            info = p.info,
                            beginTime = DateTime.Parse(p.beginTime.ToString()).ToString("yyyy-MM-dd"),
                            endTime = DateTime.Parse(p.endTime.ToString()).ToString("yyyy-MM-dd"),
                            modifyTime = DateTime.Parse(p.modifyTime.ToString()).ToString("yyyy-MM-dd HH:mm")
                        }).ToList();
            return list;
        }

        public string ToggleProc(int id)
        {
            try {
                var pro = db.Process.Single(p => p.id == id);
                if (pro.isUsing == false) {
                    if (pro.ProcessDetail.Count() == 0) {
                        return "还没有设置流程，不能启用";
                    }
                    for (int i = 1; i <= pro.ProcessDetail.Max(p => p.step); i++) {
                        if (pro.ProcessDetail.Where(p => p.step == i).Count() == 0) {
                            return "步骤" + i.ToString() + "缺失，不能启用";
                        }
                    }
                    if (pro.ProcessDetail.Where(p => p.stepType == 0 && p.userId == null).Count() > 0) {
                        return "存在步骤类型为固定人员审核但是没有设置审核人的情况，不能启用";
                    }
                    pro.isUsing = true;
                }
                else {
                    pro.isUsing = false;
                }
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<proDetailInfo> GetProcessDetail(int id)
        {
            var result = (from pd in db.ProcessDetail
                          where pd.processId == id
                          orderby pd.step
                          select new proDetailInfo()
                          {
                              id = pd.id,
                              step = pd.step,
                              stepName = pd.stepName,
                              stepType = pd.stepType,
                              userId = pd.userId,
                              auditor = pd.userId != null ? pd.User.real_name : "",
                              canModify = pd.canModify == true ? "true" : "false",
                              canBeNull = pd.canBeNull == true ? "true" : "false",
                              isCountersign = pd.isCountersign == true ? "true" : "false",
                          }).ToList();
            return result;
        }

        public string SaveProcess(Process pro)
        {
            try {
                if (pro.beginTime == null) pro.beginTime = DateTime.Parse("2001-1-1");
                if (pro.endTime == null) pro.endTime = DateTime.Parse("2050-9-9");
                pro.modifyTime = DateTime.Now;
                pro.isUsing = true;

                if (pro.id > 0) {
                    db.ProcessDetail.DeleteAllOnSubmit(db.ProcessDetail.Where(pd => pd.processId == pro.id));
                    db.Process.DeleteOnSubmit(db.Process.Single(p => p.id == pro.id));
                }
                db.Process.InsertOnSubmit(pro);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<ComboResult> GetProcStepName()
        {
            var list = (from a in db.AuditorsRelation
                        select new ComboResult()
                        {
                            value = a.step_value.ToString(),
                            name = a.step_name
                        }).Distinct().ToList();
            return list;
        }

        public List<vwAuditorRelations> GetStepAuditors(string searchValue)
        {
            var result = (from v in db.vwAuditorRelations
                          where v.step_name.Contains(searchValue)
                          || v.relate_type.Contains(searchValue)
                          || v.department_name.Contains(searchValue)
                          || v.auditor_name.Contains(searchValue)
                          orderby v.step_value
                          select v).ToList();
            return result;
        }

    }
}