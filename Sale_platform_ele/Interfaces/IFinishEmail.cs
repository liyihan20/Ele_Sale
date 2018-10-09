using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sale_platform_ele.Interfaces
{
    interface IFinishEmail
    {
        string ccToOthers(string sysNo, bool isPass);
    }
}
