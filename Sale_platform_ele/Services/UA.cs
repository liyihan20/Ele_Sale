using Sale_platform_ele.Models;
using System.Linq;

namespace Sale_platform_ele.Services
{
    public class UA:BaseSv
    {
        int userId;

        public UA(int userId)
        {            
            this.userId = userId;
        }        

        public User GetUser()
        {
            return db.User.Single(u => u.id == userId);
        }

        public string GetUserDepartmentName()
        {
            return db.Department.Where(d => d.dep_no == GetUser().department_no && d.dep_type == "部门").First().name;
        }

        public string[] GetUserPowers()
        {
            var powers = (from a in db.Authority
                          from u in db.Group
                          from ga in a.GroupAndAuth
                          from gu in u.GroupAndUser
                          where ga.group_id == u.id && gu.user_id == userId
                          select a.sname).ToArray();
            return powers;
        }

               
    }
}