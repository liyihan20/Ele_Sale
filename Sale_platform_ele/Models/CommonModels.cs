using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sale_platform_ele.Models
{
    public class ComboResult
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ResultModel
    {
        public bool suc { get; set; }
        public string msg { get; set; }
        public object extra { get; set; }
    }

    public class TreeModel
    {
        public int id { get; set; }
        public string text { get; set; }
        public string state { get; set; }
        public string iconCls { get; set; }
    }

}