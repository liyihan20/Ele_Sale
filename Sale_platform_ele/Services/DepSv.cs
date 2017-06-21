using Sale_platform_ele.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Services
{
    public class DepSv
    {
        SaleDBDataContext db = new SaleDBDataContext();

        public List<DepartmentInfo> GetDepartments(string searchValue)
        {
            var list = (from d in db.Department
                        where d.name.Contains(searchValue)
                        || d.dep_type.Contains(searchValue)
                        orderby d.dep_type, d.dep_no
                        select new DepartmentInfo()
                        {
                            id = d.id,
                            depName = d.name,
                            depType = d.dep_type,
                            depNo = d.dep_no
                        }).ToList();
            return list;
        }

        public string SaveDepartment(string depType, string depName)
        {
            if (db.Department.Where(d => d.dep_type == depType && d.name == depName).Count() > 0) {
                return "此部门已存在，不能重复保存";
            }

            try {
                var deps = db.Department.Where(d => d.dep_type == depType);
                int depNo;
                if (deps.Count() > 0) {
                    depNo = (int)deps.Max(d => d.dep_no) + 1;
                }
                else {
                    depNo = 1;
                }
                db.Department.InsertOnSubmit(new Department()
                {
                    name = depName,
                    dep_no = depNo,
                    dep_type = depType
                });
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string UpdateDepartment(int depId, string depType, string depName)
        {
            if (db.Department.Where(d => d.dep_type == depType && d.name == depName).Count() > 0) {
                return "此部门已存在，不能重复保存";
            }

            try {
                var dep = db.Department.Single(d => d.id == depId);
                dep.name = depName;
                dep.dep_type = depType;
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string RemoveDepartment(int depId)
        {
            var dep = db.Department.Single(d => d.id == depId);
            if (db.User.Where(u => u.department_no == dep.dep_no && dep.dep_type == "部门").Count() > 0) {
                return "此部门下有用户，不能删除";
            }
            if (db.AuditorsRelation.Where(a => a.relate_type == dep.dep_type && a.relate_value == dep.dep_no).Count() > 0) {
                return "此部门下有审核人关联关系，不能删除";
            }

            try {
                db.Department.DeleteOnSubmit(dep);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<ComboResult> GetDepTypes()
        {
            var result = (from d in db.Department
                          select new ComboResult()
                          {
                              name = d.dep_type,
                              value = d.dep_type
                          }).Distinct().ToList();
            return result;
        }

        public List<ComboResult> GetDepsByType(string type)
        {
            var result = (from d in db.Department
                          where d.dep_type == type
                          select new ComboResult()
                          {
                              name = d.name,
                              value = d.dep_no.ToString()
                          }).ToList();
            return result;
        }

    }
}