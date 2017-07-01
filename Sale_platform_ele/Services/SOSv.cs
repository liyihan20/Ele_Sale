using Newtonsoft.Json;
using Sale_platform_ele.Models;
using Sale_platform_ele.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sale_platform_ele.Services
{
    public class SOSv : BillSv
    {
        const string BILL_TYPE = "SO";
        const string BILL_TYPE_NAME = "销售订单";
        const string CREATE_VIEW_NAME = "CreateOrder";
        const string CHECK_VIEW_NAME = "CheckOrder";
        private Order order;

        public SOSv()
        {

        }

        public SOSv(string sysNo)
        {
            try {
                order = db.Order.Single(o => o.sys_no == sysNo);
            }
            catch {                
                throw new Exception("销售订单系统流水号不存在");
            }
        }

        public override string BillType
        {
            get
            {
                return BILL_TYPE;
            }
        }

        public override string CreateViewName
        {
            get { return CREATE_VIEW_NAME; }
        }

        public override string CheckViewName
        {
            get
            {
                return CHECK_VIEW_NAME;
            }
        }

        public override string BillTypeName
        {
            get { return BILL_TYPE_NAME; }
        }        

        public override object GetNewBill(int userId)
        {
            UA ua = new UA(userId);
            Order order = new Order();
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

        public override object GetBill(int stepVersion)
        {            
            order.step_version = stepVersion;

            if (string.IsNullOrEmpty(order.order_no)) {
                if (db.Apply.Where(a => a.sys_no == order.sys_no && a.success == true).Count() > 0) {
                    //如果订单号为空，并且已申请结束OK的，去K3把订单号带过来。
                }
            }

            return order;
        }


        public override bool HasOrderSaved(string sysNo)
        {
            return db.Order.Where(o => o.sys_no == sysNo).Count() > 0;
        }

        public override string SaveBill(System.Web.Mvc.FormCollection fc, int userId)
        {
            Order order = new Order();
            SomeUtils.SetFieldValueToModel(fc, order);
            order.OrderDetail.AddRange(JsonConvert.DeserializeObject<List<OrderDetail>>(fc.Get("Sale_order_details")));

            string validateResult = ValidateSO(order);
            if (!string.IsNullOrEmpty(validateResult)) {
                return validateResult;
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
                    d.MU = csv.GetMU((decimal)d.unit_price, (decimal)d.cost, (int)d.fee_rate, (decimal)order.exchange_rate);
                    d.commission_rate = csv.GetCommissionRate((decimal)d.MU, order.product_type_no);
                    d.commission = csv.GetCommissionMoney((decimal)d.unit_price, (decimal)d.qty, (decimal)d.commission_rate);
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

        private string ValidateSO(Order order)
        {
            if (order.OrderDetail.Count() == 0) {
                return "订单明细至少应录入一行，保存失败！";
            }

            bool isForeignOrder = !order.currency_no.Equals("RMB");
            if (order.order_type_name.Equals("生产单") && isForeignOrder) {
                if (!order.customer_no.Equals("00.01")) {
                    return "订单类型为生产单的国外订单，购货客户必须为香港信利电子有限公司（00.01）";
                }
                if (string.IsNullOrEmpty(order.oversea_customer_no)) {
                    return "订单类型为生产单的国外订单，国外客户不能为空";
                }
            }
            //if (order.step_version > 0 && !isForeignOrder) {
            //    if (string.IsNullOrEmpty(order.contract_no)) {
            //        return "国内单的合同编号不能为空";
            //    }
            //}

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
            }

            return "";
        }


        public override List<object> GetBillList(SalerSearchParamModel pm, int userId)
        {       
            pm.searchValue = pm.searchValue ?? "";

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

        public override string GetProcessNo()
        {
            return BILL_TYPE;
        }

        public override object GetNewBillFromOld()
        {
            order.sys_no = GetNextSysNo(BILL_TYPE);
            order.order_date = DateTime.Now;
            order.step_version = 0;
            order.order_no = "";

            return order;
        }



        public override string GetSpecificBillTypeName()
        {
            if (!string.IsNullOrEmpty(order.order_type_name)) {
                return order.order_type_name;
            }
            else {
                return BILL_TYPE_NAME;
            }
        }

        public override Dictionary<string, int?> GetProcessDic()
        {
            Dictionary<string, int?> dic = new Dictionary<string, int?>();
            dic.Add("部门NO", order.User.department_no);

            return dic;
        }

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
        /// 审批之前需要做的事
        /// </summary>
        /// <param name="step">步骤</param>
        /// <param name="stepName">步骤名称</param>
        /// <param name="isPass">是否通过</param>
        /// <param name="userId">用户ID</param>
        public override void DoWhenBeforeAudit(int step, string stepName, bool isPass, int userId)
        {
            if (isPass && stepName.Contains("下单组")) {
                if (string.IsNullOrEmpty(order.contract_no) && order.currency_no.Equals("RMB")) {
                    throw new Exception("国内单的合同编号必须由下单组填写");
                }
            }
        }        

        /// <summary>
        /// (有参构造) 完成申请之后需要做的事
        /// </summary>
        public override void DoWhenFinishAudit()
        {
            MoveToFormalDir(order.sys_no);
        }
    }
}