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
    public class EqmSv:BaseSv
    {
        public List<getK3EqmChDataResult> GetK3StockBill(string account, string billNo)
        {
            return db.getK3EqmChData(account, billNo).ToList();
        }

        public List<DeliveryInfoModel> GetDeliveryInfo(string customerNumber)
        {
            return db.Sale_eqm_ch_bill.Where(b => b.FCustomerNumber == customerNumber)
                .Select(b => new DeliveryInfoModel()
                {                    
                    deliveryUnit = b.FDeliveryUnit,
                    addr = b.FCustomerAddr,
                    attn = b.FCustomerContact,
                    phone = b.FCustomerPhone
                }).Distinct().ToList();           
        }

        public void SaveDeliveryBill(System.Web.Mvc.FormCollection fc, UserInfo user)
        {
            Sale_eqm_ch_bill bill = new Sale_eqm_ch_bill();
            SomeUtils.SetFieldValueToModel(fc, bill);
            List<Sale_eqm_ch_bill_detail> details = JsonConvert.DeserializeObject<List<Sale_eqm_ch_bill_detail>>(fc.Get("ch_bill_details"));
                        
            bill.FUserName = user.realName;
            bill.FSaveDate = DateTime.Now;
            bill.Sale_eqm_ch_bill_detail.AddRange(details);

            if (string.IsNullOrEmpty(bill.FSysNo)) {
                bill.FSysNo = GetNextSysNo();
            }
            else {
                var existedBill = db.Sale_eqm_ch_bill.Where(s => s.FSysNo == bill.FSysNo && (s.FDeleted == null || s.FDeleted == false)).FirstOrDefault();
                if (existedBill != null) {                    
                    existedBill.FDeleted = true;
                }
            }
            
            db.Sale_eqm_ch_bill.InsertOnSubmit(bill);
            db.SubmitChanges();
        }

        public Sale_eqm_ch_bill GetDeliveryBill(string sysNo)
        {
            return db.Sale_eqm_ch_bill.FirstOrDefault(b => b.FSysNo == sysNo && (b.FDeleted==false || b.FDeleted==null));
        }

        public List<EqmChListModel> SearchDeliveryBills(string account, string billNo, DateTime fromDate, DateTime toDate, string customer, string itemModel)
        {
            var result = from b in db.Sale_eqm_ch_bill
                         from e in b.Sale_eqm_ch_bill_detail
                         where b.FDate >= fromDate
                         && b.FDate < toDate
                         && b.FAccount == account
                         && (b.FDeleted == null || b.FDeleted == false)
                         orderby b.FDate descending
                         select new EqmChListModel()
                         {
                             FId = b.id,
                             FBillNo = b.FBillNo,
                             FCustomerName = b.FCustomerName,
                             FDate = DateTime.Parse(b.FDate.ToString()).ToString("yyyy-MM-dd"),
                             FDeliveryUnit = b.FDeliveryUnit,
                             FSysNo = b.FSysNo,
                             FUserName = b.FUserName,
                             FAmount = e.FAmount,
                             FDetailId = e.id,
                             FItemModel = e.FItemModel,
                             FItemName = e.FItemName,
                             FPrice = e.FPrice,
                             FQty = e.FQty,
                             FUnitName = e.FUnitName
                         };
            if (!string.IsNullOrWhiteSpace(billNo)) {
                result = result.Where(r => r.FBillNo.Contains(billNo));
            }
            if (!string.IsNullOrWhiteSpace(customer)) {
                result = result.Where(r => r.FCustomerName.Contains(customer));
            }
            if (!string.IsNullOrWhiteSpace(itemModel)) {
                result = result.Where(r => r.FItemModel.Contains(itemModel));
            }
            return result.Take(200).ToList();
        }

        public void ExportExcelData(int[] FIds)
        {
            var result = (from b in db.Sale_eqm_ch_bill
                          from e in b.Sale_eqm_ch_bill_detail
                          where FIds.Contains(b.id)
                          && (b.FDeleted == null || b.FDeleted == false)
                          orderby b.FDate descending
                          select new
                          {
                              h = b,
                              e = e
                          }).ToList();

            //列宽：
            ushort[] colWidth = new ushort[] {16,12,16,24,24,16,16,32,28,
                                            28,28,16,12,16,12,18,12,24,
                                            18,12,16};

            //列名：
            string[] colName = new string[] { "出库单号","出库日期","流水号","客户名称","收货客户","联系人","收货电话","收货地址","备注",
                                            "规格型号","产品名称","数量","单位","装箱数","件数","装箱规格尺寸（CM）","总毛重","行备注",
                                            "生产批号","生产日期","使用年限" };

            //設置excel文件名和sheet名
            XlsDocument xls = new XlsDocument();
            xls.FileName = string.Format("{1}送货单_{0:yyyyMMdd}.xls", DateTime.Now, result.FirstOrDefault().h.FAccount == "EQM" ? "仪器" : "工业");
            Worksheet sheet = xls.Workbook.Worksheets.Add("送货单详情");

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
            foreach (var d in result) {
                colIndex = 1;
                //"出库单号","出库日期","流水号","客户名称","收货客户","联系人","收货电话","收货地址","备注",
                //"规格型号","产品名称","数量","单位","装箱数","件数","装箱规格尺寸（CM）","总毛重","行备注",
                //生产批号","生产日期","使用年限"
                cells.Add(++rowIndex, colIndex, d.h.FBillNo);
                cells.Add(rowIndex, ++colIndex, ((DateTime)d.h.FDate).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.h.FSysNo);
                cells.Add(rowIndex, ++colIndex, d.h.FCustomerName);
                cells.Add(rowIndex, ++colIndex, d.h.FDeliveryUnit);
                cells.Add(rowIndex, ++colIndex, d.h.FCustomerContact);
                cells.Add(rowIndex, ++colIndex, d.h.FCustomerPhone);
                cells.Add(rowIndex, ++colIndex, d.h.FCustomerAddr);
                cells.Add(rowIndex, ++colIndex, d.h.FComment);

                cells.Add(rowIndex, ++colIndex, d.e.FItemModel);
                cells.Add(rowIndex, ++colIndex, d.e.FItemName);
                cells.Add(rowIndex, ++colIndex, d.e.FQty);
                cells.Add(rowIndex, ++colIndex, d.e.FUnitName);
                cells.Add(rowIndex, ++colIndex, d.e.FEveryNum);
                cells.Add(rowIndex, ++colIndex, d.e.FPackNum);
                cells.Add(rowIndex, ++colIndex, d.e.FBoxSize);
                cells.Add(rowIndex, ++colIndex, d.e.FTotalGrossWeight);
                cells.Add(rowIndex, ++colIndex, d.e.FEntryComment);

                cells.Add(rowIndex, ++colIndex, d.e.FBatch);
                cells.Add(rowIndex, ++colIndex, d.e.FProduceDate == null ? "" : ((DateTime)d.e.FProduceDate).ToString("yyyy-MM-dd"));
                cells.Add(rowIndex, ++colIndex, d.e.FUseYearSpan);                
            }

            xls.Send();
        }

        public void DeletedDeliveryBill(string sysNo)
        {
            var b = db.Sale_eqm_ch_bill.Single(c => c.FSysNo == sysNo && (c.FDeleted == null || c.FDeleted == false));
            b.FDeleted = true;
            db.SubmitChanges();
        }

        private string GetNextSysNo()
        {
            string billType = "EQ";
            string result = billType;
            string dateStr = DateTime.Now.ToString("yyMMdd");
            var maxRecord = db.SystemNo.Where(sn => sn.bill_type == billType && sn.date_string == dateStr).FirstOrDefault();
            if (maxRecord == null) {
                SystemNo sysNo = new SystemNo()
                {
                    bill_type = billType,
                    date_string = dateStr,
                    max_num = 1
                };
                db.SystemNo.InsertOnSubmit(sysNo);
                result += dateStr + "01";
            }
            else {
                maxRecord.max_num = maxRecord.max_num + 1;
                result += dateStr + string.Format("{0:00}", maxRecord.max_num);
            }
            db.SubmitChanges();
            return result;
        }

    }
}