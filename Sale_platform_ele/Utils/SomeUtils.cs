using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace Sale_platform_ele.Utils
{
    public class SomeUtils
    {

        public static string getMD5(string str)
        {
            //HashAlgorithm hash = HashAlgorithm.Create("MD5");
            //byte[] result = hash.ComputeHash(Encoding.Default.GetBytes(str));
            //return BitConverter.ToString(result);
            if (str.Length > 2) {
                str = "Who" + str.Substring(2) + "Are" + str.Substring(0, 2) + "You";
            }
            else {
                str = "Who" + str + "Are" + str + "You";
            }
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.Default.GetBytes(str);
            byte[] result = md5.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < result.Length; i++) {
                ret += result[i].ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        //将中文编码为utf-8
        public static string EncodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlEncode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        //将utf-8解码
        public static string DecodeToUTF8(string str)
        {
            string result = System.Web.HttpUtility.UrlDecode(str, System.Text.Encoding.GetEncoding("UTF-8"));
            return result;
        }

        public static string ModelToString<T>(T t)
        {
            string result = "";
            foreach (var p in t.GetType().GetProperties()) {
                var name = p.Name;
                var value = p.GetValue(t, null);
                string strType = p.PropertyType.FullName;
                //id和user_id不需要，因为要验证是否修改过
                if (name.Equals("id")
                    || name.Equals("user_id")
                    || name.Equals("step_version")
                    || name.Equals("original_user_id")
                    || name.Equals("update_user_id")
                    || name.Equals("create_date")) {
                    continue;
                }
                if (strType.Contains("String") || strType.Contains("Int32") || strType.Contains("Decimal") || strType.Contains("DateTime")) {
                    if (value != null) {
                        result += name + ":" + value + ";";
                    }
                }
            }

            return result;
        }

        public static string ModelsToString<T>(List<T> ts)
        {
            string result = "[";
            foreach (var t in ts) {
                result += "{";
                foreach (var p in t.GetType().GetProperties()) {
                    var name = p.Name;
                    var value = p.GetValue(t, null);
                    string strType = p.PropertyType.FullName;
                    //id和user_id不需要，因为要验证是否修改过
                    if (name.Equals("id")
                        || name.Equals("bl_id")
                        || name.Equals("user_id")
                        || name.Equals("step_version")
                        || name.Equals("original_user_id")
                        || name.Equals("update_user_id")
                        || name.Equals("create_date")) {
                        continue;
                    }
                    if (strType.Contains("String") || strType.Contains("Int32") || strType.Contains("Decimal") || strType.Contains("DateTime")) {
                        if (value != null) {
                            result += name + ":" + value + ";";
                        }
                    }
                }
                result += "},";
            }
            result += "]";
            return result;
        }

        /// <summary>
        /// 使用反射将表单的值设置到数据库对象中，根据字段名
        /// </summary>
        /// <param name="col">表单</param>
        /// <param name="obj">数据库对象</param>
        public static void SetFieldValueToModel(FormCollection col, object obj)
        {
            foreach (var p in obj.GetType().GetProperties()) {
                string val = col.Get(p.Name);//字段值
                string pType = p.PropertyType.FullName;//数据类型
                if (string.IsNullOrEmpty(val) || val.Equals("null")) continue;
                if (pType.Contains("DateTime")) {
                    DateTime dt;
                    if (DateTime.TryParse(val, out dt)) {
                        p.SetValue(obj, dt, null);
                    }
                }
                else if (pType.Contains("Int32")) {
                    int i;
                    if (int.TryParse(val, out i)) {
                        p.SetValue(obj, i, null);
                    }
                }
                else if (pType.Contains("Decimal")) {
                    decimal dm;
                    if (decimal.TryParse(val, out dm)) {
                        p.SetValue(obj, dm, null);
                    }
                }
                else if (pType.Contains("String")) {
                    p.SetValue(obj, val, null);
                }
                else if (pType.Contains("Bool")) {
                    bool bl;
                    if (bool.TryParse(val, out bl)) {
                        p.SetValue(obj, bl, null);
                    }
                }
            }
        }

        //url encode
        public static string MyUrlEncoder(string url)
        {
            return url.Replace("=", "(equal)").Replace("&", "(and)").Replace("/", "(slash)").Replace("?", "(ask)");
        }

        public static string MyUrlDecoder(string url)
        {
            return url.Replace("(equal)", "=").Replace("(and)", "&").Replace("(slash)", "/").Replace("(ask)", "?");
        }

        

    }
}