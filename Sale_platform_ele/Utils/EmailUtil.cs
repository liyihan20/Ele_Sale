using System.Web.Helpers;

namespace Sale_platform_ele.Utils
{
    public class EmailUtil
    {
        string server = "smtp.truly.com.cn";
        string semiServer = "smtp.truly.cn";
        string bcc = "crm@truly.com.cn";

        //半导体的邮箱
        public bool SendEmail(string content, string emailAddress, string ccEmail = null, string subject = "订单申请审批")
        {
            try {
                WebMail.SmtpServer = semiServer;
                WebMail.SmtpPort = 25;
                WebMail.UserName = "crm";
                WebMail.From = "\"CRM客户管理平台\"<crm@truly.cn>";
                WebMail.Password = "tic3006";
                WebMail.Send(
                to: emailAddress,
                cc: ccEmail,
                subject: subject + "（电子）",
                body: content + "<br /><div style='clear:both'><hr />来自:移动CRM客户管理平台<br />注意:此邮件是系统自动发送，请不要直接回复此邮件</div>",
                isBodyHtml: true
                );
            }
            catch {
                //如果发送失败，使用集团的邮箱发送。
                return UseOtherEmail(content, emailAddress);
            }
            return true;
        }

        //集团的邮箱
        public bool UseOtherEmail(string content, string emailAddress, string ccEmail = null, string subject = "订单申请审批")
        {
            try {
                WebMail.SmtpServer = server;
                WebMail.SmtpPort = 25;
                WebMail.UserName = "crm";
                WebMail.From = "\"CRM客户管理平台\"<crm@truly.com.cn>";
                WebMail.Password = "ic3508**";
                WebMail.Send(
                to: emailAddress,
                cc: ccEmail,
                bcc: bcc,
                subject: subject + "（电子）",
                body: content + "<br /><div style='clear:both'><hr />来自:CRM客户管理平台<br />注意:此邮件是系统自动发送，请不要直接回复此邮件</div>",
                isBodyHtml: true
                );
            }
            catch {
                return false;
            }
            return true;
        }

    }
}