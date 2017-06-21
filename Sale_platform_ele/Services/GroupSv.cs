using Sale_platform_ele.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Services
{
    public class GroupSv
    {
        SaleDBDataContext db = new SaleDBDataContext();

        public List<GroupInfo> GetAllGroups()
        {
            var list = (from g in db.Group
                        select new GroupInfo()
                        {
                            id = g.id,
                            groupName = g.name,
                            description = g.description
                        }).ToList();
            return list;
        }

        public string SaveGroup(string name, string description)
        {
            if (db.Group.Where(g => g.name == name).Count() > 0) {
                return "组名已存在，保存失败";
            }

            try {
                db.Group.InsertOnSubmit(new Group() { name = name, description = description });
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public string UpdateGroup(int id, string name, string description)
        {
            if (db.Group.Where(g => g.name == name && g.id != id).Count() > 0) {
                return "组名已存在，保存失败";
            }

            try {
                Group gr = db.Group.Single(g => g.id == id);
                gr.name = name;
                gr.description = description;
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        public List<GroupUser> GetUserInGroup(int groupId)
        {
            var list = (from gu in db.GroupAndUser
                        where gu.group_id == groupId
                        select new GroupUser()
                        {
                            groupUserId = gu.id,
                            userName = gu.User.username,
                            realName = gu.User.real_name,
                            depName = db.Department.Where(d => d.dep_no == gu.User.department_no && d.dep_type == "部门").First().name
                        }).ToList();
            return list;
        }

        public List<TreeModel> GetUserInDepForTree(int depId)
        {
            List<TreeModel> list;
            if (depId == 0) {
                list = (from d in db.Department
                        where d.dep_type == "部门"
                        select new TreeModel()
                        {
                            id = d.id,
                            text = d.name,
                            state = "closed",
                            iconCls = "icon-home"
                        }).ToList();
            }
            else {
                list = (from u in db.User
                        join d in db.Department.Where(dt => dt.dep_type == "部门") on u.department_no equals d.dep_no
                        where d.id == depId
                        select new TreeModel()
                        {
                            id = u.id,
                            text = u.real_name,
                            state = "open",
                            iconCls = "icon-user"
                        }).ToList();
            }
            return list;
        }

        public string AddUserInGroup(int groupId, int userId)
        {
            try {
                if (db.GroupAndUser.Where(gu => gu.group_id == groupId && gu.user_id == userId).Count() < 1) {
                    db.GroupAndUser.InsertOnSubmit(new GroupAndUser() { group_id = groupId, user_id = userId });
                    db.SubmitChanges();
                }
            }
            catch (Exception ex) {
                return ex.Message;
            }
            return "";
        }

        public string RemoveUserInGroup(int groupUserId)
        {
            try {
                var groupUsers = db.GroupAndUser.Where(gu => gu.id==groupUserId);
                db.GroupAndUser.DeleteAllOnSubmit(groupUsers);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }
            return "";
        }

        public List<GroupAuth> GetAuthInGroup(int groupId)
        {
            var list = (from ga in db.GroupAndAuth
                        where ga.group_id == groupId
                        select new GroupAuth()
                        {
                            groupAuthId = ga.id,
                            authName = ga.Authority.name,
                            authDescription = ga.Authority.description
                        }).ToList();
            return list;
        }

        public List<AuthInfo> GetAllAuths()
        {
            var list = (from a in db.Authority
                        select new AuthInfo()
                        {
                            id = a.id,
                            name = a.name,
                            description = a.description
                        }).ToList();
            return list;
        }

        public string AddAuthInGroup(int groupId, int authId)
        {
            try {
                if (db.GroupAndAuth.Where(ga => ga.group_id == groupId && ga.auth_id == authId).Count() < 1) {
                    db.GroupAndAuth.InsertOnSubmit(new GroupAndAuth() { group_id = groupId, auth_id = authId });
                    db.SubmitChanges();
                }
            }
            catch (Exception ex) {
                return ex.Message;
            }
            return "";
        }

        public string RemoveAuthInGroup(int groupAuthId)
        {
            try {
                var groupAuths = db.GroupAndAuth.Where(ga => ga.id == groupAuthId);
                db.GroupAndAuth.DeleteAllOnSubmit(groupAuths);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }
            return "";
        }

    }
}