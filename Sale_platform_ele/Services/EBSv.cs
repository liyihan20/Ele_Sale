using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using org.in2bits.MyXls;

namespace Sale_platform_ele.Services
{
    public class EBSv:BillSv
    {
        private Sale_eb_bill bill;

        public EBSv() { }
        public EBSv(string sysNo)
        {
            bill = db.Sale_eb_bill.Single(e => e.sys_no == sysNo);
        }

        public override string BillType
        {
            get { return "EB"; }
        }

        public override string BillTypeName
        {
            get { return "仪器/工业备料单"; }
        }

        public override string CreateViewName
        {
            get { return "Create" + BillType; }
        }

        public override string CheckViewName
        {
            get { return "Check" + BillType; }
        }

        public override string CheckListViewName
        {
            get { return CheckViewName + "List"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new Sale_eb_bill();
            bill.applier_name = currentUser.realName;
            bill.sys_no = GetNextSysNo(BillType);
            bill.bl_date = DateTime.Now;

            return bill;

        }

        public override object GetBill(int stepVersion)
        {
            return bill;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            bill = new Sale_eb_bill();
            SomeUtils.SetFieldValueToModel(fc, bill);

            if (string.IsNullOrWhiteSpace(bill.customer_name)) {
                return "客户名称必须填写，请在代码处输入后按回车搜索";
            }
            if (string.IsNullOrWhiteSpace(bill.product_model)) {
                return "产品必须填写，请在代码处输入后按回车搜索";
            }

            if (string.IsNullOrEmpty(bill.bl_project)) {
                return "备料明细必须至少勾选一个";
            }

            bill.applier_id = user.userId;
            bill.apply_time = DateTime.Now;

            var existedBill = db.Sale_eb_bill.Where(e => e.sys_no == bill.sys_no).FirstOrDefault();
            if (existedBill == null) {
                db.Sale_eb_bill.InsertOnSubmit(bill);
            }
            else {
                SomeUtils.CopyPropertyValue(bill, existedBill);
            }
            try {
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }
            return "";
        }

        public override List<object> GetBillList(Models.SalerSearchParamModel pm, int userId)
        {
            bool canCheckAll = new UA(userId).CanCheckAllBill();

            DateTime fd, td;
            if (!DateTime.TryParse(pm.fromDate, out fd)) {
                fd = DateTime.Parse("2017-01-01");
            }
            if (!DateTime.TryParse(pm.toDate, out td)) {
                td = DateTime.Parse("2049-09-09");
            }
            td = td.AddDays(1);

            var result = from e in db.Sale_eb_bill
                         join a in db.Apply on e.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || e.applier_id == userId)
                         && e.bl_date >= fd
                         && e.bl_date < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new EBListModel()
                         {
                             billId = e.id,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                             billDate = e.bl_date.ToShortDateString(),
                             customerName = e.customer_name,
                             dealPrice = e.deal_price,
                             productModel = e.product_model,
                             qty = e.qty,
                             productName = e.product_name,
                             sysNo = e.sys_no
                         };

            if (!string.IsNullOrWhiteSpace(pm.searchValue)) {
                result = result.Where(r => r.sysNo.Contains(pm.searchValue));
            }

            if (!string.IsNullOrWhiteSpace(pm.itemModel)) {
                result = result.Where(r => r.productModel.Contains(pm.itemModel));
            }

            if (!string.IsNullOrWhiteSpace(pm.customerName)) {
                result = result.Where(r => r.customerName.Contains(pm.customerName));
            }

            return result.OrderByDescending(r => r.billId).Take(200).ToList<object>();
        }

        public override object GetNewBillFromOld()
        {
            bill.sys_no = GetNextSysNo(BillType);
            bill.bl_date = DateTime.Now;
            bill.plan_ch_date = DateTime.MinValue;
            bill.plan_order_date = DateTime.MinValue;            
            bill.bl_no = "";

            return bill;
        }

        public override string GetProcessNo()
        {
            return BillType;
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            return null;
        }

        public override string GetProductModel()
        {
            return bill.product_model;
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.Sale_eb_bill.Where(e => e.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return BillTypeName;
        }

        public override void DoWhenBeforeApply()
        {
            
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            
        }

        /// <summary>
        /// 流程结束后生成备料单号
        /// </summary>
        /// <param name="isPass"></param>
        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                if (bill.bus_name.Equals("仪器")) {
                    bill.bl_no = GetNextNo("AO", "", 1);
                }
                else {
                    bill.bl_no = GetNextNo("C", DateTime.Now.ToString("yyyy-"));
                }
                db.SubmitChanges();
            }
        }

        private class ExcelData
        {
            public Sale_eb_bill h { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {            
            //列名：
            string[] colName = new string[] { "审核结果","流水号","备料单号","备料日期","事业部","产品类别","备料类型","协议号","客户编码","客户名称",
                                            "对应项目组","产品代码","产品名称","产品型号","办事处","营业员","计划下单日期","贸易类型","产品用途","计划出货日期",
                                            "订单数量","不含税成交价","制单人","备料明细" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("仪器/电子备料单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
            Worksheet sheet = xls.Workbook.Worksheets.Add("备料信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colName.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(16 * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }
            foreach (var d in myData) {
                colIndex = 1;

                //"审核结果","流水号","备料单号","备料日期","事业部","产品类别","备料类型","协议号","客户编码","客户名称",
                //"对应项目组","产品代码","产品名称","产品型号","办事处","营业员","计划下单日期","贸易类型","产品用途","计划出货日期",
                //"订单数量","不含税成交价","制单人"

                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.bl_no);
                cells.Add(rowIndex, ++colIndex, d.h.bl_date.ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.bus_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_type);
                cells.Add(rowIndex, ++colIndex, d.h.bl_type);
                cells.Add(rowIndex, ++colIndex, d.h.protocol_num);
                cells.Add(rowIndex, ++colIndex, d.h.customer_no);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);

                cells.Add(rowIndex, ++colIndex, d.h.project_group_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_no);
                cells.Add(rowIndex, ++colIndex, d.h.product_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_model);
                cells.Add(rowIndex, ++colIndex, d.h.agency_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.h.plan_order_date.ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.trade_type_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_usage);
                cells.Add(rowIndex, ++colIndex, d.h.plan_ch_date.ToString("yyyy-MM-dd"));

                cells.Add(rowIndex, ++colIndex, d.h.qty);
                cells.Add(rowIndex, ++colIndex, d.h.deal_price);
                cells.Add(rowIndex, ++colIndex, d.h.applier_name);
                cells.Add(rowIndex, ++colIndex, d.h.bl_project + "," + (d.h.bl_project_other ?? ""));
            }

            xls.Send();
        }
        public override void ExportSalerExcle(Models.SalerSearchParamModel pm, int userId)
        {
            bool canCheckAll = new UA(userId).CanCheckAllBill();

            DateTime fd, td;
            if (!DateTime.TryParse(pm.fromDate, out fd)) {
                fd = DateTime.Parse("2017-01-01");
            }
            if (!DateTime.TryParse(pm.toDate, out td)) {
                td = DateTime.Parse("2049-09-09");
            }
            td = td.AddDays(1);

            var result = from e in db.Sale_eb_bill
                         join a in db.Apply on e.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || e.applier_id == userId)
                         && e.bl_date >= fd
                         && e.bl_date < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new ExcelData()
                         {
                             h = e,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中")
                         };

            if (!string.IsNullOrWhiteSpace(pm.searchValue)) {
                result = result.Where(r => r.h.sys_no.Contains(pm.searchValue));
            }

            if (!string.IsNullOrWhiteSpace(pm.itemModel)) {
                result = result.Where(r => r.h.product_model.Contains(pm.itemModel));
            }

            if (!string.IsNullOrWhiteSpace(pm.customerName)) {
                result = result.Where(r => r.h.customer_name.Contains(pm.customerName));
            }

            ExportExcel(result.OrderByDescending(r => r.h.id).Take(200).ToList());
        }

        public override void ExportAuditorExcle(Models.AuditSearchParamModel pm, int userId)
        {
            DateTime fDate, tDate;
            if (!DateTime.TryParse(pm.fromDate, out fDate)) {
                fDate = DateTime.Parse("2010-6-1");
            }
            if (!DateTime.TryParse(pm.toDate, out tDate)) {
                tDate = DateTime.Parse("2049-9-9");
            }
            tDate = tDate.AddDays(1);

            pm.saler = pm.saler ?? "";
            pm.proModel = pm.proModel ?? "";
            pm.sysNo = pm.sysNo ?? "";

            var result = from a in db.Apply
                         from ad in a.ApplyDetails
                         join e in db.Sale_eb_bill on a.sys_no equals e.sys_no
                         where ad.user_id == userId
                         && a.order_type == BillType
                         && a.sys_no.Contains(pm.sysNo)
                         && a.user_name.Contains(pm.saler)
                         && a.p_model.Contains(pm.proModel)
                         && a.start_date >= fDate
                         && a.start_date <= tDate
                         && (pm.finalResult == 10
                         || (pm.finalResult == 1 && a.success == true)
                         || (pm.finalResult == 0 && a.success == null)
                         || (pm.finalResult == -1 && a.success == false))
                         && (pm.auditResult == 10
                         || (pm.auditResult == 1 && ad.pass == true)
                         || (pm.auditResult == 0 && ad.pass == null
                              && ((ad.countersign == true && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == false).Count() == 0)
                                  || ((ad.countersign == false || ad.countersign == null) && a.ApplyDetails.Where(ads => ads.step == ad.step && ads.pass == true).Count() == 0)
                              )
                            )
                         || (pm.auditResult == -1 && ad.pass == false)
                         )
                         && (ad.step == 1 || a.ApplyDetails.Where(ads => ads.step == ad.step - 1 && ads.pass == true).Count() > 0)
                         orderby a.start_date descending
                         select new ExcelData()
                         {
                             h = e,
                             auditStatus = a.success == true ? "PASS" : a.success == false ? "NG" : "----"
                         };

            if (!string.IsNullOrWhiteSpace(pm.saler)) {
                result = result.Where(r => r.h.applier_name.Contains(pm.saler));
            }

            if (!string.IsNullOrWhiteSpace(pm.sysNo)) {
                result = result.Where(r => r.h.sys_no.Contains(pm.sysNo));
            }

            if (!string.IsNullOrWhiteSpace(pm.proModel)) {
                result = result.Where(r => r.h.product_model.Contains(pm.proModel));
            }

            ExportExcel(result.OrderByDescending(r=>r.h.id).Take(200).ToList());
        }

        public override void BeforeRollBack(int step)
        {
            if (!string.IsNullOrWhiteSpace(bill.bl_no)) {
                throw new Exception("备料单号已生成，不能反审");
            }
        }
    }
}