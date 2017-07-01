using Sale_platform_ele.Services;
using Sale_platform_ele.Utils;
using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Sale_platform_ele.Controllers
{
    public class FileController : BaseController
    {
        
        public ActionResult UploadFileView(string sysNo)
        {
            ViewData["sysNo"] = sysNo;
            return View();
        }

        //保存附件
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
                    fileName = ex.ToString();
                }
            }
            return Content(finalName);
        }

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

        public ActionResult DownloadFileView(string sysNo)
        {
            ViewData["sysNo"] = sysNo;
            return View();
        }

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
            return File(new FileStream(absoluFilePath, FileMode.Open), "application/octet-stream", Server.UrlEncode(fileName));
        }

    }
}
