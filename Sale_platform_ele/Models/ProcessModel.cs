using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class ProInfo
    {
        public int id { get; set; }
        public string orderType { get; set; }
        public string info { get; set; }
        public string orderTypeName { get; set; }
        public string modifyTime { get; set; }
        public string isUsing { get; set; }
        public string beginTime { get; set; }
        public string endTime { get; set; }
    }

    public class proDetailInfo
    {
        public int id { get; set; }
        public int? step { get; set; }
        public string stepName { get; set; }
        public int? stepType { get; set; }
        public int? userId { get; set; }
        public string auditor { get; set; }
        public string canModify { get; set; }
        public string canBeNull { get; set; }
        public string isCountersign { get; set; }
    }

    public class MStepAuditor
    {
        public string stepName { get; set; }
        public int? stepValue { get; set; }
        public string relateType { get; set; }
        public int? relateValue { get; set; }
        public int? auditor { get; set; }
    }

}