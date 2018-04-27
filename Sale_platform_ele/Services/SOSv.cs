using Newtonsoft.Json;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using org.in2bits.MyXls;

namespace Sale_platform_ele.Services
{
    public class SOSv : BillSv
    {
        const string BILL_TYPE = "SO";
        const string BILL_TYPE_NAME = "销售订单";
        const string CREATE_VIEW_NAME = "CreateOrder";
        const string CHECK_VIEW_NAME = "CheckOrder";
        private Order order;

        /// <summary>
        /// 无参构造
        /// </summary>
        public SOSv()
        {

        }

        /// <summary>
        /// 有参构造
        /// </summary>
        /// <param name="sysNo">流水号</param>
        public SOSv(string sysNo)
        {
            try {
                order = db.Order.Single(o => o.sys_no == sysNo);
            }
            catch {                
                throw new Exception("销售订单系统流水号不存在");
            }
        }

        /// <summary>
        /// 单据类型EN
        /// </summary>
        public override string BillType
        {
            get
            {
                return BILL_TYPE;
            }
        }

        /// <summary>
        /// 单据类型中文
        /// </summary>
        public override string BillTypeName
        {
            get { return BILL_TYPE_NAME; }
        }        

        /// <summary>
        /// 新建单据视图
        /// </summary>
        public override string CreateViewName
        {
            get { return CREATE_VIEW_NAME; }
        }

        /// <summary>
        /// 查看单据视图
        /// </summary>
        public override string CheckViewName
        {
            get
            {
                return CHECK_VIEW_NAME;
            }
        }

        
        /// <summary>
        /// (无参构造)获取新建单据的对象实例
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override object GetNewBill(int userId)
        {
            UA ua = new UA(userId);
            order = new Order();
            order.sys_no = GetNextSysNo(BILL_TYPE);
            order.order_date = DateTime.Now;
            order.step_version = 0;
            order.User = ua.GetUser();

            var agencys = new OtherSv().GetItems("agency").Where(a => a.name == ua.GetUserDepartmentName());
            if (agencys.Count() > 0) {
                order.agency_no = agencys.First().value;
                order.agency_name = agencys.First().name;
            }

            return order;
        }

        /// <summary>
        /// (有参构造)获得已有单据的对象实例
        /// </summary>
        /// <param name="stepVersion"></param>
        /// <returns></returns>
        public override object GetBill(int stepVersion)
        {
            if (string.IsNullOrEmpty(order.order_no)) {
                if (db.Apply.Where(a => a.sys_no == order.sys_no && a.success == true).Count() > 0) {
                    //如果订单号为空，并且已申请结束OK的，去K3把订单号带过来。
                    var k3Orders = db.getK3OrderNo(order.sys_no).ToList();
                    if (k3Orders.Count() > 0) {
                        order.order_no = k3Orders.First().FBillNo;
                    }
                    db.SubmitChanges();
                }
            }

            order.step_version = stepVersion;
            return order;
        }

        /// <summary>
        /// (无参构造)单据是否已保存
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public override bool HasOrderSaved(string sysNo)
        {
            return db.Order.Where(o => o.sys_no == sysNo).Count() > 0;
        }

        /// <summary>
        /// (无参构造)保存单据
        /// </summary>
        /// <param name="fc">表单form</param>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public override string SaveBill(System.Web.Mvc.FormCollection fc, int userId)
        {
            order = new Order();
            SomeUtils.SetFieldValueToModel(fc, order);
            order.OrderDetail.AddRange(JsonConvert.DeserializeObject<List<OrderDetail>>(fc.Get("Sale_order_details")));

            string validateResult = ValidateSO(order);
            if (!string.IsNullOrEmpty(validateResult)) {
                return validateResult;
            }

            if (string.IsNullOrEmpty(order.oversea_customer_name) || string.IsNullOrEmpty(order.oversea_customer_no)) {
                order.oversea_customer_no = "";
                order.oversea_customer_name = "";
            }
            if (string.IsNullOrEmpty(order.clerk2_name) || string.IsNullOrEmpty(order.clerk2_no)) {
                order.clerk2_no = "";
                order.clerk2_name = "";
            }

            try {
                var existed = db.Order.Where(o => o.sys_no == order.sys_no);
                order.update_user_id = userId;
                if (existed.Count() > 0) {
                    order.original_id = existed.First().original_id;
                    //备份旧订单
                    BackupData bd = new BackupData();
                    bd.main_data = SomeUtils.ModelToString<Order>(existed.First());
                    bd.secondary_data = SomeUtils.ModelsToString<OrderDetail>(existed.First().OrderDetail.ToList());
                    bd.op_date = DateTime.Now;
                    bd.sys_no = order.sys_no;
                    bd.user_id = userId;
                    db.BackupData.InsertOnSubmit(bd);

                    //删除旧订单
                    db.OrderDetail.DeleteAllOnSubmit(existed.First().OrderDetail);
                    db.Order.DeleteAllOnSubmit(existed);
                }
                else {
                    order.original_id = userId;
                }

                //在后台再计算一次佣金、MU等
                CommissionSv csv = new CommissionSv();
                foreach (var d in order.OrderDetail) {
                    d.MU = csv.GetMU((decimal)d.deal_price, (decimal)d.cost,(int)d.tax_rate, (int)d.fee_rate, (decimal)order.exchange_rate);
                    d.commission_rate = csv.GetCommissionRate((decimal)d.MU, order.product_type_no);
                    d.commission = csv.GetCommissionMoney((decimal)d.deal_price , (decimal)d.qty, (decimal)d.commission_rate);
                }

                db.Order.InsertOnSubmit(order);
                db.OrderDetail.InsertAllOnSubmit(order.OrderDetail);
                db.SubmitChanges();
            }
            catch (Exception ex) {
                return ex.Message;
            }

            return "";
        }

        /// <summary>
        /// 保存时验证订单合法性
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private string ValidateSO(Order order)
        {
            if (order.OrderDetail.Count() == 0) {
                return "订单明细至少应录入一行，保存失败！";
            }

            if (string.IsNullOrEmpty(order.customer_no)) {
                return "购货单位不能为空，请输入编码或名称后按回车后在列表中选择";
            }
            if (string.IsNullOrEmpty(order.oversea_customer_no) && !string.IsNullOrEmpty(order.oversea_customer_name)) {
                return "海外客户请输入编码或名称后按回车后在列表中选择";
            }
            if (string.IsNullOrEmpty(order.clerk1_no)) {
                return "业务员1不能为空，请输入厂牌或姓名后按回车后在列表中选择";
            }
            if (string.IsNullOrEmpty(order.clerk2_no) && !string.IsNullOrEmpty(order.clerk2_name)) {
                return "业务员2请输入厂牌或姓名后按回车后在列表中选择";
            }

            bool isForeignOrder = !order.currency_no.Equals("RMB");
            if (isForeignOrder) {
                if (order.order_type_name.Equals("生产单")) {
                    if (!order.customer_no.Equals("00.01")) {
                        return "订单类型为生产单的国外订单，购货客户必须为香港信利电子有限公司（00.01）";
                    }
                    if (string.IsNullOrEmpty(order.oversea_customer_no)) {
                        return "订单类型为生产单的国外订单，国外客户不能为空";
                    }
                    if (!order.oversea_customer_no.StartsWith("04.2")) {
                        return "订单类型为生产单的国外订单，国外客户必须是04.2开头";
                    }
                }
                if (!order.delivery_place_no.Equals("HK")) {
                    return "国外单的发货地点必须选择香港货仓";
                }
                if (!order.receive_place_no.Equals("HK")) {
                    return "国外单的交货属性必须选择香港";
                }
                if (string.IsNullOrEmpty(order.clear_way_no)) {
                    return "国外单的结算方式不能为空";
                }
                else if(!order.clear_way_no.StartsWith("HK")) {
                    return "国外单的结算方式编码必须以HK开头";
                }
            }
            else {
                if (!order.delivery_place_no.Equals("SW")) {
                    return "国内单的发货地点必须选择汕尾物流中心";
                }
                if (!order.receive_place_no.Equals("GN")) {
                    return "国内单的交货属性必须选择国内";
                }
            }

            if (Math.Abs((order.percent1 ?? 0m) + (order.percent2 ?? 0m) - 100m) > 0.000001m) {
                return "比例1和比例2之和必须等于100！";
            }

            foreach (var d in order.OrderDetail) {
                if (d.fetch_date <= order.order_date) {
                    return "规格型号为【" + d.item_model + "】的产品，交货日期必须迟于订单日期";
                }
                if (isForeignOrder && d.tax_rate != 0) {
                    return "国外单的税率必须是0，请重新编辑订单明细";
                }
                if (!isForeignOrder && d.tax_rate != 17) {
                    return "国内单的税率必须是17，请重新编辑订单明细";
                }
                if (d.tax_price == 0) {
                    return "规格型号为【" + d.item_model + "】的产品，含税单价不能为0。如果是免费单，请在成交价录入0，含税单价录入成本价";
                }
            }

            return "";
        }

        /// <summary>
        /// 获得单据列表
        /// </summary>
        /// <param name="pm">查询条件模型</param>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
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

            var result = (from o in db.Order
                          from d in o.OrderDetail
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where (canCheckAll || o.original_id == userId)
                          && o.order_date >= fd
                          && o.order_date <= td
                          && (o.sys_no.Contains(pm.searchValue) || d.item_model.Contains(pm.searchValue))
                          && o.customer_name.Contains(pm.customerName)
                          && (pm.productType==null || o.product_type_name.Contains(pm.productType))
                          && (pm.saleRange==null || (pm.saleRange=="内销" && o.currency_no=="RMB") ||(pm.saleRange=="外销" && o.currency_no!="RMB"))
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.order_date descending
                          select new SOListModel()
                          {
                              orderId = o.id,
                              orderDate = DateTime.Parse(o.order_date.ToString()).ToString("yyyy-MM-dd"),
                              qty = d.qty.ToString(),
                              dealPrice = d.deal_price.ToString(),
                              sysNo = o.sys_no,
                              customerName = o.customer_name,
                              productModel = d.item_model,
                              productName = d.item_name,
                              auditStatus = (Y == null ? "未开始申请" : Y.success == true ? "申请成功" : Y.success == false ? "申请失败" : "审批之中")
                          }).Take(200).ToList();

            var list = new List<object>();
            list.AddRange(result);

            return list;
        }

        /// <summary>
        /// (有参构造)获取流程代码
        /// </summary>
        /// <returns></returns>
        public override string GetProcessNo()
        {
            return BILL_TYPE;
        }


        /// <summary>
        /// (有参构造)从旧的订单实例新建一张新的
        /// </summary>
        /// <returns></returns>
        public override object GetNewBillFromOld()
        {
            order.sys_no = GetNextSysNo(BILL_TYPE);
            order.order_date = DateTime.Now;
            order.step_version = 0;
            order.order_no = "";

            return order;
        }


        /// <summary>
        /// (有参构造)获取单据详细类型名（单据类别）
        /// </summary>
        /// <returns></returns>
        public override string GetSpecificBillTypeName()
        {
            if (!string.IsNullOrEmpty(order.order_type_name)) {
                return order.order_type_name;
            }
            else {
                return BILL_TYPE_NAME;
            }
        }

        /// <summary>
        /// (有参构造)取得流程所需要的审核人对应部门字典
        /// </summary>
        /// <returns></returns>
        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", order.User.department_no);

            return dic;
        }

        /// <summary>
        /// (有参构造)取得产品型号
        /// </summary>
        /// <returns></returns>
        public override string GetProductModel()
        {
            var detailsCount = order.OrderDetail.Count();
            if (detailsCount == 1) {
                return order.OrderDetail.First().item_model;
            }
            else {
                return order.OrderDetail.First().item_model + "...等" + detailsCount + "个";
            }
        }


        /// <summary>
        /// (有参构造)审批之前需要做的事
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="stepName">步骤名称</param>
        /// <param name="isPass">是否通过</param>
        /// <param name="userId">用户ID</param>
        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            if (isPass) {
                //审批ok之前再检查一次产品是否被禁用
                var otherSv = new OtherSv();
                foreach (var d in order.OrderDetail) {
                    if (otherSv.GetProducts(d.item_no).Count() < 1) {
                        throw new Exception("以下产品编码已被禁用或变更为历史资料：["+d.item_no+"],如有问题请联系工程部。");
                    }
                }
            }
        }        

        /// <summary>
        /// (有参构造) 完成申请之后需要做的事
        /// </summary>
        public override void DoWhenFinishAudit(bool isPass)
        {
            if (isPass) {
                MoveToFormalDir(order.sys_no); //成功结束的申请，将附件移动到正式目录
            }
            
        }

        /// <summary>
        /// 导出excel数据的模型
        /// </summary>
        private class ExcelData
        {
            public Order h { get; set; }
            public OrderDetail e { get; set; }
            public string auditStatus { get; set; }
        }

        /// <summary>
        /// 导出SO的excel通用方法
        /// </summary>
        /// <param name="myData">导出的数据</param>
        private void ExportExcel(List<ExcelData> myData)
        {
            //列宽：
            ushort[] colWidth = new ushort[] {16,16,16,14,18,16,28,28,16,20,
                                            16,14,18,24,32,14,14,14,14,14,
                                            14,16,14,14,14,14,14,18,18,60};

            //列名：
            string[] colName = new string[] { "审核结果","流水号","订单号","下单日期","办事处","订单类型","购货客户","国外客户","产品类别","产品用途",
                                            "币别","汇率","产品代码","产品名称","规格型号","单位","数量","单价","含税单价","成交价",
                                            "成本","价税合计","费用率%","MU%","税率%","佣金率%","佣金","发货地点","交货属性","摘要" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("销售订单_{0}.xls", DateTime.Now.ToString("yyyyMMdd"));
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

                cells.Add(++rowIndex, colIndex, d.auditStatus);
                cells.Add(rowIndex, ++colIndex, d.h.sys_no);
                cells.Add(rowIndex, ++colIndex, d.h.order_no);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.h.order_date).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.agency_name);
                cells.Add(rowIndex, ++colIndex, d.h.order_type_name);
                cells.Add(rowIndex, ++colIndex, d.h.customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.oversea_customer_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_type_name);
                cells.Add(rowIndex, ++colIndex, d.h.product_use);

                cells.Add(rowIndex, ++colIndex, d.h.currency_name);
                cells.Add(rowIndex, ++colIndex, d.h.exchange_rate);
                cells.Add(rowIndex, ++colIndex, d.e.item_no);
                cells.Add(rowIndex, ++colIndex, d.e.item_name);
                cells.Add(rowIndex, ++colIndex, d.e.item_model);
                cells.Add(rowIndex, ++colIndex, d.e.unit_name);
                cells.Add(rowIndex, ++colIndex, d.e.qty);
                cells.Add(rowIndex, ++colIndex, d.e.unit_price);
                cells.Add(rowIndex, ++colIndex, d.e.tax_price);
                cells.Add(rowIndex, ++colIndex, d.e.deal_price);

                cells.Add(rowIndex, ++colIndex, d.e.cost);
                cells.Add(rowIndex, ++colIndex, d.e.qty * d.e.tax_price);
                cells.Add(rowIndex, ++colIndex, d.e.fee_rate);
                cells.Add(rowIndex, ++colIndex, d.e.MU);
                cells.Add(rowIndex, ++colIndex, d.e.tax_rate);
                cells.Add(rowIndex, ++colIndex, d.e.commission_rate);
                cells.Add(rowIndex, ++colIndex, d.e.commission);
                cells.Add(rowIndex, ++colIndex, d.h.delivery_place_name);
                cells.Add(rowIndex, ++colIndex, d.h.receive_place_name);
                cells.Add(rowIndex, ++colIndex, d.h.summary);
            }

            //合计行
            cells.Add(++rowIndex, 1, "合计:");
            cells.Add(rowIndex, 17, myData.Sum(d => d.e.qty));
            cells.Add(rowIndex, 22, myData.Sum(d => d.e.qty * d.e.tax_price));

            xls.Send();
        }

        /// <summary>
        /// 营业员导出excel
        /// </summary>
        /// <param name="pm">查询参数模型</param>
        /// <param name="userId">用户id</param>
        public override void ExportSalerExcle(SalerSearchParamModel pm,int userId)
        {
            //首先从K3同步一下订单号
            db.updateAllK3OrderNo();

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

            var result = (from o in db.Order
                          from d in o.OrderDetail
                          join a in db.Apply on o.sys_no equals a.sys_no into X
                          from Y in X.DefaultIfEmpty()
                          where o.original_id == userId
                          && o.order_date >= fd
                          && o.order_date <= td
                          && (o.sys_no.Contains(pm.searchValue) || d.item_model.Contains(pm.searchValue))
                          && o.customer_name.Contains(pm.customerName)
                          && (pm.productType == null || o.product_type_name.Contains(pm.productType))
                          && (pm.saleRange == null || (pm.saleRange == "内销" && o.currency_no == "RMB") || (pm.saleRange == "外销" && o.currency_no != "RMB"))
                          && (pm.auditResult == 10 || (pm.auditResult == 0 && (Y == null || (Y != null && Y.success == null))) || (pm.auditResult == 1 && Y != null && Y.success == true) || pm.auditResult == -1 && Y != null && Y.success == false)
                          orderby o.order_date descending
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
            //首先从K3同步一下订单号
            db.updateAllK3OrderNo();

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
                          join o in db.Order on a.sys_no equals o.sys_no
                          from d in o.OrderDetail
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
        
        public override void DoWhenBeforeApply()
        {
            if (string.IsNullOrEmpty(order.contract_no)) {
                if (order.customer_no.StartsWith("04.1")) {
                    if (order.OrderDetail.Sum(d => d.deal_price) > 0) {
                        throw new Exception("客户为04.1开头的收费单，合同编号不能为空");
                    }
                }
            }
        }
    }
}