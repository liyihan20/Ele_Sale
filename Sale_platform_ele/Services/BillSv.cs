using Sale_platform_ele.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Sale_platform_ele.Services
{
    public abstract class BillSv:BaseSv
    {
        const string CHECK_VIEW_LIST_NAME = "CheckBillList";

        // --------------------------   抽象属性   -------------------------- //

        /// <summary>
        /// 单据类型EN
        /// </summary>
        public abstract string BillType { get; }

        /// <summary>
        /// 单据类型名
        /// </summary>
        public abstract string BillTypeName { get; }

        /// <summary>
        /// 新建单据视图名
        /// </summary>
        public abstract string CreateViewName { get; }               

        /// <summary>
        /// 查看单据视图名
        /// </summary>
        public abstract string CheckViewName { get; }


        // --------------------------   抽象方法   -------------------------- //

        /// <summary>
        /// 单据列表视图
        /// </summary>
        public virtual string CheckListViewName { get { return CHECK_VIEW_LIST_NAME; } }

        /// <summary>
        /// 新建单据需要返回的object
        /// </summary>
        /// <param name="userId">用户名</param>
        /// <returns>单据对象</returns>
        public abstract object GetNewBill(int userId);

        /// <summary>
        /// 获取单据对象
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <returns>单据对象</returns>
        public abstract object GetBill(int stepVersion);

        /// <summary>
        /// 验证、保存单据
        /// </summary>
        /// <param name="fc">表单值</param>
        /// <param name="userId">用户名</param>
        /// <returns>保存结果</returns>
        public abstract string SaveBill(FormCollection fc,int userId);

        /// <summary>
        /// 取得单据列表
        /// </summary>
        /// <param name="fc">查询表单值</param>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public abstract List<object> GetBillList(SalerSearchParamModel pm,int userId);

        /// <summary>
        /// 以旧单据为模板获取新单据
        /// </summary>
        /// <param name="oldSysNo">旧单据流水号</param>
        /// <returns>新的单据对象</returns>
        public abstract object GetNewBillFromOld();

        /// <summary>
        /// 获取单据对应的流程编号
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract string GetProcessNo();

        /// <summary>
        /// 获取提交申请时，流程需要的审核人对应关系字段
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract Dictionary<string, int?> GetProcessDic();

        /// <summary>
        /// 获取单据规格型号
        /// </summary>
        /// <returns></returns>
        public abstract string GetProductModel();

        /// <summary>
        /// 单据是否已保存
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public abstract bool HasOrderSaved(string sysNo);

        /// <summary>
        /// 取得单据详细类型
        /// </summary>
        /// <returns></returns>
        public abstract string GetSpecificBillTypeName();

        /// <summary>
        /// 开始审核之前需要做的事情
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="stepName">步骤名</param>
        /// <param name="isPass">是否通过</param>
        /// <param name="userId">审核人ID</param>
        public abstract void DoWhenBeforeAudit(int step,string stepName,bool isPass, int userId);
        
        /// <summary>
        /// 申请结束需要做的事情
        /// </summary>
        /// <param name="isPass">是否通过</param>
        public abstract void DoWhenFinishAudit(bool isPass);

        /// <summary>
        /// 营业员导出Excel
        /// </summary>
        /// <param name="pm">搜索参数模型</param>
        public abstract void ExportSalerExcle(SalerSearchParamModel pm, int userId);

        /// <summary>
        /// 审核人导出Excel
        /// </summary>
        /// <param name="pm">搜索参数模型</param>
        public abstract void ExportAuditorExcle(AuditSearchParamModel pm, int userId);

        // --------------------------  虚方法  -------------------------- //

        /// <summary>
        /// 取得下一个流水号
        /// </summary>
        /// <param name="billType"></param>
        /// <returns></returns>
        public virtual string GetNextSysNo(string billType)
        {
            string result = billType;
            string dateStr = DateTime.Now.ToString("yyMMdd");
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == billType && sn.date_string == dateStr);
            if (maxRecord.Count() == 0) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = billType,
                    date_string = dateStr,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += dateStr + "001";
            }
            else {
                var firstRecord = maxRecord.First();
                firstRecord.max_num = firstRecord.max_num + 1;
                result += dateStr + string.Format("{0:000}", firstRecord.max_num);
            }
            db.SubmitChanges();
            return result + "E";
        }

        /// <summary>
        /// 正式附件路径
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public virtual string GetAttachmentPath(string sysNo)
        {
            string p = ConfigurationManager.AppSettings["AttachmentPath2"];
            string p1 = sysNo.Substring(0, 2);
            string p2 = sysNo.Substring(2, 2);
            string p3 = sysNo.Substring(4, 2);
            string p4 = sysNo.Substring(6, 2);
            string path = Path.Combine(p, p1, p2, p3,p4);
            return path;
        }

        /// <summary>
        /// 将附件从临时目录转移到正式目录
        /// </summary>
        /// <param name="sysNo"></param>
        public virtual void MoveToFormalDir(string sysNo)
        {
            string fileName = sysNo + ".rar";
            string oldPath = Path.Combine(ConfigurationManager.AppSettings["AttachmentPath1"], fileName);
            if (System.IO.File.Exists(oldPath)) {
                FileInfo info = new FileInfo(oldPath);
                string newPath = GetAttachmentPath(sysNo);
                if (!Directory.Exists(newPath)) {
                    Directory.CreateDirectory(newPath);
                }
                string newFile = Path.Combine(newPath, fileName);
                //如果正式目录已存在，则先删除
                if (File.Exists(newFile)) {
                    File.Delete(newFile);
                }
                info.MoveTo(newFile);
            }
        }

        
    }
}