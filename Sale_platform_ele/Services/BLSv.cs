using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using org.in2bits.MyXls;

namespace Sale_platform_ele.Services
{
    public class BLSv:BillSv
    {
        private Sale_BL bill;

        public BLSv() { }
        public BLSv(string sysNo)
        {
            try {
                bill = db.Sale_BL.Single(b => b.sys_no == sysNo);
            }
            catch {
                throw new Exception("流水号不存在");
            }
        }

        public override string BillType
        {
            get { return "BL"; }
        }

        public override string BillTypeName
        {
            get { return "备料单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateBL"; }
        }

        public override string CheckViewName
        {
            get { return "CheckBL"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckBLList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            var bl = new Sale_BL();
            bl.applier_name = currentUser.realName;
            bl.sys_no = GetNextSysNo(BillType);
            bl.bill_date = DateTime.Now;
            bl.step_version = 0;

            return bl;
        }

        public override object GetBill(int stepVersion)
        {
            bill.step_version = stepVersion;
            return bill;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            bill = new Sale_BL();
            SomeUtils.SetFieldValueToModel(fc, bill);

            if (string.IsNullOrWhiteSpace(bill.customer_name)) {
                return "客户名称必须填写，请在代码处输入后按回车搜索";
            }
            if (string.IsNullOrWhiteSpace(bill.product_model)) {
                return "产品必须填写，请在代码处输入后按回车搜索";
            }
            if (string.IsNullOrEmpty(bill.clerk_no)) {
                return "营业员必须输入姓名或厂牌后按回车，在列表中选择";
            }

            if (string.IsNullOrEmpty(bill.bl_project)) {
                return "备料明细必须至少勾选一个";
            }

            var existedBill = db.Sale_BL.Where(e => e.sys_no == bill.sys_no).FirstOrDefault();
            if (existedBill == null) {
                bill.applier_id = user.userId;
                bill.applier_name = user.realName;
                bill.apply_time = DateTime.Now;

                db.Sale_BL.InsertOnSubmit(bill);
            }
            else {
                bill.applier_id = existedBill.applier_id;
                bill.applier_name = existedBill.applier_name;
                bill.apply_time = existedBill.apply_time;
                db.BackupData.InsertOnSubmit(new BackupData()
                {
                    op_date = DateTime.Now,
                    sys_no = bill.sys_no,
                    user_id = user.userId,
                    main_data = JsonConvert.SerializeObject(existedBill)
                });

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

        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {
            bool canCheckAll = new UA(userId).CanCheckAllBill();

            DateTime fd, td;
            if (!DateTime.TryParse(pm.fromDate, out fd)) {
                fd = DateTime.Parse("2021-01-01");
            }
            if (!DateTime.TryParse(pm.toDate, out td)) {
                td = DateTime.Parse("2049-09-09");
            }
            td = td.AddDays(1);

            var result = from e in db.Sale_BL
                         join a in db.Apply on e.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || e.applier_id == userId)
                         && e.bill_date >= fd
                         && e.bill_date < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new BLListModel()
                         {
                             billId = e.id,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                             billDate = e.bill_date.ToShortDateString(),
                             customerName = e.customer_name,
                             productModel = e.product_model,
                             qty = e.bl_qty,
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
            bill.bill_date = DateTime.Now;

            return bill;
        }

        public override string GetProcessNo()
        {
            return BillType;
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", new UA(bill.applier_id).GetUser().department_no);

            return dic;
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
            return db.Sale_BL.Where(b => b.sys_no == sysNo).Count() > 0;
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
            if (stepName.Contains("计划经理") && isPass) {
                if (string.IsNullOrEmpty(bill.good_percent)) {
                    throw new Exception("必须填写订料良率并保存后，才能同意");
                }
            }
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            
        }

        private class ExcelData
        {
            public Sale_BL h { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            //列名：
            string[] colName = new string[] { "审核结果","流水号","备料日期","客户型号","版本号","产品类别","客户编码","客户名称","终端客户编码","终端客户名称",
                                            "产品代码","产品名称","产品型号","备料数量(粒)","营业员","产品用途","订料良率","产品类型","表面处理","是否半孔板",
                                            "是否出样","制单人","备料项目","备注","消耗计划" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("电子备料单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
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

                // "审核结果","流水号","备料日期","客户型号","版本号","产品类别","客户编码","客户名称","终端客户编码","终端客户名称",
                //"产品代码","产品名称","产品型号","备料数量(粒)","营业员","产品用途","订料良率","产品类型","表面处理","是否半孔板",
                //"是否出样","制单人","备料项目","备注"

                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.bill_date.ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.customer_pn);
                cells.Add(rowIndex, ++colIndex, d.h.version_no);
                cells.Add(rowIndex, ++colIndex, d.h.product_type);
                cells.Add(rowIndex, ++colIndex, d.h.customer_no);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.zz_customer_no);
                cells.Add(rowIndex, ++colIndex, d.h.zz_customer_name);

                cells.Add(rowIndex, ++colIndex, d.h.product_no);
                cells.Add(rowIndex, ++colIndex, d.h.product_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_model);
                cells.Add(rowIndex, ++colIndex, d.h.bl_qty);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.h.usage);
                cells.Add(rowIndex, ++colIndex, d.h.good_percent);
                cells.Add(rowIndex, ++colIndex, d.h.product_classification);
                cells.Add(rowIndex, ++colIndex, d.h.surface_type);
                cells.Add(rowIndex, ++colIndex, d.h.is_half_hole);

                cells.Add(rowIndex, ++colIndex, d.h.is_make_sample);
                cells.Add(rowIndex, ++colIndex, d.h.applier_name);
                cells.Add(rowIndex, ++colIndex, d.h.bl_project);
                cells.Add(rowIndex, ++colIndex, d.h.comment);
                cells.Add(rowIndex, ++colIndex, d.h.bl_plan);
            }

            xls.Send();
        }

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

            var result = from e in db.Sale_BL
                         join a in db.Apply on e.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || e.applier_id == userId)
                         && e.bill_date >= fd
                         && e.bill_date < td
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

            var result = from a in db.Apply
                         from ad in a.ApplyDetails
                         join e in db.Sale_BL on a.sys_no equals e.sys_no
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

            ExportExcel(result.OrderByDescending(r => r.h.id).Take(200).ToList());
        }

        public override void BeforeRollBack(int step)
        {
            
        }

    }
}