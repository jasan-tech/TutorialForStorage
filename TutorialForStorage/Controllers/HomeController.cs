using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace TutorialForStorage.Controllers
{
    public class HomeController : Controller
    {
        private readonly string UPLOAD_PATH = @"~/Images/";
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> UploadImage(HttpPostedFileBase file)
        {
            if (file is null)
            {
                return RedirectToAction("Index", new { value = "uploadFailure" });
            }
            var folderPath = HostingEnvironment.MapPath(UPLOAD_PATH);
            var filePath = Path.Combine(folderPath, file.FileName);
            file.SaveAs(filePath);
            await new BlobsController().Upload(filePath);
            System.IO.File.Delete(filePath);
            return RedirectToAction("Index", new { value = "uploadSuccess" });
        }
    }
}
