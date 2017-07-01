using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sale_platform_ele.Services
{
    public class ProcessSv:BaseSv
    {
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

        public string SaveStepAuditor(MStepAuditor sa, int id = 0)
        {
            if (sa.stepValue == null) {
                sa.stepValue = db.AuditorsRelation.Max(a => a.step_value) + 1;
            }
            if (db.AuditorsRelation.Where(a => a.auditor_id == sa.auditor && a.step_value == sa.stepValue
                && a.relate_type == sa.relateType && a.relate_value == sa.relateValue).Count() > 0) {
                return "对应关系已存在";
            }

            try {
                AuditorsRelation ar;
                if (id == 0) {
                    ar = new AuditorsRelation();
                    db.AuditorsRelation.InsertOnSubmit(ar);
                }
                else {
                    ar = db.AuditorsRelation.Single(a => a.id == id);
                }
                ar.auditor_id = sa.auditor;
                ar.step_name = sa.stepName;
                ar.step_value = sa.stepValue;
                ar.relate_value = sa.relateValue;
                ar.relate_type = sa.relateType;

                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string RemoveStepAuditor(int id)
        {
            try {
                db.AuditorsRelation.DeleteOnSubmit(db.AuditorsRelation.Single(a => a.id == id));
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<ComboResult> GetAuditorRelateTypes()
        {
            var list = (from a in db.AuditorsRelation
                        select new ComboResult()
                        {
                            value = a.relate_type,
                            name = a.relate_type
                        }).Distinct().ToList();
            return list;
        }

        public List<ComboResult> GetProcessStepName()
        {
            var list = (from a in db.AuditorsRelation
                        select new ComboResult()
                        {
                            value = a.step_value.ToString(),
                            name = a.step_name
                        }).Distinct().ToList();
            return list;
        }

        public Process GetProcessByNo(string processNo)
        {
            var pros = db.Process.Where(p => p.billType == processNo);
            if (pros.Count() == 0) {
                throw new Exception("此流程编号不存在");
            }
            var pro = pros.First();
            if (pro.beginTime > DateTime.Now || pro.endTime < DateTime.Now) {
                throw new Exception("此流程已过期");
            }
            return pro;
        }

    }
}