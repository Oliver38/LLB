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
using System.Globalization;
using IronPdf.Editing;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using QRCoder;
using System.Drawing.Imaging;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Text;
using LLB.Helpers;

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


        [Route("LLBLicense")]
        public async Task<IActionResult> TestsAsync(string searchref)
        {

            // searchref = "fa965cee-0e29-40f8-8484-630aca6eb8b3";
            // IronPdf.HtmlToPdf Renderer = new IronPdf.HtmlToPdf();
            var Renderer = new IronPdf.HtmlToPdf();
            string webRootPath = _env.WebRootPath;
            string pdfFilePath = Path.Combine(webRootPath, "Template.pdf");
            // string pdfFilePath = $"~/COICODENEW.pdf";




            var applications = _db.ApplicationInfo.Where(a => a.Id == searchref).FirstOrDefault();
            if (applications == null)
            {
                return NotFound("The licence could not be found.");
            }

            var managers = _db.ManagersParticulars
                .Where(b => b.ApplicationId == searchref
                    && (b.Status == "active"
                        || b.Status == "pending-resigned"
                        || b.Status == "pending-deceased"))
                .ToList();
            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == searchref).FirstOrDefault();
            var licenses = _db.LicenseTypes.Where(b => b.Id == applications.LicenseTypeID).FirstOrDefault();
            if (AgentLicenseHelper.IsAgentLicenseApplication(applications))
            {
                return GenerateAgentLicensePdf(applications, outletinfo, licenses);
            }

            var pdf = PdfDocument.FromFile(pdfFilePath);
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
                Text = $"{applications.BusinessName.ToUpper()} ",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(22),
                VerticalOffset = new Length(27),
            };



            TextStamper council = new TextStamper()
            {
                Text = $"{outletinfo.Council}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(78.5),
                VerticalOffset = new Length(19.1),
            };


            TextStamper licenseName = new TextStamper()
            {
                Text = $"{licenses.LicenseName.ToUpper()}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalOffset = new Length(1),
                VerticalOffset = new Length(20),
            };

            TextStamper tradingname = new TextStamper()
            {
                Text = $"{outletinfo.TradingName.ToUpper()}",
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
                Text = $"{outletinfo.Address.ToUpper()}",
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
                managerscontent += $"<tr><td>{mans.Name.ToUpper()} {mans.Surname.ToUpper()}</td></tr>";
            }

            managerscontent += "</table>";

            // Render HTML content with text stamper


            TextStamper expirydate = new TextStamper()
            {
                Text = $"{applications.ExpiryDate.Date}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(34.4),
                VerticalOffset = new Length(24.5),
            };



            TextStamper grantdate = new TextStamper()
            {
                Text = $"{applications.ApprovedDate.Date}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(34.4),
                VerticalOffset = new Length(26.1),
            };


            TextStamper expirydateuthority = new TextStamper()
            {
                Text = $"{applications.ExpiryDate.Date}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(34.4),
                VerticalOffset = new Length(27.8),
            };

            TextStamper llbnum = new TextStamper()
            {
                Text = $"{applications.LLBNum}",
                FontFamily = "Times New Roman",
                UseGoogleFont = false,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(34.4),
                VerticalOffset = new Length(29.5),
            };
            // pdf = PdfDocument.R(managerscontent);

            HtmlStamper managerslist = new HtmlStamper()
            {

                Html = managerscontent,

                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(23),
                VerticalOffset = new Length(40),
            };

            var isHotelLicense = licenses?.LicenseName?.IndexOf("hotel", StringComparison.OrdinalIgnoreCase) >= 0;
            var hotelRoomContent = $@"
                <div style='font-family: Times New Roman; font-size: 14px;'>
                    Number of bedrooms to be maintained:
                    <strong>Double:</strong> {FormatRoomCount(outletinfo?.HotelDoubleRooms)}
                    <strong>Single:</strong> {FormatRoomCount(outletinfo?.HotelSingleRooms)}
                </div>";
            HtmlStamper hotelRoomInfo = new HtmlStamper()
            {
                Html = hotelRoomContent,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(13),
                VerticalOffset = new Length(38),
            };

            string renconditions = "<ul style='font-family: Times New Roman; font-size: 13px;'> " +
                "<li>RENEWAL PERIOD IS FROM NOV. TO JAN. YEARLY.</li> " +
                "<li>ATTACH COPY OF CURRENT LiCENCE</li> " +
                "<li>ATTACH HEALTH REPORT ON PREMISES OR INSPECTION REPORT</li> " +
                "<li>ATTACH ZBC CLEARANCE CERTIFICATE</li></ul>";

            HtmlStamper conditions = new HtmlStamper()
            {

                Html = renconditions,

                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(13),
                VerticalOffset = new Length(47.5),
            };
            var payload = VerificationLinkHelper.BuildLiveUrl($"Home/CheckLicense?LLBNUMBER={Uri.EscapeDataString(applications.LLBNum ?? string.Empty)}");

            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                // QRCodeData qrCodeDatab = qrGenerator.CreateQrCode(qrText , QRCodeGenerator.ECCLevel.Q, QRCodeGenerator.EciMode.Utf8);//Url.Action("facebook.com")
                QRCode qrCodery = new QRCode(qrCodeData);

                //Bitmap qrCodeImage = qrCode.GetGraphic(50);
                // Image qrCodeImage = qrCode.GetGraphic(50);
                Bitmap qrCodeImage = qrCodery.GetGraphic(30, Color.Black, Color.Transparent,
                    (Bitmap)Bitmap.FromFile("C:\\My\\logo.png"), 80);

                //qrCodeImage.
                qrCodeImage.Save($"C:\\My\\QRCodes\\{searchref}.png", ImageFormat.Png);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //string rqcontent = $"<img  src='~/QRcodes/{applications.Id}.png'>";
            string rqcontent = $" <figure><img style='height:140px;width:140px; ' src='C:\\My\\QRCodes\\{searchref}.png'></figure>";
            HtmlStamper qrcode = new HtmlStamper()
            {

                Html = rqcontent,

                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(55),
                VerticalOffset = new Length(37),
            };


            //signature
            string sigcontent = $" <figure><img style='height:81px;width:81px; ' src='C:\\My\\llbsig.png'></figure>";
            HtmlStamper signature = new HtmlStamper()
            {

                Html = sigcontent,

                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalOffset = new Length(12),
                VerticalOffset = new Length(42.5),
            };


            var stampersToApply = new List<Stamper> { licensee, tradingname, location, managerscount, managerslist, qrcode, signature, expirydate, grantdate, expirydateuthority, llbnum, conditions, council, licenseName };
            if (isHotelLicense)
            {
                stampersToApply.Add(hotelRoomInfo);
            }

            pdf.ApplyMultipleStamps(stampersToApply.ToArray());
            // pdf.ApplyStamp(stamper2);

            string savePath = Path.Combine(webRootPath, "HtmlToPDFRAW.pdf");
            pdf.SaveAs(savePath);

            System.Net.WebClient client = new System.Net.WebClient();
            Byte[] byteArray = client.DownloadData(savePath);

            ViewBag.title = "New Search";
            // return new FileContentResult(byteArray, "application/pdf");
            return File(pdf.BinaryData, "application/pdf;");

        }

        private FileContentResult GenerateAgentLicensePdf(ApplicationInfo application, OutletInfo? outlet, LicenseTypes? license)
        {
            var sourceApplication = string.IsNullOrWhiteSpace(application.CompanyNumber)
                ? null
                : _db.ApplicationInfo.FirstOrDefault(record => record.Id == application.CompanyNumber);
            var sourceOutlet = sourceApplication == null
                ? null
                : _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == sourceApplication.Id);

            var agentName = BuildAgentLicenseeName(application);
            var businessAddress = FirstNonEmpty(outlet?.Address, application.OperationAddress, "N/A");
            var expiryDate = FormatAgentLicenseDate(application.ExpiryDate);
            var holderName = BuildWholesaleHolderName(sourceApplication, sourceOutlet);
            var holderLlbNumber = FirstNonEmpty(sourceApplication?.LLBNum, "N/A");
            var fileName = SanitizeFileName(FirstNonEmpty(application.LLBNum, agentName, "agent-liquor-licence"));
            var verificationReference = FirstNonEmpty(application.LLBNum, application.Id, agentName);
            var verificationUrl = VerificationLinkHelper.BuildLiveUrl($"Documents/AgentLicenseVerification?searchref={Uri.EscapeDataString(verificationReference)}");
            var qrCodeDataUri = GenerateQrCodeDataUri(verificationUrl);
            var coatOfArmsDataUri = GetImageDataUri(Path.Combine(_env.WebRootPath, "front", "img", "IMG", "Coat_of_arms_of_ZimbabweB.png"));
            var coatOfArmsMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<img src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' />";
            var qrCodeMarkup = string.IsNullOrWhiteSpace(qrCodeDataUri)
                ? string.Empty
                : $@"
      <div class='qr-card'>
        <div class='qr-title'>Scan To Verify</div>
        <div class='qr-wrap'>
          <img class='qr-code' src='{qrCodeDataUri}' alt='Licence verification QR code' />
          <div class='qr-crest'>{coatOfArmsMarkup}</div>
        </div>
      </div>";

            var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <style>
    @page {{ size: A4 portrait; margin: 0; }}
    html, body {{
      margin: 0;
      padding: 0;
      background: #f4f0dc;
      color: #1b1b1b;
      font-family: ""Times New Roman"", Times, serif;
    }}

    .page {{
      position: relative;
      width: 210mm;
      height: 297mm;
      overflow: hidden;
      background:
        radial-gradient(circle at 16% 12%, rgba(244, 196, 48, 0.34), transparent 34mm),
        radial-gradient(circle at 92% 10%, rgba(0, 122, 61, 0.22), transparent 44mm),
        radial-gradient(circle at 88% 88%, rgba(190, 32, 46, 0.18), transparent 48mm),
        linear-gradient(135deg, #fffdf2 0%, #ffffff 43%, #f5fff8 100%);
      box-sizing: border-box;
    }}

    .page::before {{
      content: """";
      position: absolute;
      inset: 10mm;
      border: 2px solid #0f5f36;
      box-shadow: inset 0 0 0 1.5mm #f4c430, inset 0 0 0 2.2mm #b61f2c;
    }}

    .page::after {{
      content: """";
      position: absolute;
      inset: 17mm;
      border: 1px solid rgba(15, 95, 54, 0.35);
      pointer-events: none;
    }}

    .watermark {{
      position: absolute;
      top: 102mm;
      left: 63mm;
      width: 84mm;
      opacity: 0.055;
    }}

    .watermark img {{
      width: 100%;
    }}

    .crest-top {{
      position: absolute;
      top: 22mm;
      left: 0;
      width: 100%;
      text-align: center;
    }}

    .crest-top img {{
      height: 31mm;
    }}

    .title {{
      position: absolute;
      top: 58mm;
      left: 0;
      width: 100%;
      text-align: center;
      font-size: 21px;
      font-weight: 700;
      letter-spacing: 0;
      color: #0f3f28;
    }}

    .subtitle {{
      position: absolute;
      top: 69mm;
      left: 0;
      width: 100%;
      text-align: center;
      font-size: 10.5px;
      color: #6c4f00;
      text-transform: uppercase;
    }}

    .content {{
      position: absolute;
      top: 83mm;
      left: 22mm;
      width: 118mm;
      font-size: 11px;
      line-height: 1.5;
      z-index: 1;
    }}

    .line {{
      margin: 3mm 0 0;
    }}

    .label {{
      display: inline-block;
      min-width: 42mm;
      color: #313131;
    }}

    .value {{
      font-weight: 700;
      font-size: 12.5px;
      color: #000;
    }}

    .condition {{
      margin-top: 8mm;
      color: #6c4f00;
      font-weight: 700;
    }}

    .holder {{
      margin-top: 3mm;
      font-weight: 700;
      font-size: 13.5px;
      line-height: 1.35;
      color: #0f3f28;
    }}

    .renewal {{
      position: absolute;
      top: 181mm;
      left: 22mm;
      font-size: 10.5px;
      white-space: nowrap;
      z-index: 1;
    }}

    .renewal .value {{
      margin-left: 10mm;
      font-size: 13px;
      letter-spacing: 0;
      color: #b61f2c;
    }}

    .delete-note {{
      position: absolute;
      top: 193mm;
      left: 22mm;
      font-size: 9.5px;
      z-index: 1;
    }}

    .side-panel {{
      position: absolute;
      top: 83mm;
      right: 21mm;
      width: 43mm;
      z-index: 1;
    }}

    .qr-card {{
      border: 1px solid rgba(15, 95, 54, 0.6);
      background: rgba(255, 255, 255, 0.88);
      padding: 5mm 4mm 4mm;
      text-align: center;
    }}

    .qr-title {{
      font-family: Arial, Helvetica, sans-serif;
      font-size: 8.5px;
      font-weight: 700;
      color: #0f3f28;
      text-transform: uppercase;
      margin-bottom: 2.5mm;
    }}

    .qr-wrap {{
      position: relative;
      width: 31mm;
      height: 31mm;
      margin: 0 auto;
    }}

    .qr-code {{
      width: 31mm;
      height: 31mm;
      display: block;
    }}

    .qr-crest {{
      position: absolute;
      top: 50%;
      left: 50%;
      width: 8.5mm;
      height: 8.5mm;
      transform: translate(-50%, -50%);
      background: #fff;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1mm;
    }}

    .qr-crest img {{
      max-width: 7mm;
      max-height: 7mm;
    }}

    .signature-block {{
      position: absolute;
      right: 21mm;
      top: 165mm;
      width: 48mm;
      text-align: center;
      font-size: 10px;
      z-index: 1;
    }}

    .signature-mark {{
      height: 22mm;
      font-family: ""Brush Script MT"", ""Segoe Script"", cursive;
      font-size: 27px;
      font-style: italic;
      transform: rotate(-6deg);
      transform-origin: center;
      color: #1b1b1b;
    }}

    .signature-line {{
      border-bottom: 1px dotted #000;
      height: 1px;
      margin-bottom: 1.5mm;
    }}

    .secretary {{
      font-weight: 700;
    }}
  </style>
</head>
<body>
  <div class='page'>
    <div class='watermark'>{coatOfArmsMarkup}</div>
    <div class='crest-top'>{coatOfArmsMarkup}</div>
    <div class='title'>AGENT LIQUOR LICENCE</div>
    <div class='subtitle'>Liquor Licensing Board</div>

    <div class='content'>
      <div>Subject to the provisions of the act and the conditions specified herein</div>
      <div class='line'><span class='label'>Licensee:</span><span class='value'>{EncodeHtml(agentName)}</span></div>
      <div class='line'><span class='label'>Business Address:</span><span class='value'>{EncodeHtml(businessAddress.ToUpperInvariant())}</span></div>
      <div class='line'><span class='label'>Date of expiry of licence:</span><span class='value'>{EncodeHtml(expiryDate)}</span></div>

      <div class='condition'>Conditions of licence imposed by the Board</div>
      <div>Licence is employed by the following holder of an agent's liquor licence</div>
      <div class='holder'>{EncodeHtml(holderName.ToUpperInvariant())}</div>
      <div class='holder'>{EncodeHtml(holderLlbNumber)}</div>
    </div>

    <div class='side-panel'>
      {qrCodeMarkup}
    </div>

    <div class='renewal'>
      <span>Date of issue/renewal* of licence</span>
      <span class='value'>NOVEMBER TO JANUARY YEARLY</span>
    </div>

    <div class='delete-note'>*Delete the inapplicable</div>

    <div class='signature-block'>
      <div class='signature-mark'>Secretary</div>
      <div class='signature-line'></div>
      <div class='secretary'>Secretary Liquor Licensing Board</div>
    </div>
  </div>
</body>
</html>";

            var renderer = new HtmlToPdf();
            renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            renderer.PrintOptions.MarginTop = 0;
            renderer.PrintOptions.MarginBottom = 0;
            renderer.PrintOptions.MarginLeft = 0;
            renderer.PrintOptions.MarginRight = 0;
            var pdf = renderer.RenderHtmlAsPdf(html);

            return File(pdf.BinaryData, "application/pdf", $"{fileName}.pdf");
        }

        [AllowAnonymous]
        [HttpGet("AgentLicenseVerification")]
        public IActionResult AgentLicenseVerification(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                return Content(BuildAgentLicenseVerificationHtml(
                    false,
                    "No licence reference was supplied.",
                    null,
                    null,
                    null,
                    null,
                    null), "text/html");
            }

            var normalizedReference = searchref.Trim();
            var application = _db.ApplicationInfo.FirstOrDefault(record =>
                record.Id == normalizedReference || record.LLBNum == normalizedReference);

            if (application == null || !AgentLicenseHelper.IsAgentLicenseApplication(application))
            {
                return Content(BuildAgentLicenseVerificationHtml(
                    false,
                    "The agent liquor licence could not be verified from the supplied reference.",
                    null,
                    null,
                    null,
                    null,
                    normalizedReference), "text/html");
            }

            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == application.Id);
            var sourceApplication = string.IsNullOrWhiteSpace(application.CompanyNumber)
                ? null
                : _db.ApplicationInfo.FirstOrDefault(record => record.Id == application.CompanyNumber);
            var sourceOutlet = sourceApplication == null
                ? null
                : _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == sourceApplication.Id);

            var isApproved = string.Equals(application.Status, "approved", StringComparison.OrdinalIgnoreCase);
            var isExpired = application.ExpiryDate != default && application.ExpiryDate.Date < DateTime.Today;
            var isValid = isApproved && !isExpired;
            var message = isValid
                ? "This agent liquor licence is valid and matches an approved record in the system."
                : isExpired
                    ? "This agent liquor licence was found, but it has expired."
                    : $"This agent liquor licence was found, but its current status is '{application.Status ?? "Unknown"}'.";

            return Content(BuildAgentLicenseVerificationHtml(
                isValid,
                message,
                application,
                outlet,
                sourceApplication,
                sourceOutlet,
                normalizedReference), "text/html");
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

        [HttpGet("ExtendedHoursLicense")]
        public IActionResult ExtendedHoursLicense(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                TempData["error"] = "The extended hours certificate could not be found.";
                return RedirectToAction("ExtendedHoursListings", "Home");
            }

            var extendedHours = FindExtendedHoursCertificate(searchref);
            if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
            {
                TempData["error"] = "The extended hours certificate could not be found.";
                return RedirectToAction("ExtendedHoursListings", "Home");
            }

            if (!string.Equals(extendedHours.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "The extended hours certificate is only available after the application has been approved.";
                return RedirectToAction("ExtendedHours", "Postprocess", new { id = extendedHours.ApplicationId, process = "EXH" });
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == extendedHours.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == extendedHours.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                TempData["error"] = "The extended hours certificate could not be generated because application details are incomplete.";
                return RedirectToAction("ExtendedHoursListings", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            if (User.IsInRole("client")
                && !string.Equals(application.UserID, currentUserId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extendedHours.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var approvalDate = extendedHours.DateOfApproval ?? extendedHours.DateUpdated;
            var issuedDate = approvalDate == default ? DateTime.Now : approvalDate;
            var extendedHoursDate = extendedHours.ExtendedHoursDate == default
                ? issuedDate
                : extendedHours.ExtendedHoursDate;

            var boardHeading = EncodeHtml("LIQUOR LICENSING BOARD");
            var certificateTitle = EncodeHtml("Extended Hours Licence Certificate");
            var rawTradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            var rawBusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            var rawLlbNumber = application.LLBNum ?? "N/A";
            var rawReference = extendedHours.Reference ?? "N/A";
            var rawLicenseName = license?.LicenseName ?? "N/A";
            var rawRegionName = region?.RegionName ?? "N/A";
            var rawAddress = outlet.Address ?? application.OperationAddress ?? "N/A";
            var rawCouncil = outlet.Council ?? "N/A";
            var rawReason = extendedHours.ReasonForExtention ?? "Not provided";
            var tradingName = EncodeHtml(rawTradingName);
            var businessName = EncodeHtml(rawBusinessName);
            var llbNumber = EncodeHtml(rawLlbNumber);
            var reference = EncodeHtml(rawReference);
            var licenseName = EncodeHtml(rawLicenseName);
            var regionName = EncodeHtml(rawRegionName);
            var address = EncodeHtml(rawAddress);
            var council = EncodeHtml(rawCouncil);
            var reason = EncodeHtml(rawReason);
            var certificateStatement = EncodeHtml(
                $"{rawTradingName} has been approved to operate under extended hours on {extendedHoursDate:dd MMMM yyyy}.");
            var coatOfArmsDataUri = GetImageDataUri(Path.Combine(_env.WebRootPath, "front", "img", "IMG", "Coat_of_arms_of_ZimbabweB.png"));
            var coatOfArmsMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<div class='crest'><img src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' /></div>";
            var verificationReference = extendedHours.Reference ?? extendedHours.Id;
            var verificationUrl = VerificationLinkHelper.BuildLiveUrl($"Documents/ExtendedHoursLicenseVerification?searchref={Uri.EscapeDataString(verificationReference)}");
            var qrCodeDataUri = GenerateQrCodeDataUri(verificationUrl);
            var qrLogoMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<img class='qr-logo' src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' />";
            var qrCodeMarkup = string.IsNullOrWhiteSpace(qrCodeDataUri)
                ? string.Empty
                : $@"<div class='verification'>
      <div class='verification-label'>Scan To Verify</div>
      <div class='verification-qr'>
        <img class='qr-code' src='{qrCodeDataUri}' alt='Certificate verification QR code' />
        {qrLogoMarkup}
      </div>
      <div class='verification-caption'>Extended Hours Certificate</div>
    </div>";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <style>
    @@page {{
      size: A4 portrait;
      margin: 0;
    }}
    html,
    body {{
      font-family: 'Times New Roman', serif;
      color: #10243d;
      margin: 0;
      width: 210mm;
      height: 297mm;
    }}
    .certificate {{
      width: 210mm;
      min-height: 297mm;
      box-sizing: border-box;
      border: 8px solid #b48a3c;
      padding: 10mm 10mm 8mm 10mm;
      position: relative;
      background: #fffdfa;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }}
    .certificate::before {{
      content: '';
      position: absolute;
      inset: 3mm;
      border: 1px solid #d4bf8d;
      pointer-events: none;
    }}
    .header {{
      text-align: center;
      margin-bottom: 8px;
    }}
    .board {{
      font-size: 13.5px;
      letter-spacing: 2px;
      text-transform: uppercase;
      color: #7a5a1c;
      margin-bottom: 4px;
      font-weight: bold;
      line-height: 1.25;
    }}
    .crest {{
      display: flex;
      justify-content: center;
      margin: 2px 0 6px 0;
    }}
    .crest img {{
      width: 22mm;
      height: auto;
    }}
    h1 {{
      font-size: 29px;
      margin: 0 0 3px 0;
      padding-top: 10px;
      padding-bottom: 10px;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      line-height: 1.2;
    }}
    .subtitle {{
      font-size: 11px;
      margin: 0;
      color: #4d5c6d;
      line-height: 1.35;
    }}
    .statement {{
      margin: 8px 0 8px 0;
      padding-bottom: 20px;
      font-size: 13px;
      line-height: 1.5;
      text-align: center;
    }}
    .emphasis {{
      font-weight: bold;
      color: #0b4a76;
    }}
    .details-wrap {{
      flex: 1 1 auto;
      display: flex;
      margin-top: 4px;
      min-height: 0;
    }}
    .details {{
      width: 100%;
      height: 100%;
      border-collapse: collapse;
      table-layout: fixed;
      font-size: 11.6px;
      flex: 1 1 auto;
    }}
    .details tbody {{
      height: 100%;
    }}
    .details tbody tr {{
      height: 10%;
    }}
    .details th,
    .details td {{
      border: 1px solid #d7dfe8;
      padding: 7px 7px;
      vertical-align: middle;
      font-size: 13.2px;
      line-height: 1.4;
    }}
    .details th {{
      width: 28%;
      text-align: left;
      background: #f4efe2;
      color: #5f4d24;
      text-transform: uppercase;
      font-size: 13.2px;
      letter-spacing: 0.45px;
    }}
    .declaration {{
      margin-top: 8px;
      padding: 8px 10px;
      background: #f8fafc;
      border-left: 3px solid #0b4a76;
      font-size: 11.2px;
      line-height: 1.45;
    }}
    .body-content {{
      flex: 1 1 auto;
      display: flex;
      flex-direction: column;
    }}
    .bottom-row {{
      margin-top: 10px;
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      align-items: end;
      gap: 10px;
    }}
    .signature {{
      min-width: 130px;
      padding-top: 16px;
      border-top: 1px solid #10243d;
      text-align: center;
      font-size: 10.6px;
      line-height: 1.35;
      justify-self: end;
    }}
    .issued {{
      font-size: 10.6px;
      line-height: 1.45;
      align-self: end;
    }}
    .verification {{
      text-align: center;
      align-self: end;
    }}
    .verification-qr {{
      position: relative;
      width: 34mm;
      height: 34mm;
      margin: 0 auto 3px auto;
    }}
    .verification-qr .qr-code {{
      width: 100%;
      height: 100%;
      display: block;
    }}
    .verification-qr .qr-logo {{
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
      width: 9.5mm;
      height: 9.5mm;
      object-fit: contain;
      background: #fffdfa;
      border-radius: 50%;
      padding: 1mm;
      box-sizing: border-box;
    }}
    .verification-label {{
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.6px;
      font-weight: bold;
      color: #7a5a1c;
      margin-bottom: 2px;
    }}
    .verification-caption {{
      font-size: 9px;
      color: #4d5c6d;
      line-height: 1.3;
    }}
  </style>
</head>
<body>
  <div class='certificate'>
    <div class='body-content'>
      <div class='header'>
        <div class='board'>{boardHeading}</div>
        {coatOfArmsMarkup}
        <h1>{certificateTitle}</h1>
        <p class='subtitle'>This certificate confirms board approval for extended trading hours on the stated date.</p>
      </div>

      <div class='statement'>
        This is to certify that <span class='emphasis'>{tradingName}</span>, operating under
        LLB Licence Number <span class='emphasis'>{llbNumber}</span>, has been authorised by the
        Liquor Licensing Board to trade on extended hours for <span class='emphasis'>{extendedHoursDate:dd MMMM yyyy}</span>.
      </div>

      <div class='details-wrap'>
        <table class='details'>
          <tbody>
            <tr>
              <th>Trading Name</th>
              <td>{tradingName}</td>
            </tr>
            <tr>
              <th>Business Name</th>
              <td>{businessName}</td>
            </tr>
            <tr>
              <th>Licence Type</th>
              <td>{licenseName}</td>
            </tr>
            <tr>
              <th>Region</th>
              <td>{regionName}</td>
            </tr>
            <tr>
              <th>Address</th>
              <td>{address}</td>
            </tr>
            <tr>
              <th>Council</th>
              <td>{council}</td>
            </tr>
            <tr>
              <th>Extended Hours Date</th>
              <td>{extendedHoursDate:dddd, dd MMMM yyyy}</td>
            </tr>
            <tr>
              <th>Application Reference</th>
              <td>{reference}</td>
            </tr>
            <tr>
              <th>Approved On</th>
              <td>{issuedDate:dd MMMM yyyy}</td>
            </tr>
            <tr>
              <th>Purpose / Justification</th>
              <td>{reason}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class='declaration'>
        {certificateStatement} This approval is valid only for the date stated on this certificate and must be presented together with the main liquor licence whenever required by an inspecting authority.
      </div>
    </div>

    <div class='bottom-row'>
      <div class='issued'>
        <div><strong>Issued Date:</strong> {issuedDate:dd MMMM yyyy}</div>
        <div><strong>Certificate Ref:</strong> {reference}</div>
      </div>
      {qrCodeMarkup}
      <div class='signature'>
        For: Liquor Licensing Board
      </div>
    </div>
  </div>
</body>
</html>";

            var renderer = new HtmlToPdf();
            renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            renderer.PrintOptions.MarginTop = 0;
            renderer.PrintOptions.MarginBottom = 0;
            renderer.PrintOptions.MarginLeft = 0;
            renderer.PrintOptions.MarginRight = 0;
            var pdf = renderer.RenderHtmlAsPdf(html);

            return File(pdf.BinaryData, "application/pdf");
        }

        [AllowAnonymous]
        [HttpGet("ExtendedHoursLicenseVerification")]
        public IActionResult ExtendedHoursLicenseVerification(string searchref)
        {
            var model = new ExtendedHoursCertificateVerificationViewModel();

            if (string.IsNullOrWhiteSpace(searchref))
            {
                model.IsValid = false;
                model.Message = "No certificate reference was supplied.";
                return View(model);
            }

            var extendedHours = FindExtendedHoursCertificate(searchref);
            if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
            {
                model.IsValid = false;
                model.Message = "The certificate could not be verified from the supplied reference.";
                return View(model);
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == extendedHours.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == extendedHours.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                model.IsValid = false;
                model.Message = "The certificate record exists, but its application details are incomplete.";
                return View(model);
            }

            var approvalDate = extendedHours.DateOfApproval ?? extendedHours.DateUpdated;
            var issuedDate = approvalDate == default ? (DateTime?)null : approvalDate;
            var extendedHoursDate = extendedHours.ExtendedHoursDate == default
                ? (DateTime?)null
                : extendedHours.ExtendedHoursDate;

            model.IsValid = string.Equals(extendedHours.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            model.Message = model.IsValid
                ? "This certificate is valid and matches an approved extended hours application in the system."
                : $"This record was found, but its current status is '{extendedHours.Status ?? "Unknown"}'.";
            model.CertificateReference = extendedHours.Reference ?? extendedHours.Id;
            model.Status = extendedHours.Status ?? "Unknown";
            model.TradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            model.BusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            model.LLBNumber = application.LLBNum ?? "N/A";
            model.LicenseName = license?.LicenseName ?? "N/A";
            model.RegionName = region?.RegionName ?? "N/A";
            model.Council = outlet.Council ?? "N/A";
            model.Address = outlet.Address ?? application.OperationAddress ?? "N/A";
            model.ExtendedHoursDate = extendedHoursDate;
            model.ApprovedOn = issuedDate;
            model.Justification = extendedHours.ReasonForExtention ?? "Not provided";

            return View(model);
        }

        [HttpGet("TemporaryRetailLicense")]
        public IActionResult TemporaryRetailLicense(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                TempData["error"] = "The temporary retail certificate could not be found.";
                return RedirectToAction("TemporaryRetailListings", "Home");
            }

            var temporaryRetail = FindTemporaryRetailCertificate(searchref);
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
            {
                TempData["error"] = "The temporary retail certificate could not be found.";
                return RedirectToAction("TemporaryRetailListings", "Home");
            }

            if (!string.Equals(temporaryRetail.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "The temporary retail certificate is only available after the application has been approved.";
                return RedirectToAction("TemporaryRetails", "Postprocess", new { id = temporaryRetail.ApplicationId, process = "TRL" });
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == temporaryRetail.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == temporaryRetail.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                TempData["error"] = "The temporary retail certificate could not be generated because application details are incomplete.";
                return RedirectToAction("TemporaryRetailListings", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            if (User.IsInRole("client")
                && !string.Equals(application.UserID, currentUserId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(temporaryRetail.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var approvalDate = temporaryRetail.DateOfApproval ?? temporaryRetail.DateUpdated;
            var issuedDate = approvalDate == default ? DateTime.Now : approvalDate;
            var temporaryRetailDate = temporaryRetail.TemporaryRetailsDate == default
                ? issuedDate
                : temporaryRetail.TemporaryRetailsDate;

            var boardHeading = EncodeHtml("L I Q U O R   L I C E N S I N G   B O A R D");
            var certificateTitle = EncodeHtml("Temporary Retail Licence Certificate");
            var rawTradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            var rawBusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            var rawLlbNumber = application.LLBNum ?? "N/A";
            var rawReference = temporaryRetail.Reference ?? "N/A";
            var rawLicenseName = license?.LicenseName ?? "N/A";
            var rawRegionName = region?.RegionName ?? "N/A";
            var rawAddress = temporaryRetail.LocationAddress ?? outlet.Address ?? application.OperationAddress ?? "N/A";
            var rawCouncil = outlet.Council ?? "N/A";
            var rawReason = temporaryRetail.ReasonForExtention ?? "Not provided";
            var tradingName = EncodeHtml(rawTradingName);
            var businessName = EncodeHtml(rawBusinessName);
            var llbNumber = EncodeHtml(rawLlbNumber);
            var reference = EncodeHtml(rawReference);
            var licenseName = EncodeHtml(rawLicenseName);
            var regionName = EncodeHtml(rawRegionName);
            var address = EncodeHtml(rawAddress);
            var council = EncodeHtml(rawCouncil);
            var reason = EncodeHtml(rawReason);
            var certificateStatement = EncodeHtml(
                $"{rawTradingName} has been approved to conduct temporary retail trading on {temporaryRetailDate:dd MMMM yyyy} at {rawAddress}.");
            var coatOfArmsDataUri = GetImageDataUri(Path.Combine(_env.WebRootPath, "front", "img", "IMG", "Coat_of_arms_of_ZimbabweB.png"));
            var coatOfArmsMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<div class='crest'><img src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' /></div>";
            var verificationReference = temporaryRetail.Reference ?? temporaryRetail.Id;
            var verificationUrl = VerificationLinkHelper.BuildLiveUrl($"Documents/TemporaryRetailLicenseVerification?searchref={Uri.EscapeDataString(verificationReference)}");
            var qrCodeDataUri = GenerateQrCodeDataUri(verificationUrl);
            var qrLogoMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<img class='qr-logo' src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' />";
            var qrCodeMarkup = string.IsNullOrWhiteSpace(qrCodeDataUri)
                ? string.Empty
                : $@"<div class='verification'>
      <div class='verification-label'>Scan To Verify</div>
      <div class='verification-qr'>
        <img class='qr-code' src='{qrCodeDataUri}' alt='Certificate verification QR code' />
        {qrLogoMarkup}
      </div>
      <div class='verification-caption'>Temporary Retail Certificate</div>
    </div>";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <style>
    @@page {{
      size: A4 portrait;
      margin: 0;
    }}
    html,
    body {{
      font-family: 'Times New Roman', serif;
      color: #10243d;
      margin: 0;
      width: 210mm;
      height: 297mm;
    }}
    .certificate {{
      width: 210mm;
      min-height: 297mm;
      box-sizing: border-box;
      border: 8px solid #b48a3c;
      padding: 10mm 10mm 8mm 10mm;
      position: relative;
      background: #fffdfa;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }}
    .certificate::before {{
      content: '';
      position: absolute;
      inset: 3mm;
      border: 1px solid #d4bf8d;
      pointer-events: none;
    }}
    .header {{
      text-align: center;
      margin-bottom: 8px;
    }}
    .board {{
      font-size: 13.5px;
      letter-spacing: 2px;
      text-transform: uppercase;
      color: #7a5a1c;
      margin-bottom: 4px;
      font-weight: bold;
      line-height: 1.25;
    }}
    .crest {{
      display: flex;
      justify-content: center;
      margin: 2px 0 6px 0;
    }}
    .crest img {{
      width: 22mm;
      height: auto;
    }}
    h1 {{
      font-size: 24px;
      margin: 0 0 3px 0;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      line-height: 1.2;
    }}
    .subtitle {{
      font-size: 11px;
      margin: 0;
      color: #4d5c6d;
      line-height: 1.35;
    }}
    .statement {{
      margin: 8px 0 8px 0;
      font-size: 13px;
      line-height: 1.5;
      text-align: center;
    }}
    .emphasis {{
      font-weight: bold;
      color: #0b4a76;
    }}
    .details-wrap {{
      flex: 1 1 auto;
      display: flex;
      margin-top: 4px;
      min-height: 0;
    }}
    .details {{
      width: 100%;
      height: 100%;
      border-collapse: collapse;
      table-layout: fixed;
      font-size: 11.6px;
      flex: 1 1 auto;
    }}
    .details tbody {{
      height: 100%;
    }}
    .details tbody tr {{
      height: 10%;
    }}
    .details th,
    .details td {{
      border: 1px solid #d7dfe8;
      padding: 7px 7px;
      vertical-align: middle;
      line-height: 1.4;
    }}
    .details th {{
      width: 28%;
      text-align: left;
      background: #f4efe2;
      color: #5f4d24;
      text-transform: uppercase;
      font-size: 10.2px;
      letter-spacing: 0.45px;
    }}
    .declaration {{
      margin-top: 8px;
      padding: 8px 10px;
      background: #f8fafc;
      border-left: 3px solid #0b4a76;
      font-size: 11.2px;
      line-height: 1.45;
    }}
    .body-content {{
      flex: 1 1 auto;
      display: flex;
      flex-direction: column;
    }}
    .bottom-row {{
      margin-top: 10px;
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      align-items: end;
      gap: 10px;
    }}
    .signature {{
      min-width: 130px;
      padding-top: 16px;
      border-top: 1px solid #10243d;
      text-align: center;
      font-size: 10.6px;
      line-height: 1.35;
      justify-self: end;
    }}
    .issued {{
      font-size: 10.6px;
      line-height: 1.45;
      align-self: end;
    }}
    .verification {{
      text-align: center;
      align-self: end;
    }}
    .verification-qr {{
      position: relative;
      width: 34mm;
      height: 34mm;
      margin: 0 auto 3px auto;
    }}
    .verification-qr .qr-code {{
      width: 100%;
      height: 100%;
      display: block;
    }}
    .verification-qr .qr-logo {{
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
      width: 9.5mm;
      height: 9.5mm;
      object-fit: contain;
      background: #fffdfa;
      border-radius: 50%;
      padding: 1mm;
      box-sizing: border-box;
    }}
    .verification-label {{
      font-size: 10px;
      text-transform: uppercase;
      letter-spacing: 0.6px;
      font-weight: bold;
      color: #7a5a1c;
      margin-bottom: 2px;
    }}
    .verification-caption {{
      font-size: 9px;
      color: #4d5c6d;
      line-height: 1.3;
    }}
  </style>
</head>
<body>
  <div class='certificate'>
    <div class='body-content'>
      <div class='header'>
        <div class='board'>{boardHeading}</div>
        {coatOfArmsMarkup}
        <h1>{certificateTitle}</h1>
        <p class='subtitle'>This certificate confirms board approval for temporary retail trading on the stated date.</p>
      </div>

      <div class='statement'>
        This is to certify that <span class='emphasis'>{tradingName}</span>, operating under
        LLB Licence Number <span class='emphasis'>{llbNumber}</span>, has been authorised by the
        Liquor Licensing Board to conduct temporary retail trading on <span class='emphasis'>{temporaryRetailDate:dd MMMM yyyy}</span>.
      </div>

      <div class='details-wrap'>
        <table class='details'>
          <tbody>
            <tr>
              <th>Trading Name</th>
              <td>{tradingName}</td>
            </tr>
            <tr>
              <th>Business Name</th>
              <td>{businessName}</td>
            </tr>
            <tr>
              <th>Licence Type</th>
              <td>{licenseName}</td>
            </tr>
            <tr>
              <th>Region</th>
              <td>{regionName}</td>
            </tr>
            <tr>
              <th>Approved Venue</th>
              <td>{address}</td>
            </tr>
            <tr>
              <th>Council</th>
              <td>{council}</td>
            </tr>
            <tr>
              <th>Temporary Retail Date</th>
              <td>{temporaryRetailDate:dddd, dd MMMM yyyy}</td>
            </tr>
            <tr>
              <th>Application Reference</th>
              <td>{reference}</td>
            </tr>
            <tr>
              <th>Approved On</th>
              <td>{issuedDate:dd MMMM yyyy}</td>
            </tr>
            <tr>
              <th>Purpose / Justification</th>
              <td>{reason}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class='declaration'>
        {certificateStatement} This approval is valid only for the date and venue stated on this certificate and must be presented together with the main liquor licence whenever required by an inspecting authority.
      </div>
    </div>

    <div class='bottom-row'>
      <div class='issued'>
        <div><strong>Issued Date:</strong> {issuedDate:dd MMMM yyyy}</div>
        <div><strong>Certificate Ref:</strong> {reference}</div>
      </div>
      {qrCodeMarkup}
      <div class='signature'>
        For: Liquor Licensing Board
      </div>
    </div>
  </div>
</body>
</html>";

            var renderer = new HtmlToPdf();
            renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            renderer.PrintOptions.MarginTop = 0;
            renderer.PrintOptions.MarginBottom = 0;
            renderer.PrintOptions.MarginLeft = 0;
            renderer.PrintOptions.MarginRight = 0;
            var pdf = renderer.RenderHtmlAsPdf(html);

            return File(pdf.BinaryData, "application/pdf");
        }

        [AllowAnonymous]
        [HttpGet("TemporaryRetailLicenseVerification")]
        public IActionResult TemporaryRetailLicenseVerification(string searchref)
        {
            var model = new TemporaryRetailCertificateVerificationViewModel();

            if (string.IsNullOrWhiteSpace(searchref))
            {
                model.IsValid = false;
                model.Message = "No certificate reference was supplied.";
                return View(model);
            }

            var temporaryRetail = FindTemporaryRetailCertificate(searchref);
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
            {
                model.IsValid = false;
                model.Message = "The certificate could not be verified from the supplied reference.";
                return View(model);
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == temporaryRetail.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == temporaryRetail.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                model.IsValid = false;
                model.Message = "The certificate record exists, but its application details are incomplete.";
                return View(model);
            }

            var approvalDate = temporaryRetail.DateOfApproval ?? temporaryRetail.DateUpdated;
            var issuedDate = approvalDate == default ? (DateTime?)null : approvalDate;
            var temporaryRetailDate = temporaryRetail.TemporaryRetailsDate == default
                ? (DateTime?)null
                : temporaryRetail.TemporaryRetailsDate;

            model.IsValid = string.Equals(temporaryRetail.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            model.Message = model.IsValid
                ? "This certificate is valid and matches an approved temporary retail application in the system."
                : $"This record was found, but its current status is '{temporaryRetail.Status ?? "Unknown"}'.";
            model.CertificateReference = temporaryRetail.Reference ?? temporaryRetail.Id;
            model.Status = temporaryRetail.Status ?? "Unknown";
            model.TradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            model.BusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            model.LLBNumber = application.LLBNum ?? "N/A";
            model.LicenseName = license?.LicenseName ?? "N/A";
            model.RegionName = region?.RegionName ?? "N/A";
            model.Council = outlet.Council ?? "N/A";
            model.Address = temporaryRetail.LocationAddress ?? outlet.Address ?? application.OperationAddress ?? "N/A";
            model.TemporaryRetailDate = temporaryRetailDate;
            model.ApprovedOn = issuedDate;
            model.Justification = temporaryRetail.ReasonForExtention ?? "Not provided";

            return View(model);
        }

        [HttpGet("InspectionComplianceCertificate")]
        public IActionResult InspectionComplianceCertificate(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                TempData["error"] = "The inspection certificate could not be found.";
                return RedirectToAction("InspectionListings", "Home");
            }

            var inspection = FindInspectionRecord(searchref);
            if (inspection == null || string.IsNullOrWhiteSpace(inspection.ApplicationId))
            {
                TempData["error"] = "The inspection certificate could not be found.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (!string.Equals(inspection.Service, "Inspection", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only post-formation inspection certificates are available here.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (!string.Equals(inspection.Status, "Inspected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "The inspection certificate is only available after the inspection report has been submitted.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (!IsPassed(inspection.Overall))
            {
                return RedirectToAction("InspectionFailedReport", new { searchref = inspection.Id });
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == inspection.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == inspection.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                TempData["error"] = "The inspection certificate could not be generated because application details are incomplete.";
                return RedirectToAction("InspectionListings", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            if (User.IsInRole("client")
                && !string.Equals(application.UserID, currentUserId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(inspection.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var issuedDate = inspection.InspectionDate == default ? DateTime.Now : inspection.InspectionDate;
            var rawTradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            var rawBusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            var rawLlbNumber = application.LLBNum ?? "N/A";
            var rawReference = inspection.Reference ?? inspection.Id ?? "N/A";
            var rawLicenseName = license?.LicenseName ?? "N/A";
            var rawRegionName = region?.RegionName ?? "N/A";
            var rawAddress = outlet.Address ?? application.OperationAddress ?? "N/A";
            var rawCouncil = outlet.Council ?? "N/A";
            var rawComments = inspection.Comments ?? "No comments recorded.";
            var criteriaRows = BuildInspectionCriteriaRows(inspection);
            var coatOfArmsDataUri = GetImageDataUri(Path.Combine(_env.WebRootPath, "front", "img", "IMG", "Coat_of_arms_of_ZimbabweB.png"));
            var coatOfArmsMarkup = string.IsNullOrWhiteSpace(coatOfArmsDataUri)
                ? string.Empty
                : $"<div class='crest'><img src='{coatOfArmsDataUri}' alt='Zimbabwe Coat of Arms' /></div>";

            var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <style>
    body {{ margin: 0; font-family: Arial, Helvetica, sans-serif; color: #17212b; background: #f3f5f7; }}
    .document {{ width: 760px; min-height: 1030px; margin: 0 auto; padding: 42px 46px; background: #fff; box-sizing: border-box; }}
    .header {{ text-align: center; border-bottom: 3px solid #183b56; padding-bottom: 18px; margin-bottom: 24px; }}
    .board {{ font-size: 14px; font-weight: 700; letter-spacing: 2px; color: #183b56; }}
    .crest img {{ height: 82px; margin: 12px auto; display: block; }}
    h1 {{ margin: 8px 0 6px; font-size: 25px; text-transform: uppercase; }}
    .subtitle {{ margin: 0; font-size: 13px; color: #52616f; }}
    .statement {{ border: 1px solid #cad4dd; padding: 18px; font-size: 16px; line-height: 1.6; margin-bottom: 20px; }}
    .emphasis {{ font-weight: 700; color: #0f5132; }}
    table {{ width: 100%; border-collapse: collapse; margin-bottom: 18px; }}
    th, td {{ border: 1px solid #d9e1e8; padding: 9px 10px; font-size: 12px; vertical-align: top; }}
    th {{ width: 34%; background: #eef3f7; text-align: left; color: #243746; }}
    .criteria th {{ width: auto; }}
    .pass {{ color: #0f5132; font-weight: 700; }}
    .comments {{ border: 1px solid #d9e1e8; padding: 13px; font-size: 12px; line-height: 1.5; min-height: 56px; }}
    .footer {{ margin-top: 28px; display: flex; justify-content: space-between; align-items: flex-end; font-size: 12px; }}
    .signature {{ border-top: 1px solid #17212b; padding-top: 8px; min-width: 210px; text-align: center; }}
  </style>
</head>
<body>
  <div class='document'>
    <div class='header'>
      <div class='board'>LIQUOR LICENSING BOARD</div>
      {coatOfArmsMarkup}
      <h1>Inspection Compliance Certificate</h1>
      <p class='subtitle'>Post-formation inspection compliance certificate</p>
    </div>

    <div class='statement'>
      This is to certify that <span class='emphasis'>{EncodeHtml(rawTradingName)}</span>, operating under
      LLB Licence Number <span class='emphasis'>{EncodeHtml(rawLlbNumber)}</span>, passed the post-formation
      inspection and was found compliant with the applicable liquor licensing regulations.
    </div>

    <table>
      <tbody>
        <tr><th>Trading Name</th><td>{EncodeHtml(rawTradingName)}</td></tr>
        <tr><th>Business Name</th><td>{EncodeHtml(rawBusinessName)}</td></tr>
        <tr><th>Licence Type</th><td>{EncodeHtml(rawLicenseName)}</td></tr>
        <tr><th>Region</th><td>{EncodeHtml(rawRegionName)}</td></tr>
        <tr><th>LLB Number</th><td>{EncodeHtml(rawLlbNumber)}</td></tr>
        <tr><th>Outlet Address</th><td>{EncodeHtml(rawAddress)}</td></tr>
        <tr><th>Council</th><td>{EncodeHtml(rawCouncil)}</td></tr>
        <tr><th>Inspection Reference</th><td>{EncodeHtml(rawReference)}</td></tr>
        <tr><th>Inspection Date</th><td>{issuedDate:dd MMMM yyyy}</td></tr>
      </tbody>
    </table>

    <table class='criteria'>
      <thead><tr><th>Inspection Item</th><th>Result</th></tr></thead>
      <tbody>{criteriaRows}</tbody>
    </table>

    <div class='comments'><strong>Inspector Comments:</strong><br />{EncodeHtml(rawComments)}</div>

    <div class='footer'>
      <div>
        <div><strong>Issued Date:</strong> {issuedDate:dd MMMM yyyy}</div>
        <div><strong>Certificate Ref:</strong> {EncodeHtml(rawReference)}</div>
      </div>
      <div class='signature'>For: Liquor Licensing Board</div>
    </div>
  </div>
</body>
</html>";

            var renderer = new HtmlToPdf();
            renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            renderer.PrintOptions.MarginTop = 0;
            renderer.PrintOptions.MarginBottom = 0;
            renderer.PrintOptions.MarginLeft = 0;
            renderer.PrintOptions.MarginRight = 0;
            var pdf = renderer.RenderHtmlAsPdf(html);
            return File(pdf.BinaryData, "application/pdf", $"{SanitizeFileName(rawTradingName)}-inspection-certificate.pdf");
        }

        [HttpGet("InspectionFailedReport")]
        public IActionResult InspectionFailedReport(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                TempData["error"] = "The failed inspection report could not be found.";
                return RedirectToAction("InspectionListings", "Home");
            }

            var inspection = FindInspectionRecord(searchref);
            if (inspection == null || string.IsNullOrWhiteSpace(inspection.ApplicationId))
            {
                TempData["error"] = "The failed inspection report could not be found.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (!string.Equals(inspection.Service, "Inspection", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only post-formation inspection reports are available here.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (!string.Equals(inspection.Status, "Inspected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "The failed inspection report is only available after the inspection report has been submitted.";
                return RedirectToAction("InspectionListings", "Home");
            }

            if (IsPassed(inspection.Overall))
            {
                return RedirectToAction("InspectionComplianceCertificate", new { searchref = inspection.Id });
            }

            var application = _db.ApplicationInfo.FirstOrDefault(record => record.Id == inspection.ApplicationId);
            var outlet = _db.OutletInfo.FirstOrDefault(record => record.ApplicationId == inspection.ApplicationId);
            var license = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(record => record.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(record => record.Id == application.ApplicationType);

            if (application == null || outlet == null)
            {
                TempData["error"] = "The failed inspection report could not be generated because application details are incomplete.";
                return RedirectToAction("InspectionListings", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            if (User.IsInRole("client")
                && !string.Equals(application.UserID, currentUserId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(inspection.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var issuedDate = inspection.InspectionDate == default ? DateTime.Now : inspection.InspectionDate;
            var rawTradingName = outlet.TradingName ?? application.BusinessName ?? "N/A";
            var rawBusinessName = application.BusinessName ?? outlet.TradingName ?? "N/A";
            var rawLlbNumber = application.LLBNum ?? "N/A";
            var rawReference = inspection.Reference ?? inspection.Id ?? "N/A";
            var rawLicenseName = license?.LicenseName ?? "N/A";
            var rawRegionName = region?.RegionName ?? "N/A";
            var rawAddress = outlet.Address ?? application.OperationAddress ?? "N/A";
            var rawCouncil = outlet.Council ?? "N/A";
            var rawComments = inspection.Comments ?? "No comments recorded.";
            var criteriaRows = BuildInspectionCriteriaRows(inspection);

            var html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <style>
    body {{ margin: 0; font-family: Arial, Helvetica, sans-serif; color: #17212b; background: #f3f5f7; }}
    .document {{ width: 760px; min-height: 1030px; margin: 0 auto; padding: 42px 46px; background: #fff; box-sizing: border-box; }}
    .header {{ border-bottom: 3px solid #7f1d1d; padding-bottom: 16px; margin-bottom: 22px; }}
    .board {{ font-size: 14px; font-weight: 700; letter-spacing: 2px; color: #183b56; }}
    h1 {{ margin: 12px 0 6px; font-size: 25px; text-transform: uppercase; color: #7f1d1d; }}
    .subtitle {{ margin: 0; font-size: 13px; color: #52616f; }}
    .notice {{ border: 1px solid #f1c0c0; background: #fff4f4; padding: 16px; font-size: 14px; line-height: 1.6; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; margin-bottom: 18px; }}
    th, td {{ border: 1px solid #d9e1e8; padding: 9px 10px; font-size: 12px; vertical-align: top; }}
    th {{ width: 34%; background: #eef3f7; text-align: left; color: #243746; }}
    .criteria th {{ width: auto; }}
    .pass {{ color: #0f5132; font-weight: 700; }}
    .fail {{ color: #842029; font-weight: 700; }}
    .comments {{ border: 1px solid #d9e1e8; padding: 13px; font-size: 12px; line-height: 1.5; min-height: 56px; }}
    .footer {{ margin-top: 28px; display: flex; justify-content: space-between; align-items: flex-end; font-size: 12px; }}
    .signature {{ border-top: 1px solid #17212b; padding-top: 8px; min-width: 210px; text-align: center; }}
  </style>
</head>
<body>
  <div class='document'>
    <div class='header'>
      <div class='board'>LIQUOR LICENSING BOARD</div>
      <h1>Failed Inspection Report</h1>
      <p class='subtitle'>Post-formation inspection report</p>
    </div>

    <div class='notice'>
      The post-formation inspection for <strong>{EncodeHtml(rawTradingName)}</strong> was not passed.
      Corrective action is required before resubmission. A fresh inspection application and payment must be submitted after corrective action.
    </div>

    <table>
      <tbody>
        <tr><th>Trading Name</th><td>{EncodeHtml(rawTradingName)}</td></tr>
        <tr><th>Business Name</th><td>{EncodeHtml(rawBusinessName)}</td></tr>
        <tr><th>Licence Type</th><td>{EncodeHtml(rawLicenseName)}</td></tr>
        <tr><th>Region</th><td>{EncodeHtml(rawRegionName)}</td></tr>
        <tr><th>LLB Number</th><td>{EncodeHtml(rawLlbNumber)}</td></tr>
        <tr><th>Outlet Address</th><td>{EncodeHtml(rawAddress)}</td></tr>
        <tr><th>Council</th><td>{EncodeHtml(rawCouncil)}</td></tr>
        <tr><th>Inspection Reference</th><td>{EncodeHtml(rawReference)}</td></tr>
        <tr><th>Inspection Date</th><td>{issuedDate:dd MMMM yyyy}</td></tr>
        <tr><th>Overall Remark</th><td><span class='fail'>Failed</span></td></tr>
      </tbody>
    </table>

    <table class='criteria'>
      <thead><tr><th>Inspection Item</th><th>Result</th></tr></thead>
      <tbody>{criteriaRows}</tbody>
    </table>

    <div class='comments'><strong>Inspector Comments:</strong><br />{EncodeHtml(rawComments)}</div>

    <div class='footer'>
      <div>
        <div><strong>Report Date:</strong> {issuedDate:dd MMMM yyyy}</div>
        <div><strong>Report Ref:</strong> {EncodeHtml(rawReference)}</div>
      </div>
      <div class='signature'>For: Liquor Licensing Board</div>
    </div>
  </div>
</body>
</html>";

            var renderer = new HtmlToPdf();
            renderer.PrintOptions.PaperSize = PdfPrintOptions.PdfPaperSize.A4;
            renderer.PrintOptions.MarginTop = 0;
            renderer.PrintOptions.MarginBottom = 0;
            renderer.PrintOptions.MarginLeft = 0;
            renderer.PrintOptions.MarginRight = 0;
            var pdf = renderer.RenderHtmlAsPdf(html);
            return File(pdf.BinaryData, "application/pdf", $"{SanitizeFileName(rawTradingName)}-failed-inspection-report.pdf");
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

        private static string BuildAgentLicenseeName(ApplicationInfo application)
        {
            var title = FormatAgentTitle(application.Title);
            var names = new[] { title, application.PlaceOfBirth, application.PlaceOfEntry }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim());

            var fullName = string.Join(" ", names);
            return string.IsNullOrWhiteSpace(fullName)
                ? FirstNonEmpty(application.BusinessName, "N/A")
                : fullName;
        }

        private static string FormatAgentTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            var normalizedTitle = title.Trim();
            if (normalizedTitle.EndsWith(".", StringComparison.Ordinal))
            {
                return normalizedTitle;
            }

            var titlesWithFullStop = new[] { "Mr", "Mrs", "Ms", "Dr", "Prof" };
            return titlesWithFullStop.Any(value => string.Equals(value, normalizedTitle, StringComparison.OrdinalIgnoreCase))
                ? $"{normalizedTitle}."
                : normalizedTitle;
        }

        private static string BuildAgentLicenseVerificationHtml(
            bool isValid,
            string message,
            ApplicationInfo? application,
            OutletInfo? outlet,
            ApplicationInfo? sourceApplication,
            OutletInfo? sourceOutlet,
            string? suppliedReference)
        {
            var licensee = application == null ? "N/A" : BuildAgentLicenseeName(application);
            var address = application == null ? "N/A" : FirstNonEmpty(outlet?.Address, application.OperationAddress, "N/A");
            var holderName = BuildWholesaleHolderName(sourceApplication, sourceOutlet);
            var status = application?.Status ?? "Unknown";
            var llbNumber = FirstNonEmpty(application?.LLBNum, suppliedReference, "N/A");
            var expiry = application == null ? "N/A" : FormatAgentLicenseDate(application.ExpiryDate);
            var approved = application == null ? "N/A" : FormatAgentLicenseDate(application.ApprovedDate);
            var outcomeClass = isValid ? "valid" : "invalid";
            var outcomeText = isValid ? "VALID" : "NOT VALID";

            return $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>Agent Liquor Licence Verification</title>
  <style>
    body {{
      margin: 0;
      font-family: Arial, Helvetica, sans-serif;
      background: #f4f6f2;
      color: #1c2520;
    }}

    .wrap {{
      max-width: 760px;
      margin: 32px auto;
      background: #fff;
      border: 1px solid #d8dfd5;
      border-top: 6px solid #0f5f36;
      padding: 28px;
      box-sizing: border-box;
    }}

    h1 {{
      margin: 0 0 6px;
      font-size: 24px;
      color: #0f3f28;
    }}

    .badge {{
      display: inline-block;
      margin: 14px 0;
      padding: 8px 12px;
      color: #fff;
      font-weight: 700;
      letter-spacing: 0;
    }}

    .valid {{
      background: #0f5f36;
    }}

    .invalid {{
      background: #b61f2c;
    }}

    .message {{
      margin: 0 0 20px;
      line-height: 1.45;
    }}

    table {{
      width: 100%;
      border-collapse: collapse;
    }}

    th, td {{
      border: 1px solid #e0e5dc;
      padding: 11px 12px;
      text-align: left;
      vertical-align: top;
      font-size: 14px;
    }}

    th {{
      width: 32%;
      background: #f4f7f1;
      color: #34433a;
    }}
  </style>
</head>
<body>
  <main class='wrap'>
    <h1>Agent Liquor Licence Verification</h1>
    <div class='badge {outcomeClass}'>{outcomeText}</div>
    <p class='message'>{EncodeHtml(message)}</p>

    <table>
      <tbody>
        <tr><th>LLB Number</th><td>{EncodeHtml(llbNumber)}</td></tr>
        <tr><th>Licensee</th><td>{EncodeHtml(licensee)}</td></tr>
        <tr><th>Business Address</th><td>{EncodeHtml(address)}</td></tr>
        <tr><th>Wholesale Holder</th><td>{EncodeHtml(holderName)}</td></tr>
        <tr><th>Status</th><td>{EncodeHtml(status)}</td></tr>
        <tr><th>Approved Date</th><td>{EncodeHtml(approved)}</td></tr>
        <tr><th>Expiry Date</th><td>{EncodeHtml(expiry)}</td></tr>
      </tbody>
    </table>
  </main>
</body>
</html>";
        }

        private static string BuildWholesaleHolderName(ApplicationInfo? sourceApplication, OutletInfo? sourceOutlet)
        {
            var businessName = sourceApplication?.BusinessName?.Trim();
            var tradingName = sourceOutlet?.TradingName?.Trim();

            if (!string.IsNullOrWhiteSpace(businessName)
                && !string.IsNullOrWhiteSpace(tradingName)
                && !string.Equals(businessName, tradingName, StringComparison.OrdinalIgnoreCase))
            {
                return $"{businessName} - {tradingName}";
            }

            return FirstNonEmpty(businessName, tradingName, "N/A");
        }

        private static string FormatAgentLicenseDate(DateTime value)
        {
            return value == default
                ? "N/A"
                : value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string EncodeHtml(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string SanitizeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "certificate";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), character) >= 0 ? '-' : character);
            }

            return builder.ToString();
        }

        private ExtendedHours? FindExtendedHoursCertificate(string searchref)
        {
            var normalizedReference = searchref.Trim();
            return _db.ExtendedHours.FirstOrDefault(record =>
                record.Id == normalizedReference || record.Reference == normalizedReference);
        }

        private TemporaryRetails? FindTemporaryRetailCertificate(string searchref)
        {
            var normalizedReference = searchref.Trim();
            return _db.TemporaryRetails.FirstOrDefault(record =>
                record.Id == normalizedReference || record.Reference == normalizedReference);
        }

        private Inspection? FindInspectionRecord(string searchref)
        {
            var normalizedReference = searchref.Trim();
            return _db.Inspection.FirstOrDefault(record =>
                record.Id == normalizedReference || record.Reference == normalizedReference);
        }

        private static bool IsPassed(string? value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string InspectionOutcomeText(string? value)
        {
            return IsPassed(value) ? "Passed" : "Failed";
        }

        private static string InspectionOutcomeCssClass(string? value)
        {
            return IsPassed(value) ? "pass" : "fail";
        }

        private static string FormatRoomCount(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "0";
        }

        private static string BuildInspectionCriteriaRows(Inspection inspection)
        {
            var rows = new (string Label, string? Value)[]
            {
                ("Ventilation", inspection.Ventilation),
                ("Lighting", inspection.Lighting),
                ("Sewage Disposal And Drainage", inspection.SewageDisposalAndDrainage),
                ("Toilets", inspection.Toilets),
                ("Water Supply", inspection.WaterSupply),
                ("Rubbish Disposal", inspection.RubbishDisposal),
                ("Standard Of Food", inspection.StandardOfFood),
                ("Food Storage Arrangements", inspection.FoodStorageArrangements),
                ("Staff Uniforms And Accommodation", inspection.StaffUniformsAndAccommodation),
                ("Equipment And Appointments", inspection.EquipmentAndAppointments),
                ("Hygiene Standards", inspection.HygieneStandards),
                ("Overall Remark", inspection.Overall)
            };

            var builder = new StringBuilder();
            foreach (var row in rows)
            {
                builder.Append("<tr><td>");
                builder.Append(EncodeHtml(row.Label));
                builder.Append("</td><td><span class='");
                builder.Append(InspectionOutcomeCssClass(row.Value));
                builder.Append("'>");
                builder.Append(InspectionOutcomeText(row.Value));
                builder.Append("</span></td></tr>");
            }

            return builder.ToString();
        }

        private static string GetImageDataUri(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            var bytes = System.IO.File.ReadAllBytes(path);
            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }

        private static string GenerateQrCodeDataUri(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.H);
            var qrCode = new SvgQRCode(qrCodeData);
            var svg = qrCode.GetGraphic(12);
            return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
        }

    }
}
