using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class AuditStatusModel
    {
        public int step { get; set; }
        public string stepName { get; set; }
        public string opDate { get; set; }
        public string auditor { get; set; }
        public string department { get; set; }
        public string comment { get; set; }
        public bool? pass { get; set; }
    }

    public class AuditListModel
    {
        public int step { get; set; }
        public string stepName { get; set; }
        public int applyId { get; set; }
        public int applyDetailId { get; set; }
        public string sysNo { get; set; }
        public string depName { get; set; }
        public string salerName { get; set; }
        public string applyTime { get; set; }
        public string status { get; set; }
        public string orderNo { get; set; }
        public string hasImportK3 { get; set; }
        public string finalStatus { get; set; }
        public string orderType { get; set; }
        public string model { get; set; }
        public string customer { get; set; }
    }

    public class AuditResultModel
    {
        public bool canAudit { get; set; }
        public string auditResult { get; set; }
        public string comment { get; set; }
    }


}