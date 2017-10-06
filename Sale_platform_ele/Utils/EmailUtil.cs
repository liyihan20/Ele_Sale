using System.Web.Helpers;

namespace Sale_platform_ele.Utils
{
    public class EmailUtil
    {        
        //半导体的邮箱
        public bool SendEmail(string content, string emailAddress, string ccEmail = null, string subject = "订单申请审批")
        {
            return TrulyEmail.EmailUtil.SemiSend("CRM客户管理平台", subject+"（电子）", content, emailAddress, ccEmail);            
        }
        

    }
}