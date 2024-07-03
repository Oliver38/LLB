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
        public IActionResult Tests(string searchref)
        {
           // IronPdf.HtmlToPdf Renderer = new IronPdf.HtmlToPdf();
            var Renderer = new IronPdf.HtmlToPdf();
            string webRootPath = _env.WebRootPath;
            string pdfFilePath = Path.Combine(webRootPath ,"COICODENEW.pdf");
           // string pdfFilePath = $"~/COICODENEW.pdf";


            var pdf = PdfDocument.FromFile(pdfFilePath);

            if (pdf == null)
            {
                throw new InvalidOperationException($"Failed to load the PDF document from {pdfFilePath}.");
            }


            TextStamper stamper2 = new TextStamper()
            {
                Text = "Hello World! Stamp Two Here lalala!",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 30,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalOffset = new Length(10),
                VerticalOffset = new Length(10),
            };

            TextStamper stamper3 = new TextStamper()
            {
                Text = "Hello World! Stamp Two Here bom bom tilaw!",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 30,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalOffset = new Length(30),
                VerticalOffset = new Length(30),
            };


            Stamper[] stampersToApply = { stamper2, stamper3 };
            pdf.ApplyMultipleStamps(stampersToApply);
            pdf.ApplyStamp(stamper2);

            string savePath = Path.Combine(webRootPath, "HtmlToPDFRAW.pdf");
            pdf.SaveAs(savePath);

             WebClient client = new WebClient();
            Byte[] buffer = System.IO.File.ReadAllBytes(savePath);
            

           //// string filePath = savePath;
            ////return File(filePath, "application/pdf");

            // byte[] byteArray = System.IO.File.ReadAllBytes(savePath);

             return File(buffer, "application/pdf", "HtmlToPDFRAW.pdf");

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
