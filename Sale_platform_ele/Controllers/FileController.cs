using Sale_platform_ele.Models;
using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Sale_platform_ele.Controllers
{
    public class FileController : BaseController
    {
        private const string TAG = "文件模块";

        private void Wlog(string log, string sysNo = "", int unusual = 0)
        {
            base.Wlog(TAG, log, sysNo, unusual);
        }

        /// <summary>
        /// 文件上传视图
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public ActionResult UploadFileView(string sysNo)
        {
            ViewData["sysNo"] = sysNo;
            return View();
        }

        /// <summary>
        /// 文件保存方法
        /// </summary>
        /// <param name="postedFile"></param>
        /// <param name="saveName"></param>
        /// <returns></returns>
        [NonAction]
        private bool SaveFile(HttpPostedFileBase postedFile, string saveName)
        {
            bool result = false;
            string filepath = ConfigurationManager.AppSettings["AttachmentPath1"];
            if (!Directory.Exists(filepath)) {
                Directory.CreateDirectory(filepath);
            }
            try {
                postedFile.SaveAs(Path.Combine(filepath, saveName));
                result = true;
            }
            catch (Exception e) {
                throw new ApplicationException(e.Message);
            }
            return result;
        }

        /// <summary>
        /// 开始上传文件
        /// </summary>
        /// <param name="FileData">上传文件流</param>
        /// <param name="num">流水号</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult BeginUploadFile(HttpPostedFileBase FileData, string num)
        {
            string fileName = "";
            string finalName = "NOFILE";

            if (null != FileData && !String.IsNullOrEmpty(num)) {
                try {
                    fileName = Path.GetFileName(FileData.FileName);//获得文件名    
                    string ext = Path.GetExtension(fileName);//获取拓展名
                    finalName = num + ext;
                    SaveFile(FileData, finalName);
                }
                catch (Exception ex) {
                    finalName = ex.ToString();
                }
            }
            Wlog("开始上传文件,finalName:" + finalName, num);
            return Content(finalName);
        }

        /// <summary>
        /// 取得文件的基本信息，文件名、大小和上传时间
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <returns></returns>
        public JsonResult GetFileInfo(string sysNo)
        {
            var fileName = sysNo + ".rar";
            FileInfo info = new FileInfo(ConfigurationManager.AppSettings["AttachmentPath1"] + fileName);
            if (!info.Exists) {
                BillUtils ut = new BillUtils();
                BillSv sv = (BillSv)ut.GetBillSvInstance(ut.GetBillEnType(sysNo));
                info = new FileInfo(Path.Combine(sv.GetAttachmentPath(sysNo), fileName));
                if (!info.Exists)
                    return Json(new { success = false });
            }
            return Json(new
            {
                success = true,
                am = new
                {
                    file_name = fileName,
                    file_size = info.Length / 1024 + "K",
                    upload_time = info.CreationTime.ToString()
                }
            });
        }

        /// <summary>
        /// 文件下载视图
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <returns></returns>
        public ActionResult DownloadFileView(string sysNo)
        {
            ViewData["sysNo"] = sysNo;
            return View();
        }

        /// <summary>
        /// 开始下载文件
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <returns></returns>
        public FileStreamResult BeginDownloadFile(string sysNo)
        {
            string fileName = sysNo + ".rar";
            string absoluFilePath = ConfigurationManager.AppSettings["AttachmentPath1"] + fileName;
            FileInfo info = new FileInfo(absoluFilePath);
            if (!info.Exists) {
                BillUtils ut = new BillUtils();
                BillSv sv = (BillSv)ut.GetBillSvInstance(ut.GetBillEnType(sysNo));
                absoluFilePath = Path.Combine(sv.GetAttachmentPath(sysNo), fileName);
                info = new FileInfo(absoluFilePath);
                if (!info.Exists) {
                    return null;
                }
            }
            Wlog("开始下载文件", sysNo);
            return File(new FileStream(absoluFilePath, FileMode.Open), "application/octet-stream", Server.UrlEncode(fileName));
        }

        /// <summary>
        /// 营业单据列表页面，导出Excel
        /// </summary>
        /// <param name="fc">form表单</param>
        public void ExportSalerExcel(FormCollection fc)
        {
            SalerSearchParamModel pm = new SalerSearchParamModel();
            SomeUtils.SetFieldValueToModel(fc, pm);

            BillSv bill = (BillSv)new BillUtils().GetBillSvInstance(pm.billType);

            Wlog("营业员导出Excel："+JsonConvert.SerializeObject(pm));

            bill.ExportSalerExcle(pm, currentUser.userId);
        }

        public void ExportAuditorExcel(string billType, FormCollection fc)
        {
            AuditSearchParamModel pm = new AuditSearchParamModel();
            SomeUtils.SetFieldValueToModel(fc, pm);

            BillSv bill = (BillSv)new BillUtils().GetBillSvInstance(billType);

            Wlog("审核人导出Excel：" + JsonConvert.SerializeObject(pm), billType);

            bill.ExportAuditorExcle(pm, currentUser.userId);
        }

    }
}
