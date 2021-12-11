using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using Newtonsoft.Json;
using org.in2bits.MyXls;

namespace Sale_platform_ele.Services
{
    public class MXSv:BillSv
    {
        private Sale_MX bill;

        public MXSv() {  }

        public MXSv(string sysNo)
        {
            bill = db.Sale_MX.Single(m => m.sys_no == sysNo);
        }

        public override string BillType
        {
            get { return "MX"; }
        }

        public override string BillTypeName
        {
            get { return "修改/取消单"; }
        }

        public override string CreateViewName
        {
            get { return "CreateMX"; }
        }

        public override string CheckViewName
        {
            get { return "CheckMX"; }
        }

        public override string CheckListViewName
        {
            get { return "CheckMXList"; }
        }

        public override object GetNewBill(UserInfo currentUser)
        {
            var m = new MXCheckModel();
            m.bill = new Sale_MX()
            {
                sys_no = GetNextSysNo(BillType),
                applier_name = currentUser.realName,
                bill_date = DateTime.Now
            };
            return m;
        }

        public override object GetBill(int stepVersion)
        {
            var m = new MXCheckModel();
            m.bill = bill;
            m.detailsBefore = db.Sale_MX_detail.Where(d => d.sys_no==bill.sys_no && d.detail_type == "before").ToList();
            m.detailsAfter = db.Sale_MX_detail.Where(d => d.sys_no == bill.sys_no && d.detail_type == "after").ToList();

            return m;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            bill = new Sale_MX();
            SomeUtils.SetFieldValueToModel(fc, bill);
            List<Sale_MX_detail> beforeList = JsonConvert.DeserializeObject<List<Sale_MX_detail>>(fc.Get("before_details"));
            List<Sale_MX_detail> afterList = JsonConvert.DeserializeObject<List<Sale_MX_detail>>(fc.Get("after_details"));

            if (beforeList.Count() > 0) {
                if (fc.Get("before_details").Equals(fc.Get("after_details"))) {
                    return "原单明细表格内容和变更后表格内容不能完全一致";
                }
            }

            if ("取消".Equals(bill.tran_type) && "生产单".Equals(bill.bill_type)) {
                if (afterList.Where(a => decimal.Parse(a.relate_qty) > 0).Count() > 0) {
                    return "此单据存在K3关联数量（出货数量）不为0的记录，不能整单取消；如需修改数量，业务类型请选择【修改】";
                }
            }

            string[] p1 = new string[] { "型号", "数量", "含税单价", "成交价", "成本价" };
            string[] p2 = new string[] { "交货日期", "包装信息", "客户名称", "贸易类型", "其它" };
            if (string.IsNullOrEmpty(bill.change_project)) {
                return "变更项目至少需选择一项才能保存";
            }

            //变更项目包含一项以上的P1项时，表格必须要有数据
            if (p1.Any(p => bill.change_project.Contains(p))) {
                if (beforeList.Count() < 1 || afterList.Count()<1) {
                    return "你选择的变更项目要求在表格中处理，当前表格没有数据";
                }
            }
            if (p2.Any(p => bill.change_project.Contains(p))) {
                if (string.IsNullOrEmpty(bill.comment)) {
                    return "你选择的变更项目要求在附注中说明，当前附注没有内容";
                }
            }

            bill.applier_name = user.realName;
            bill.applier_id = user.userId;
            bill.apply_time = DateTime.Now;

            beforeList.ForEach(b => { b.detail_type = "before"; b.sys_no = bill.sys_no; });
            afterList.ForEach(a => { a.detail_type = "after"; a.sys_no = bill.sys_no; });
            var details = new List<Sale_MX_detail>();
            details.AddRange(beforeList);
            details.AddRange(afterList);

            try {
                var existBill = db.Sale_MX.Where(s => s.sys_no == bill.sys_no).FirstOrDefault();
                if (existBill != null) {
                    var existDetails = db.Sale_MX_detail.Where(s => s.sys_no == bill.sys_no).ToList();

                    //备份旧订单
                    BackupData bd = new BackupData();
                    bd.main_data = JsonConvert.SerializeObject(existBill);
                    bd.secondary_data = JsonConvert.SerializeObject(existDetails);
                    bd.op_date = DateTime.Now;
                    bd.sys_no = bill.sys_no;
                    bd.user_id = user.userId;
                    db.BackupData.InsertOnSubmit(bd);

                    //删除旧单
                    db.Sale_MX.DeleteOnSubmit(existBill);
                    db.Sale_MX_detail.DeleteAllOnSubmit(existDetails);
                }

                db.Sale_MX.InsertOnSubmit(bill);
                db.Sale_MX_detail.InsertAllOnSubmit(details);

                db.SubmitChanges();
            }
            catch (Exception ex) {
                return "保存失败：" + ex.Message;
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

            var result = from e in db.Sale_MX
                         join a in db.Apply on e.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         where (canCheckAll || e.applier_id == userId)
                         && e.bill_date >= fd
                         && e.bill_date < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new MXListModel()
                         {
                             billId = e.id,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                             billDate = e.bill_date.ToShortDateString(),
                             customerName = e.customer_name,
                             sysNo = e.sys_no,
                             billNo = e.bill_no,
                             billType = e.bill_type,
                             tranType = e.tran_type
                         };

            if (!string.IsNullOrWhiteSpace(pm.searchValue)) {
                result = result.Where(r => r.billNo.Contains(pm.searchValue));
            }

            if (!string.IsNullOrWhiteSpace(pm.customerName)) {
                result = result.Where(r => r.customerName.Contains(pm.customerName));
            }

            return result.OrderByDescending(r => r.billId).Take(200).ToList<object>();
        }

        public override object GetNewBillFromOld()
        {
            bill.bill_date = DateTime.Now;

            var m = new MXCheckModel();
            m.bill = bill;
            m.detailsBefore = db.Sale_MX_detail.Where(d => d.sys_no == bill.sys_no && d.detail_type == "before").ToList();
            m.detailsAfter = db.Sale_MX_detail.Where(d => d.sys_no == bill.sys_no && d.detail_type == "after").ToList();

            m.bill.sys_no = GetNextSysNo(BillType);
            return m;

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
            return db.Sale_MX_detail.Where(d => d.sys_no == bill.sys_no).Select(d => d.item_model).FirstOrDefault();
        }

        public override string GetCustomerName()
        {
            return bill.customer_name;
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.Sale_MX.Where(m => m.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return bill.tran_type + bill.bill_type;
        }

        public override void DoWhenBeforeApply()
        {
            
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                //生成编码
                bill.code_num = GetNextNo("PCB", DateTime.Now.ToString("yy-"), 3);
                db.SubmitChanges();
            }
        }

        private class ExcelData
        {
            public Sale_MX h { get; set; }
            public Sale_MX_detail e { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            //列名：
            string[] colName = new string[] { "审核结果","流水号","下单日期","业务类型","单据类型","单据编号","客户名称","办事处","营业员",
                                            "制单人","变更项目","附注","明细类型","单据行号","物料名称","规格型号","数量","成交价","含税单价",
                                            "成本价","成交金额","交货日期","币别","K3关联数量" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("电子修改取消单单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
            Worksheet sheet = xls.Workbook.Worksheets.Add("单据列表");

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

                //"审核结果","流水号","下单日期","业务类型","单据类型","单据编号","客户名称","办事处","营业员",
                //"制单人","变更项目","附注","明细类型","单据行号","物料名称","规格型号","数量","成交价","含税单价",
                //"成本价","成交金额","交货日期","币别","K3关联数量"

                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.bill_date.ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.tran_type);
                cells.Add(rowIndex, ++colIndex, d.h.bill_type);
                cells.Add(rowIndex, ++colIndex, d.h.bill_no);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.agency_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);

                cells.Add(rowIndex, ++colIndex, d.h.applier_name);
                cells.Add(rowIndex, ++colIndex, d.h.change_project);
                cells.Add(rowIndex, ++colIndex, d.h.comment);
                cells.Add(rowIndex, ++colIndex, d.e.detail_type == "before" ? "原单" : "改单");
                cells.Add(rowIndex, ++colIndex, d.e.entry_no);
                cells.Add(rowIndex, ++colIndex, d.e.item_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_model);
                cells.Add(rowIndex, ++colIndex, d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.deal_price);
                cells.Add(rowIndex, ++colIndex, d.e.tax_price);

                cells.Add(rowIndex, ++colIndex, d.e.cost);
                cells.Add(rowIndex, ++colIndex, d.e.deal_sum);
                cells.Add(rowIndex, ++colIndex, d.e.fetch_date);
                cells.Add(rowIndex, ++colIndex, d.e.currency_name);
                cells.Add(rowIndex, ++colIndex, d.e.relate_qty);
            }

            xls.Send();
        }

        public override void ExportSalerExcle(SalerSearchParamModel pm, int userId)
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

            var result = from h in db.Sale_MX
                         join a in db.Apply on h.sys_no equals a.sys_no into X
                         from Y in X.DefaultIfEmpty()
                         join e in db.Sale_MX_detail on h.sys_no equals e.sys_no into et
                         from ef in et.DefaultIfEmpty()
                         where (canCheckAll || h.applier_id == userId)
                         && h.bill_date >= fd
                         && h.bill_date < td
                         && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                         select new ExcelData()
                         {
                             h = h,
                             e = ef,
                             auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                         };

            if (!string.IsNullOrWhiteSpace(pm.searchValue)) {
                result = result.Where(r => r.h.bill_no.Contains(pm.searchValue));
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
                         join h in db.Sale_MX on a.sys_no equals h.sys_no
                         join e in db.Sale_MX_detail on h.sys_no equals e.sys_no into et
                         from ef in et.DefaultIfEmpty()
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
                             h = h,
                             e = ef,
                             auditStatus = a.success == true ? "PASS" : a.success == false ? "NG" : "----"
                         };

            if (!string.IsNullOrWhiteSpace(pm.saler)) {
                result = result.Where(r => r.h.applier_name.Contains(pm.saler));
            }

            if (!string.IsNullOrWhiteSpace(pm.sysNo)) {
                result = result.Where(r => r.h.sys_no.Contains(pm.sysNo));
            }

            if (!string.IsNullOrWhiteSpace(pm.proModel)) {
                result = result.Where(r => r.e.item_model.Contains(pm.proModel));
            }

            ExportExcel(result.OrderByDescending(r => r.h.id).Take(200).ToList());
        }

        public override void BeforeRollBack(int step)
        {
            
        }

        public object GetK3OrderForMX(string billNo)
        {
            return db.getK3OrderForMx(billNo).ToList();
        }

    }
}