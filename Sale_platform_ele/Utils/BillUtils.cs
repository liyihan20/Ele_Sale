using System;
using System.Reflection;

namespace Sale_platform_ele.Utils
{
    public class BillUtils
    {
        public string GetBillType(string typ)
        {
            string ty = typ.Length >= 2 ? typ.Substring(0, 2) : "";
            if (!string.IsNullOrEmpty(ty)) {
                Type t = Type.GetType(string.Format("Sale_platform_ele.Services.{0}Sv", ty));
                if (t.IsClass) {
                    //BillSv bs = (BillSv)Activator.CreateInstance(t);
                    //return bs.GetBillType();
                    object obj = Activator.CreateInstance(t);
                    MethodInfo mi = t.GetMethod("GetBillType");
                    return (string)mi.Invoke(obj, null);
                }
            }
            return "";
        }
    }
}