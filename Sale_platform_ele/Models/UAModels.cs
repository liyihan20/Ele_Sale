using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class DepartmentInfo
    {
        public int id { get; set; }
        public string depName { get; set; }
        public int? depNo { get; set; }
        public string depType { get; set; }
    }

    public class UsersInfo
    {
        public int id { get; set; }
        public string userName { get; set; }
        public string realName { get; set; }
        public string password { get; set; }
        public string depName { get; set; }
        public string email { get; set; }
        public string registerDate { get; private set; }
        public string isForbit { get; set; }
        public string forbitReason { get; set; }
        public string forbitDate { get; private set; }
        public string lastLoginDate { get; private set; }

        public DateTime? registerDateDT { set {if (value != null) registerDate = ((DateTime)value).ToString("yyyy-MM-dd");} }
        public DateTime? forbitDateDT { set { if (value != null) forbitDate = ((DateTime)value).ToString("yyyy-MM-dd"); } }
        public DateTime? lastLoginDateDT { set { if (value != null) lastLoginDate = ((DateTime)value).ToString("yyyy-MM-dd"); } }
    }

    public class GroupInfo
    {
        public int id { get; set; }
        public string groupName { get; set; }
        public string description { get; set; }
    }

    public class GroupUser
    {
        public int groupUserId { get; set; }
        public string userName { get; set; }
        public string realName { get; set; }
        public string depName { get; set; }
    }

    public class GroupAuth
    {
        public int groupAuthId { get; set; }
        public string authName { get; set; }
        public string authDescription { get; set; }
    }

    public class AuthInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

}