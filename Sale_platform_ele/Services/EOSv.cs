using Newtonsoft.Json;
using org.in2bits.MyXls;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Sale_platform_ele.Interfaces;

namespace Sale_platform_ele.Services
{
    public class EOSv:BillSv
    {
        const string BILL_TYPE = "EO";
        const string BILL_TYPE_NAME = "仪器/工业销售订单";
        const string CREATE_VIEW_NAME = "CreateEO";
        const string CHECK_VIEW_NAME = "CheckEO";
        const string CHECK_VIEW_LIST_NAME = "CheckEOList";
        private Sale_eo_bill bill;

        public EOSv()
        {

        }

        public EOSv(string sysNo)
        {
            try {
                bill = db.Sale_eo_bill.Single(c => c.sys_no == sysNo);
            }
            catch{
                throw new Exception("流水号不存在");
            }
        }

        public override string BillType
        {
            get { return BILL_TYPE; }
        }

        public override string BillTypeName
        {
            get { return BILL_TYPE_NAME; }
        }

        public override string CreateViewName
        {
            get { return CREATE_VIEW_NAME; }
        }

        public override string CheckViewName
        {
            get { return CHECK_VIEW_NAME; }
        }

        public override string CheckListViewName
        {
            get { return CHECK_VIEW_LIST_NAME; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            bill = new Sale_eo_bill();
            bill.sys_no = GetNextSysNo(BILL_TYPE);
            bill.applier_name = currentUser.realName;
            bill.account = "";

            return bill;
        }

        public override object GetBill(int stepVersion)
        {            
            return bill;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            bill = new Sale_eo_bill();
            SomeUtils.SetFieldValueToModel(fc, bill);
            bill.Sale_eo_bill_detail.AddRange(JsonConvert.DeserializeObject<List<Sale_eo_bill_detail>>(fc.Get("details")));

            if (string.IsNullOrEmpty(bill.clerk_no)) {
                return "营业员请输入厂牌或姓名然后按回车键搜索后，在列表中选择";
            }

            try {
                var existedBill = db.Sale_eo_bill.Where(s => s.sys_no == bill.sys_no).FirstOrDefault();
                if (existedBill != null) {
                    bill.applier_id = existedBill.applier_id;
                    bill.applier_name = existedBill.applier_name;
                    bill.apply_time = existedBill.apply_time;

                    //备份
                    JsonSerializerSettings js = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                    BackupData bd = new BackupData();
                    bd.op_date = DateTime.Now;
                    bd.sys_no = existedBill.sys_no;
                    bd.user_id = user.userId;
                    bd.main_data = JsonConvert.SerializeObject(existedBill, js);
                    bd.secondary_data = JsonConvert.SerializeObject(existedBill.Sale_eo_bill_detail, js);
                    db.BackupData.InsertOnSubmit(bd);

                    //删除
                    db.Sale_eo_bill_detail.DeleteAllOnSubmit(existedBill.Sale_eo_bill_detail);
                    db.Sale_eo_bill.DeleteOnSubmit(existedBill);
                }
                else {
                    bill.applier_id = user.userId;
                    bill.applier_name = user.realName;
                    bill.apply_time = DateTime.Now;
                }

                db.Sale_eo_bill.InsertOnSubmit(bill);
                db.Sale_eo_bill_detail.InsertAllOnSubmit(bill.Sale_eo_bill_detail);
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

            var result = from o in db.Sale_eo_bill
                         join e in db.Sale_eo_bill_detail on o.id equals e.eo_id
                         join a in db.Apply on o.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || o.applier_id == userId)
                         && o.apply_time >= fd
                         && o.apply_time < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new EOListModel()
                         {
                             account = o.account,
                             billId = o.id,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                             applyDate = o.apply_time.ToShortDateString(),
                             customerName = o.customer_name,
                             taxedPrice = e.tax_price,
                             productModel = e.product_model,
                             qty = e.qty,
                             productName = e.product_name,
                             sysNo = o.sys_no
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
            var eo = db.Sale_eo_bill.Where(e => e.sys_no == bill.sys_no).FirstOrDefault();
            eo.sys_no = GetNextSysNo(BillType);
            return eo;
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
            var details = db.Sale_eo_bill_detail.Where(d => d.eo_id == bill.id).ToList();

            if (details.Count() == 1) {
                return details.First().product_model;
            }
            else {
                return details.First().product_model + "...等" + details.Count() + "个";
            }
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.Sale_eo_bill.Where(c => c.sys_no == sysNo).Count() > 0;
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

        public override void DoWhenFinishAudit(bool isPass)
        {
            
        }

        /// <summary>
        /// 导出excel数据的模型
        /// </summary>
        private class ExcelData
        {
            public Sale_eo_bill h { get; set; }
            public Sale_eo_bill_detail e { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出CH的excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            
            //列名：
            string[] colName = new string[] { "审核结果","公司","流水号","订单类型","下单日期","制单人","产品类别","客户编码","客户名称","营业员","付款条款",
                                            "摘要","产品代码","产品名称","规格型号","单位","数量","含税单价","金额","税率%","交货日期",
                                            "备料单号","BOM表码","备注" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("仪器工业申请单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
            Worksheet sheet = xls.Workbook.Worksheets.Add("订单信息列表");

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
                col.Width = (ushort)(18 * 256);
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
                //"审核结果","公司","流水号","下单日期","制单人","产品类别","客户编码","客户名称","营业员","付款条款",
                //"摘要","产品代码","产品名称","规格型号","单位","数量","含税单价","金额","税率%","交货日期",
                //"备料单号","BOM表码","备注"
                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.account);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.order_type_name);
                cells.Add(rowIndex, ++colIndex, d.h.apply_time.ToShortDateString());
                cells.Add(rowIndex, ++colIndex, d.h.applier_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_type_name);
                cells.Add(rowIndex, ++colIndex, d.h.customer_no);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.h.clear_type_name);

                cells.Add(rowIndex, ++colIndex, d.h.summary);
                cells.Add(rowIndex, ++colIndex, d.e.product_no);
                cells.Add(rowIndex, ++colIndex, d.e.product_name);
                cells.Add(rowIndex, ++colIndex, d.e.product_model);
                cells.Add(rowIndex, ++colIndex, d.e.unit_name);
                cells.Add(rowIndex, ++colIndex, d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.tax_price);
                cells.Add(rowIndex, ++colIndex, d.e.qty * d.e.tax_price);
                cells.Add(rowIndex, ++colIndex, d.e.tax_rate);
                cells.Add(rowIndex, ++colIndex, d.e.fetch_date.ToShortDateString());

                cells.Add(rowIndex, ++colIndex, d.e.bl_no);
                cells.Add(rowIndex, ++colIndex, d.e.bom_no);
                cells.Add(rowIndex, ++colIndex, d.e.comment);
            }

            xls.Send();
        }

        /// <summary>
        /// 营业员导出excel
        /// </summary>
        /// <param name="pm">查询参数模型</param>
        /// <param name="userId">用户id</param>
        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
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

            var result = from h in db.Sale_eo_bill
                         join e in db.Sale_eo_bill_detail on h.id equals e.eo_id
                         join a in db.Apply on h.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || h.applier_id == userId)
                         && h.apply_time >= fd
                         && h.apply_time < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new ExcelData()
                         {
                             h = h,
                             e = e,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中")
                         };

            if (!string.IsNullOrWhiteSpace(pm.searchValue)) {
                result = result.Where(r => r.h.sys_no.Contains(pm.searchValue));
            }

            if (!string.IsNullOrWhiteSpace(pm.itemModel)) {
                result = result.Where(r => r.e.product_model.Contains(pm.itemModel));
            }

            if (!string.IsNullOrWhiteSpace(pm.customerName)) {
                result = result.Where(r => r.h.customer_name.Contains(pm.customerName));
            }

            ExportExcel(result.OrderByDescending(r => r.h.id).Take(200).ToList());
        }

        /// <summary>
        /// 审核人导出Excel
        /// </summary>
        /// <param name="pm">查询参数模型</param>
        /// <param name="userId">用户ID</param>
        public override void ExportAuditorExcle(AuditSearchParamModel pm, int userId)
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

            var result = (from a in db.Apply
                          from ad in a.ApplyDetails
                          join o in db.Sale_eo_bill on a.sys_no equals o.sys_no
                          join e in db.Sale_eo_bill_detail on o.id equals e.eo_id
                          where ad.user_id == userId
                          && a.order_type == BILL_TYPE
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
                              h = o,
                              e = e,
                              auditStatus = a.success == true ? "PASS" : a.success == false ? "NG" : "----"
                          }).Take(400).ToList();

            ExportExcel(result);
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override void BeforeRollBack(int step)
        {
            
        }
    }
}