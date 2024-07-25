using LLB.Data;
using LLB.Models;
using IronPdf;
using System.Drawing;
//using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using LLB.Models.DataModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static System.Net.WebRequestMethods;
using System.Net.Mail;
using System.Net;
using PasswordGenerator;
using DNTCaptcha.Core;
using LLB.Models.ViewModel;
using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using IronPdf.Editing;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;

namespace LLB.Controllers
{
    [Authorize]
    [Route("Documents")]
    public class DocumentsController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly IWebHostEnvironment _env;

        public DocumentsController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService, IWebHostEnvironment env)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _env = env;
        }
    

    [Route("C")]
        public async Task<IActionResult> TestsAsync(string searchref)
        {

          // searchref = "fa965cee-0e29-40f8-8484-630aca6eb8b3";
           // IronPdf.HtmlToPdf Renderer = new IronPdf.HtmlToPdf();
            var Renderer = new IronPdf.HtmlToPdf();
            string webRootPath = _env.WebRootPath;
            string pdfFilePath = Path.Combine(webRootPath ,"Template.pdf");
           // string pdfFilePath = $"~/COICODENEW.pdf";




            var pdf = PdfDocument.FromFile(pdfFilePath);
            var applications = _db.ApplicationInfo.Where(a => a.Id == searchref).FirstOrDefault();
            var managers = _db.ManagersParticulars.Where(b => b.ApplicationId == searchref).ToList();
            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == searchref).FirstOrDefault();
            //var outletinfo = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            if (pdf == null)
            {
                throw new InvalidOperationException($"Failed to load the PDF document from {pdfFilePath}.");
            }

            TextStamper licensee = new TextStamper()
            {
                Text = $"{user.Name} { user.LastName}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(22),
                VerticalOffset = new Length(27),
            };

            TextStamper tradingname = new TextStamper()
            {
                Text = $"{outletinfo.TradingName}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(25.2),
                VerticalOffset = new Length(29.8),
            };




            TextStamper location = new TextStamper()
            {
                Text = $"{outletinfo.Address}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(28.4),
                VerticalOffset = new Length(32.5),
            };
            var managersfig = managers.Count();

            TextStamper managerscount = new TextStamper()
            {
                Text = $"{managersfig}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(53),
                VerticalOffset = new Length(36.2),
            };


           

            //TextStamper managerslist;
            string managerscontent = "<table style='font-family: Times New Roman; font-size: 14px;'>";
            foreach (var mans in managers)
            {
                managerscontent += $"<tr><td>{mans.Name} {mans.Surname}</td></tr>";
            }

            managerscontent += "</table>";

            // Render HTML content with text stamper
           // pdf = PdfDocument.R(managerscontent);

            HtmlStamper managerslist = new HtmlStamper()
                {
                  
                Html = managerscontent ,
                    
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    HorizontalOffset = new Length(23),
                    VerticalOffset = new Length(40),
            };

            var payload = "content to be edited";

            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.H);
                // QRCodeData qrCodeDatab = qrGenerator.CreateQrCode(qrText , QRCodeGenerator.ECCLevel.Q, QRCodeGenerator.EciMode.Utf8);//Url.Action("facebook.com")
                QRCode qrCodery = new QRCode(qrCodeData);

                //Bitmap qrCodeImage = qrCode.GetGraphic(50);
                // Image qrCodeImage = qrCode.GetGraphic(50);
                Bitmap qrCodeImage = qrCodery.GetGraphic(20, Color.Black, Color.Transparent,
                    (Bitmap)Bitmap.FromFile("C:\\My\\logo.png"), 80);

                //qrCodeImage.
                qrCodeImage.Save($"C:\\My\\QRCodes\\{searchref}.png", ImageFormat.Png);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //string rqcontent = $"<img  src='~/QRcodes/{applications.Id}.png'>";
            string rqcontent = $" <figure><img style='height:120px;width:120px; ' src='C:\\My\\QRCodes\\{searchref}.png'></figure>";
            HtmlStamper qrcode = new HtmlStamper()
            {

                Html = rqcontent,

                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(55),
                VerticalOffset = new Length(38),
            };


            //signature
            string sigcontent = $" <figure><img style='height:81px;width:81px; ' src='C:\\My\\llbsig.png'></figure>";
            HtmlStamper signature = new HtmlStamper()
            {

                Html = sigcontent,

                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(53),
                VerticalOffset = new Length(63),
            };


            Stamper[] stampersToApply = { licensee, tradingname, location,managerscount,managerslist, qrcode,signature };
            pdf.ApplyMultipleStamps(stampersToApply);
           // pdf.ApplyStamp(stamper2);

            string savePath = Path.Combine(webRootPath, "HtmlToPDFRAW.pdf");
            pdf.SaveAs(savePath);

            System.Net.WebClient client = new System.Net.WebClient();
            Byte[] byteArray = client.DownloadData(savePath);

            ViewBag.title = "New Search";
            // return new FileContentResult(byteArray, "application/pdf");
            return File(pdf.BinaryData, "application/pdf;");

        }

        [Route("D")]
        public IActionResult Testsb(string searchref)
        {
            var Renderer = new IronPdf.HtmlToPdf();
            List<string> HtmlList = new List<string>();
            string[] HtmlArray;

           string html =
                  @" <meta http-equiv='content-type' content='text/html; charset=utf-8' /><img src='logo.jpeg'><html style = 'p.dashed {border - style: dashed;}'><table style='font-size:16px'><tr><td >Entity No.&nbsp;&nbsp;&nbsp;&nbsp;</td>"
            + $"<td>3456</td></tr><tr> <td>Entity Name</td><td>Oliver PVT</td></tr>"
            + $"<tr><td>Date of Incorporation<span></span></td><td>{DateTime.Now}</td></tr>";
            HtmlList.Add(html);

            string htmlb =
                  @" <meta http-equiv='content-type' content='text/html; charset=utf-8' /><img src='logo.jpeg'><html style = 'p.dashed {border - style: dashed;}'><table style='font-size:16px'><tr><td >Entity No.&nbsp;&nbsp;&nbsp;&nbsp;</td>"
            + $"<td>3456</td></tr><tr> <td>Entity Name</td><td>Oliver PVT</td></tr>"
            + $"<tr><td>Date of Incorporation<span></span></td><td>{DateTime.Now}</td></tr>";
            HtmlList.Add(htmlb);

            HtmlArray = HtmlList.ToArray();

            string finalhtml = string.Concat(HtmlArray);
            string DocPath = @"C:/My/" + $"_Fiscalreport4.pdf";

            Renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            //Renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A3;
           // Renderer.PrintOptions.PaperOrientation = PdfPrintOptions.PdfPaperOrientation.Portrait;
            //Renderer.PrintOptions.PaperOrientation = PdfPrintOptions.PdfPaperOrientation.Landscape;
            // Renderer.PrintOptions.PaperOrientation = 0;

            Renderer.PrintOptions.EnableJavaScript = true;
            Renderer.PrintOptions.RenderDelay = 500; //milliseconds
            Renderer.PrintOptions.CssMediaType = IronPdf.PdfPrintOptions.PdfCssMediaType.Screen;

            Renderer.PrintOptions.MarginTop = 60;
            Renderer.PrintOptions.MarginBottom = 60;
            Renderer.PrintOptions.MarginLeft = 15;
            Renderer.PrintOptions.MarginRight = 10;


            //Renderer.RenderHtmlAsPdf(finalhtml).SaveAs(DocPath);
            var bg = Renderer.RenderHtmlAsPdf(finalhtml);
            //bg.AddBackgroundPdf(@"C:\\My\\bg.pdf");
            bg.SaveAs("/Users/p.pdf");


            System.Net.WebClient client = new System.Net.WebClient();
            Byte[] byteArray = client.DownloadData("/Users/p.pdf");

            ViewBag.title = "New Search";

            return new FileContentResult(byteArray, "application/pdf");
        }

        [Route("E")]
        public IActionResult Testsc(string searchref)
        {

            var renderer = new IronPdf.HtmlToPdf();
            var pdf = renderer.RenderHtmlAsPdf("<h1> Hello IronPdf </h1>");
            pdf.SaveAs("pixel-perfect.pdf");

            System.Net.WebClient client = new System.Net.WebClient();
            Byte[] byteArray = client.DownloadData("pixel-perfect.pdf");

            ViewBag.title = "New Search";

            return new FileContentResult(byteArray, "application/pdf");
        }

        }
}
