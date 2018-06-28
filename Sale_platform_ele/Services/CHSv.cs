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
    public class CHSv:BillSv
    {
        const string BILL_TYPE = "CH";
        const string BILL_TYPE_NAME = "出货申请单";
        const string CREATE_VIEW_NAME = "CreateCH";
        const string CHECK_VIEW_NAME = "CheckCH";
        const string CHECK_VIEW_LIST_NAME = "CheckCHList";
        private ChBill bill;

        public CHSv()
        {

        }

        public CHSv(string sysNo)
        {
            try {
                bill = db.ChBill.Single(c => c.sys_no == sysNo);
            }
            catch{
                throw new Exception("出货流水号不存在");
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

        public override object GetNewBill(int userId)
        {
            var vw = new vwChBill();
            vw.sys_no = GetNextSysNo("CH");
            vw.bill_date = DateTime.Now;
            vw.real_name = new UA(userId).GetUser().real_name;

            List<vwChBill> list = new List<vwChBill>();
            list.Add(vw);
            return list;
        }

        public override object GetBill(int stepVersion)
        {
            var vw = db.vwChBill.Where(v => v.sys_no == bill.sys_no).ToList();
            return vw;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, int userId)
        {
            bill = new ChBill();
            SomeUtils.SetFieldValueToModel(fc, bill);
            bill.ChBillDetail.AddRange(JsonConvert.DeserializeObject<List<ChBillDetail>>(fc.Get("ch_bill_details")));

            try {
                //保存送货地址信息
                var dInfos = new OtherSv().GetDeliveryInfo(bill.customer_no)
                    .Where(d => d.deliveryUnit == bill.delivery_unit && d.attn == bill.delivery_attn
                        && d.addr == bill.delivery_addr && d.phone == bill.delivery_phone).Count();
                if (dInfos == 0) {
                    var info = new CustomerDeliveryInfo()
                    {
                        customer_name = bill.customer_name,
                        customer_number = bill.customer_no,
                        delivery_unit = bill.delivery_unit,
                        addr = bill.delivery_addr,
                        attn = bill.delivery_attn,
                        phone = bill.delivery_phone,
                        op_date = DateTime.Now,
                        op_name = new UA(userId).GetUser().real_name
                    };
                    db.CustomerDeliveryInfo.InsertOnSubmit(info);                    
                }

                var existsed = db.ChBill.Where(c => c.sys_no == bill.sys_no).ToList();
                if (existsed.Count() > 0) {
                    //备份旧单
                    BackupData bd = new BackupData();
                    bd.sys_no = bill.sys_no;
                    bd.op_date = DateTime.Now;
                    bd.user_id = userId;
                    bd.main_data = SomeUtils.ModelToString<ChBill>(existsed.First());
                    bd.secondary_data = SomeUtils.ModelsToString<ChBillDetail>(existsed.First().ChBillDetail.ToList());
                    db.BackupData.InsertOnSubmit(bd);


                    //删掉旧单
                    db.ChBillDetail.DeleteAllOnSubmit(existsed.First().ChBillDetail);
                    db.ChBill.DeleteAllOnSubmit(existsed);
                }
                bill.bill_date = DateTime.Now;
                bill.user_id = userId;
                db.ChBill.InsertOnSubmit(bill);
                db.ChBillDetail.InsertAllOnSubmit(bill.ChBillDetail);
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
            pm.searchValue = pm.searchValue ?? "";
            pm.customerName = pm.customerName ?? "";

            DateTime fd, td;
            if (!DateTime.TryParse(pm.fromDate, out fd)) {
                fd = DateTime.Parse("2017-01-01");
            }
            if (!DateTime.TryParse(pm.toDate, out td)) {
                td = DateTime.Parse("2049-09-09");
            }
            td = td.AddDays(1);

            var result = (from o in db.ChBill
                          from d in o.ChBillDetail
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where (canCheckAll || o.user_id == userId)
                          && o.bill_date >= fd
                          && o.bill_date <= td
                          && (o.sys_no.Contains(pm.searchValue) || d.item_model.Contains(pm.searchValue))
                          && (o.customer_name.Contains(pm.customerName) || o.customer_no.Contains(pm.customerName))
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.bill_date descending
                          select new CHListModel()
                          {
                              billId = o.id,
                              billDate = DateTime.Parse(o.bill_date.ToString()).ToString("yyyy-MM-dd"),
                              qty = d.apply_qty.ToString(),                              
                              sysNo = o.sys_no,
                              customerName = o.customer_name,
                              productModel = d.item_model,
                              productName = d.item_name,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中"),
                              k3AuditDate = o.k3_audit2_date == null ? "未出库" : DateTime.Parse(o.k3_audit2_date.ToString()).ToString("yyyy-MM-dd"),
                              orderNo=d.order_no,
                              orderEntryNo=d.order_entry_no,
                              productType=o.product_type
                          }).Take(200).ToList<object>();

            return result;
        }

        public override object GetNewBillFromOld()
        {
            var vw = db.vwChBill.Where(v => v.sys_no == bill.sys_no).ToList();
            string newSysNo = GetNextSysNo(BillType);
            vw.ForEach(v =>v.sys_no = newSysNo);

            return vw;
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
            var detailsCount = bill.ChBillDetail.Count();
            if (detailsCount == 1) {
                return bill.ChBillDetail.First().item_model;
            }
            else {
                return bill.ChBillDetail.First().item_model + "...等" + detailsCount + "个";
            }
        }

        public override bool HasOrderSaved(string sysNo)
        {
            return db.ChBill.Where(c => c.sys_no == sysNo).Count() > 0;
        }

        public override string GetSpecificBillTypeName()
        {
            return BillTypeName;
        }

        public override void DoWhenBeforeApply()
        {
            foreach (var d in bill.ChBillDetail) {
                //先检查是否有未结束的同一张订单分录出货申请
                var existsedApplies = (from c in db.ChBill
                                       from cd in c.ChBillDetail
                                       from a in db.Apply
                                       where c.sys_no == a.sys_no
                                       && (a.success == null || a.success == true)
                                       && c.k3_stock_no == null
                                       select c.sys_no).ToList();
                if (existsedApplies.Count() > 0) {
                    throw new Exception("此订单存在未出库的出货申请，在出库前不能再次申请：#订单号：" + d.order_no + "#出货流水号：" + existsedApplies.First() + "#");
                }
            }

            ValidateK3StockAndRelateQty();
        }

        private void ValidateK3StockAndRelateQty(bool hasSubmited = false)
        {
            foreach (var d in bill.ChBillDetail) {
                var k3Info = db.vwK3OrderInfo.Where(k => k.orderId == d.order_id && k.orderEntry == d.order_entry_no).ToList();
                if (k3Info.Count() > 0) {
                    var ki = k3Info.First();
                    var stockQty = ki.stockQty;
                    if (hasSubmited) {
                        //已提交的需要加上本单的数量
                        stockQty += bill.ChBillDetail.Where(c => c.item_id == d.item_id).Sum(c => c.apply_qty);                        
                    }                    
                    if (d.apply_qty > stockQty) {
                        throw new Exception("申请数量不能大于库存数量：#单号：" + d.order_no + "#行号：" + d.order_entry_no + "#库存数：" + stockQty + "#");
                    }
                    
                    if (d.apply_qty > ki.qty - ki.relateQty) {
                        throw new Exception("申请数量不能大于k3可出数量：#单号：" + d.order_no + "#行号：" + d.order_entry_no + "#可出数：" + (ki.qty - ki.relateQty) + "#");
                    }
                }
                else {
                    throw new Exception("当前订单不存在#" + d.order_no + "#" + d.order_entry_no + "#");
                }
            }
        }

        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            ValidateK3StockAndRelateQty(true);
        }

        public override void DoWhenFinishAudit(bool isPass)
        {
            
        }

        /// <summary>
        /// 导出excel数据的模型
        /// </summary>
        private class ExcelData
        {
            public ChBill h { get; set; }
            public ChBillDetail e { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出SO的excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            //列宽：
            ushort[] colWidth = new ushort[] {16,16,16,14,14,16,28,14,16,
                                            28,28,16,16,32,14,18,24,12,18,
                                            18,16,16,24,24,12,24,18};

            //列名：
            string[] colName = new string[] { "审核结果","流水号","下单日期","制单人","产品类别","客户编码","客户名称","营业员","营业员电话",
                                            "备注","收货单位","ATTN","收货电话","收货地址","产品代码","产品名称","规格型号","单位","订单数量",
                                            "申请数量","客户P/O","客户P/N","行备注","订单号","订单行号","出库单号","出库日期" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("出货申请单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
            Worksheet sheet = xls.Workbook.Worksheets.Add("出货信息列表");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
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
                //"审核结果","流水号","下单日期","办事处","产品类别","客户编码","客户名称","营业员","营业员电话",
                //"备注","收货单位","ATTN","收货电话","收货地址","产品代码","产品名称","规格型号","单位","订单数量",
                //"申请数量","客户P/O","客户P/N","行备注","订单号","订单行号","出库单号","出库日期"
                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.h.bill_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.User.real_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_type);
                cells.Add(rowIndex, ++colIndex, d.h.customer_no);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_name);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_phone);

                cells.Add(rowIndex, ++colIndex, d.h.comment);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_unit);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_attn);
                cells.Add(rowIndex, ++colIndex, d.h.clerk_phone);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_addr);
                cells.Add(rowIndex, ++colIndex, d.e.item_no);
                cells.Add(rowIndex, ++colIndex, d.e.item_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_model);
                cells.Add(rowIndex, ++colIndex, d.e.unit_name);
                cells.Add(rowIndex, ++colIndex, d.e.order_qty);

                cells.Add(rowIndex, ++colIndex, d.e.apply_qty);
                cells.Add(rowIndex, ++colIndex, d.e.customer_po);
                cells.Add(rowIndex, ++colIndex, d.e.customer_pn);
                cells.Add(rowIndex, ++colIndex, d.e.comment);
                cells.Add(rowIndex, ++colIndex, d.e.order_no);
                cells.Add(rowIndex, ++colIndex, d.e.order_entry_no);
                cells.Add(rowIndex, ++colIndex, d.h.k3_stock_no);
                cells.Add(rowIndex, ++colIndex, d.h.k3_audit2_date == null ? "" : ((DateTime)d.h.k3_audit2_date).ToString("yyyy-MM-dd"));
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
            pm.searchValue = pm.searchValue ?? "";
            pm.customerName = pm.customerName ?? "";
            DateTime fd, td;
            if (!DateTime.TryParse(pm.fromDate, out fd)) {
                fd = DateTime.Parse("2017-01-01");
            }
            if (!DateTime.TryParse(pm.toDate, out td)) {
                td = DateTime.Parse("2049-09-09");
            }
            td = td.AddDays(1);

            var result = (from o in db.ChBill
                          from d in o.ChBillDetail
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where (canCheckAll || o.user_id == userId)
                          && o.bill_date >= fd
                          && o.bill_date <= td
                          && (o.sys_no.Contains(pm.searchValue) || d.item_model.Contains(pm.searchValue))
                          && (o.customer_name.Contains(pm.customerName) || o.customer_no.Contains(pm.customerName))
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.bill_date descending
                          select new ExcelData()
                          {
                              h = o,
                              e = d,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中")
                          }).Take(200).ToList();

            ExportExcel(result);
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
                          join o in db.ChBill on a.sys_no equals o.sys_no
                          from d in o.ChBillDetail
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
                              e = d,
                              auditStatus = a.success == true ? "PASS" : a.success == false ? "NG" : "----"
                          }).Take(200).ToList();

            ExportExcel(result);
        }

        public List<ClerkAndCustomerModel> GetCleckAndCustomerList(string searchValue)
        {
            return (from c in db.ClerkAndCustomer
                    from d in db.Department
                    where c.User.department_no == d.dep_no
                    && (
                    c.customer_name.Contains(searchValue)
                    || c.customer_number.Contains(searchValue)
                    || c.User.real_name.Contains(searchValue)
                    || c.User.username.Contains(searchValue)
                    || d.name.Contains(searchValue))
                    select new ClerkAndCustomerModel()
                    {
                        id = c.id,
                        agency = d.name,
                        clerkId = c.clerk_id,
                        clerkName = c.User.real_name,
                        clerkNumber = c.User.username,
                        customerName = c.customer_name,
                        customerNumber = c.customer_number
                    }).ToList();
        }

        public string SaveClerkAndCustomer(int clerkId, string customerName, string customerNumber)
        {
            if (new OtherSv().GetCustomerId(customerNumber) == 0) {
                return "客户不存在；请输入后按回车键搜索然后在列表中选取";
            }

            if (db.ClerkAndCustomer.Where(c => c.clerk_id == clerkId && c.customer_number == customerNumber).Count() > 0) {
                return "此关系已存在，请不要重复添加";
            }

            try {
                var cc = new ClerkAndCustomer()
                    {
                        clerk_id = clerkId,
                        customer_number = customerNumber,
                        customer_name = customerName
                    };
                db.ClerkAndCustomer.InsertOnSubmit(cc);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return "保存失败：" + ex.Message;
            }

            return "";
        }

        public string UpdateClerkAndCustomer(int id, int clerkId, string customerName, string customerNumber)
        {
            if (new OtherSv().GetCustomerId(customerNumber) == 0) {
                return "客户不存在；请输入后按回车键搜索然后在列表中选取";
            }

            if (db.ClerkAndCustomer.Where(c => c.clerk_id == clerkId && c.customer_number == customerNumber).Count() > 0) {
                return "此关系已存在，请不要重复添加";
            }

            if (db.ClerkAndCustomer.Where(c => c.id == id).Count() == 0) {
                return "数据已过期，请刷新页面";
            }

            try {
                var cc = db.ClerkAndCustomer.Single(c => c.id == id);
                cc.clerk_id = clerkId;
                cc.customer_number = customerNumber;
                cc.customer_name = customerName;

                db.SubmitChanges();
            }
            catch (Exception ex) {
                return "保存失败：" + ex.Message;
            }

            return "";
        }

        public string RemoveClerkAndCustomer(int id)
        {
            try {
                db.ClerkAndCustomer.DeleteAllOnSubmit(db.ClerkAndCustomer.Where(c => c.id == id));
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return "删除失败：" + ex.Message;
            }
            return "";
        }

        public List<vwK3OrderInfo> GetOrderStockInfo(string customerNumber, string saleStyle, string productType, DateTime fromDate, DateTime toDate,
            string orderNumber, string productModel, string hasStockQty, string isClosed)
        {
            var result = from v in db.vwK3OrderInfo
                         where v.orderDate >= fromDate
                         && v.orderDate <= toDate
                         && v.orderNumber.Contains(orderNumber)
                         && v.itemModel.Contains(productModel)
                         select v;
            if (!string.IsNullOrEmpty(customerNumber) && !"*".Equals(customerNumber)) {
                result = result.Where(r => r.customerNumber == customerNumber);
            }
            if (!string.IsNullOrEmpty(saleStyle)) {
                result = result.Where(r => r.saleStyle == saleStyle);
            }
            if (!string.IsNullOrEmpty(productType)) {
                string[] pTypeArr = productType.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                result = result.Where(r => pTypeArr.Contains(r.productType));
            }
            if ("1".Equals(hasStockQty)) {
                result = result.Where(r => r.stockQty > 0);
            }
            else if ("0".Equals(hasStockQty)) {
                result = result.Where(r => r.stockQty == 0);
            }
            if ("1".Equals(isClosed)) {
                result = result.Where(r => r.isclosed > 0);
            }
            else if ("0".Equals(isClosed)) {
                result = result.Where(r => r.isclosed == 0 && r.qty > r.relateQty);
            }

            return result.OrderBy(r => r.orderDate).ToList();
        }

        public List<vwK3StockInfo> GetK3StockInfo(int orderId, int orderEntryId)
        {
            return db.vwK3StockInfo.Where(v => v.orderID == orderId && v.orderEntryId == orderEntryId).OrderBy(v => v.stockDate).ToList();
        }

        public List<StockTeamReport> GetStockTeamReport(string sysNo, string stockNo, string orderNo,string customer,string model, DateTime fromDate, DateTime toDate,int userId)
        {
            //先检查出货组权限
            List<string> ptypes = new List<string>();
            if (new UA(userId).HasGotPower("CH_FPC_Team")) {
                ptypes.Add("FPC/软硬结合板");
                ptypes.Add("FPC样品/FPC开模");
            }
            if (new UA(userId).HasGotPower("CH_PCB_Team")) {
                ptypes.Add("PCB/HDI");
                ptypes.Add("PCB样品/HDI样品/PCB开模/HDI开模");
            }
            string[] ptypeArr=ptypes.ToArray();

            var result = (from c in db.ChBill
                          from d in c.ChBillDetail
                          where c.k3_stock_no != null
                          && c.sys_no.Contains(sysNo)
                          && c.k3_stock_no.Contains(stockNo)
                          && d.order_no.Contains(orderNo)
                          && c.k3_audit1_date > fromDate
                          && c.k3_audit1_date < toDate
                          && (c.customer_name.Contains(customer) || c.customer_no.Contains(customer))
                          && ptypeArr.Contains(c.product_type)
                          && d.item_model.Contains(model)
                          orderby c.k3_audit1_date
                          select new StockTeamReport()
                          {
                              chDetailId = d.id,
                              sysNo = c.sys_no,
                              stockAudit1Date = DateTime.Parse(c.k3_audit1_date.ToString()).ToString("yyyy-MM-dd HH:mm"),
                              k3StockNo = c.k3_stock_no,
                              customerName = c.customer_name,
                              billName = c.User.real_name,
                              productType = c.product_type,
                              itemModel = d.item_model,
                              itemName = d.item_name,
                              applyQty = d.apply_qty,
                              unitName = d.unit_name,
                              orderNo = d.order_no,
                              orderEntryNo = d.order_entry_no.ToString(),
                              deliveryNumber = d.delivery_num,
                              cardboardNum = d.cardboard_num,
                              cycle = d.cycle,
                              packs = d.packs,
                              customerPN = d.customer_pn,
                              customerPO = d.customer_po,
                              deliveryAddr = c.delivery_addr,
                              deliveryUnit = c.delivery_unit
                          }).Take(200).ToList();
            return result;
        }

        public string UpdateCHPackInfo(int detailId, string deliveryNumber, int cardboardNum, int packs, string cycle)
        {
            try {
                var detail = db.ChBillDetail.Single(c => c.id == detailId);
                detail.delivery_num = deliveryNumber;
                detail.cardboard_num = cardboardNum;
                detail.packs = packs;
                detail.cycle = cycle;
                db.SubmitChanges();
                return "";
            }
            catch(Exception ex) {
                return ex.Message;
            }
        }

        //出货组报表
        public void ExportStockTeamExcel(string sysNo, string stockNo, string orderNo, string customer, string model, DateTime fromDate, DateTime toDate, int userId)
        {
            var datas = GetStockTeamReport(sysNo, stockNo, orderNo, customer, model, fromDate, toDate, userId);

            //列宽：
            ushort[] colWidth = new ushort[] {26,24,16,16,20,32,12,10,12,10,
                                            18,18,24,48 };

            //列名：
            string[] colName = new string[] { "销售订单号","客户名称","客户P/O","客户P/N","产品名称","规格型号","出货数量","单位","叉板数","件数",
                                            "快递单号","周期","收货单位","送货地址" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("出货报表_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
            Worksheet sheet = xls.Workbook.Worksheets.Add("出货报表信息");

            //设置各种样式

            //标题样式
            XF boldXF = xls.NewXF();
            boldXF.HorizontalAlignment = HorizontalAlignments.Centered;
            boldXF.Font.Height = 12 * 20;
            boldXF.Font.FontName = "宋体";
            boldXF.Font.Bold = true;

            //设置列宽
            ColumnInfo col;
            for (ushort i = 0; i < colWidth.Length; i++) {
                col = new ColumnInfo(xls, sheet);
                col.ColumnIndexStart = i;
                col.ColumnIndexEnd = i;
                col.Width = (ushort)(colWidth[i] * 256);
                sheet.AddColumnInfo(col);
            }

            Cells cells = sheet.Cells;
            int rowIndex = 1;
            int colIndex = 1;

            //设置标题
            foreach (var name in colName) {
                cells.Add(rowIndex, colIndex++, name, boldXF);
            }
            foreach (var d in datas) {
                colIndex = 1;
                //"销售订单号","客户名称","客户P/O","客户P/N","产品名称","规格型号","出货数量","单位","叉板数","件数",
                //"快递单号","周期","收货单位","送货地址"
                cells.Add(++rowIndex, colIndex, d.orderNo);
                cells.Add(rowIndex, ++colIndex, d.customerName);
                cells.Add(rowIndex, ++colIndex, d.customerPO);
                cells.Add(rowIndex, ++colIndex, d.customerPN);
                cells.Add(rowIndex, ++colIndex, d.itemName);
                cells.Add(rowIndex, ++colIndex, d.itemModel);
                cells.Add(rowIndex, ++colIndex, d.applyQty);
                cells.Add(rowIndex, ++colIndex, d.unitName);
                cells.Add(rowIndex, ++colIndex, d.cardboardNum);
                cells.Add(rowIndex, ++colIndex, d.packs);

                cells.Add(rowIndex, ++colIndex, d.deliveryNumber);
                cells.Add(rowIndex, ++colIndex, d.cycle);
                cells.Add(rowIndex, ++colIndex, d.deliveryUnit);
                cells.Add(rowIndex, ++colIndex, d.deliveryAddr);
            }

            xls.Send();
        }

        //打印送货单
        public List<VwChPrintReport> GetChReportData(string sysNo)
        {
            return db.VwChPrintReport.Where(v => v.sys_no == sysNo).ToList();
        }
    }
}