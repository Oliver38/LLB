using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IronPdf;
using QRCoder;
using System.Net;
using System.Text;
using Webdev.Payments;

namespace LLB.Controllers
{
    [Authorize]
    [Route("GovernmentPermit")]
    public class GovernmentPermitController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskAllocationHelper _taskAllocationHelper;
        private readonly IWebHostEnvironment _env;
        private static readonly string[] ZimbabweMinistries =
        {
            "Defence",
            "Energy and Power Development",
            "Environment, Climate and Wildlife",
            "Finance and Investment Promotion",
            "Foreign Affairs and International Trade",
            "Health and Child Care",
            "Higher and Tertiary Education, Innovation Science and Technology Development",
            "Home Affairs and Cultural Heritage",
            "Industry and Commerce",
            "Information Communication Technology and Courier Services",
            "Information, Publicity and Broadcasting Services",
            "Justice, Legal and Parliamentary Affairs",
            "Local Government, Public Works And National Housing",
            "Mines and Mining Development",
            "Ministry of Lands, Agriculture, Fisheries, Water And Rural Development",
            "National Housing and Social Amenities",
            "Office of the President and Cabinet",
            "Primary and Secondary Education",
            "Public Service, Labour and Social Welfare",
            "Sports Recreation Arts and Culture",
            "Tourism and Hospitality",
            "Transport and Infrastructural Development",
            "Veterans of Liberation Struggle",
            "Women Affairs, Community, Small and Medium Enterprises Development",
            "Youth Empowerment and Development and Vocational Training"
        };

        public GovernmentPermitController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            TaskAllocationHelper taskAllocationHelper,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
            _env = env;
        }

        [HttpGet("Apply")]
        public async Task<IActionResult> ApplyAsync(string? applicationId, string? id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            GovernmentPermit? permit = null;
            ApplicationInfo? sourceApplication = null;

            if (!string.IsNullOrWhiteSpace(id))
            {
                permit = await _db.GovernmentPermit
                    .FirstOrDefaultAsync(item => item.Id == id && item.UserId == currentUser.Id);

                if (permit == null)
                {
                    TempData["error"] = "The government permit application could not be found.";
                    return RedirectToAction("Dashboard", "Home");
                }

                sourceApplication = await GetApplicationAsync(permit.ApplicationId);
                var payment = RefreshGovernmentPermitPaymentStatus(permit);
                await TrySubmitGovernmentPermitForVerificationAsync(permit, payment, showMessages: false);
            }
            else
            {
                sourceApplication = await GetApplicationAsync(applicationId);
            }

            if (!string.IsNullOrWhiteSpace(applicationId) && sourceApplication == null)
            {
                TempData["error"] = "The licence selected for this government permit could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            PopulateApplyView(currentUser, sourceApplication, permit);
            return View();
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(
            string? applicationId,
            string titleOfAuthority,
            string ministry,
            string locationName,
            string address,
            string province,
            string district,
            string council,
            IFormFile letterFromTheSuperior)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                var sourceApplication = await _db.ApplicationInfo
                    .FirstOrDefaultAsync(item => item.Id == applicationId && item.UserID == currentUser.Id);

                if (sourceApplication == null)
                {
                    TempData["error"] = "The licence selected for this government permit could not be found.";
                    return RedirectToAction("Dashboard", "Home");
                }
            }

            titleOfAuthority = (titleOfAuthority ?? string.Empty).Trim();
            ministry = (ministry ?? string.Empty).Trim();
            locationName = (locationName ?? string.Empty).Trim();
            address = (address ?? string.Empty).Trim();
            province = (province ?? string.Empty).Trim();
            district = (district ?? string.Empty).Trim();
            council = (council ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(titleOfAuthority)
                || string.IsNullOrWhiteSpace(ministry)
                || string.IsNullOrWhiteSpace(locationName)
                || string.IsNullOrWhiteSpace(address)
                || string.IsNullOrWhiteSpace(province)
                || string.IsNullOrWhiteSpace(district)
                || string.IsNullOrWhiteSpace(council))
            {
                TempData["error"] = "Complete all government permit details before submitting.";
                return RedirectToAction("Apply", new { applicationId });
            }

            if (letterFromTheSuperior == null || letterFromTheSuperior.Length <= 0)
            {
                TempData["error"] = "Upload the authorisation letter before submitting.";
                return RedirectToAction("Apply", new { applicationId });
            }

            var fee = await GetGovernmentPermitFeeAsync() ?? 0;
            var now = DateTime.Now;
            var permitId = Guid.NewGuid().ToString();
            var permit = new GovernmentPermit
            {
                Id = permitId,
                UserId = currentUser.Id,
                ApplicationId = string.IsNullOrWhiteSpace(applicationId) ? null : applicationId,
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, GovernmentPermitHelper.ProcessCode),
                Status = fee > 0 ? "awaiting payment" : "payment not required",
                TitleOfAuthority = titleOfAuthority,
                Ministry = ministry,
                LocationName = locationName,
                Address = address,
                Province = province,
                District = district,
                Council = council,
                Payment = Convert.ToDouble(fee),
                PaymentStatus = fee > 0 ? "Not Paid" : "Not Required",
                LetterFromTheSuperior = await SaveApplicationAttachmentAsync(letterFromTheSuperior),
                DateAdded = now,
                DateUpdated = now
            };

            _db.Add(permit);
            await _db.SaveChangesAsync();

            if (fee <= 0)
            {
                var submitted = await TrySubmitGovernmentPermitForVerificationAsync(permit, null, showMessages: true);
                if (!submitted && TempData["error"] == null)
                {
                    TempData["success"] = "Government permit application saved.";
                }
            }
            else
            {
                TempData["success"] = "Government permit application saved. Complete payment to submit it for verification.";
            }

            return RedirectToAction("Apply", new { id = permitId });
        }

        [HttpGet("Payment")]
        public async Task<IActionResult> PaymentAsync(string id, string? currency = null)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var permit = await _db.GovernmentPermit.FirstOrDefaultAsync(item => item.Id == id && item.UserId == currentUser.Id);
            if (permit == null)
            {
                TempData["error"] = "The government permit application could not be found.";
                return RedirectToAction("GovernmentPermitListings", "Home");
            }

            if (!IsGovernmentPermitPaymentStage(permit))
            {
                TempData["error"] = "This government permit application is not open for payment.";
                return RedirectToAction("Apply", new { id = permit.Id });
            }

            var fee = Convert.ToDecimal(permit.Payment ?? 0);
            if (fee <= 0)
            {
                permit.PaymentStatus = "Not Required";
                permit.Status = "payment not required";
                permit.DateUpdated = DateTime.Now;
                _db.Update(permit);
                await _db.SaveChangesAsync();

                var submitted = await TrySubmitGovernmentPermitForVerificationAsync(permit, null, showMessages: true);
                if (!submitted && TempData["error"] == null)
                {
                    TempData["success"] = "No payment is required for this government permit application.";
                }

                return RedirectToAction("Apply", new { id = permit.Id });
            }

            var existingTransaction = RefreshGovernmentPermitPaymentStatus(permit);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                permit.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                permit.Status = "paid";
                permit.DateUpdated = DateTime.Now;
                _db.Update(permit);
                await _db.SaveChangesAsync();

                var submitted = await TrySubmitGovernmentPermitForVerificationAsync(permit, existingTransaction, showMessages: true);
                if (!submitted && TempData["error"] == null)
                {
                    TempData["success"] = "This government permit application has already been paid for.";
                }

                return RedirectToAction("Apply", new { id = permit.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current government permit payment before starting another one.";
                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Apply", new { id = permit.Id });
            }

            PaynowCurrencyContext paymentCurrency;
            try
            {
                paymentCurrency = PaynowCurrencyHelper.BuildPaymentContext(_db, fee, currency);
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Apply", new { id = permit.Id });
            }

            var paynow = PaynowCurrencyHelper.CreatePaynow(paymentCurrency);
            var callbackUrl = PaynowCurrencyHelper.BuildReturnUrl("/GovernmentPermit/Apply?id=" + permit.Id, paymentCurrency.PaymentMode);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var payment = paynow.CreatePayment("12345");
            payment.Add(GovernmentPermitHelper.ServiceName, paymentCurrency.PaynowAmount);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Apply", new { id = permit.Id });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUser.Id,
                ApplicationId = permit.Id,
                Service = GovernmentPermitHelper.ServiceName,
                PollUrl = response.PollUrl(),
                PopDoc = string.Empty,
                SystemRef = response.RedirectLink(),
                Status = "not paid",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };
            PaynowCurrencyHelper.ApplyCurrency(transaction, paymentCurrency);

            var status = paynow.PollTransaction(transaction.PollUrl);
            var statusData = status.GetData();
            transaction.PaynowRef = statusData["paynowreference"];
            transaction.PaymentStatus = statusData["status"];

            _db.Add(transaction);
            permit.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
            permit.DateUpdated = DateTime.Now;
            _db.Update(permit);
            await _db.SaveChangesAsync();

            return Redirect(response.RedirectLink());
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplicationsAsync(string id)
        {
            var permit = await _db.GovernmentPermit
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            if (permit == null)
            {
                TempData["error"] = "The government permit application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(permit.Id, includeCompleted: true);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This government permit application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            var model = await BuildReviewModelAsync(permit, task);
            return View(model);
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string id)
        {
            var permit = await _db.GovernmentPermit.FirstOrDefaultAsync(item => item.Id == id);
            if (permit == null)
            {
                TempData["error"] = "The government permit application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(permit.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This government permit application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            var reviewStage = GetReviewStage(task);
            var now = DateTime.Now;
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;

            if (reviewStage == "verification")
            {
                var recommenderId = await GetAvailableRecommenderIdAsync();
                if (string.IsNullOrWhiteSpace(recommenderId))
                {
                    TempData["error"] = "No recommender is currently available to receive this government permit application.";
                    return RedirectToAction("ViewApplications", new { id = permit.Id });
                }

                permit.Status = "verified";
                permit.VerifierId = currentUserId;
                permit.DateVerified = now;
                permit.DateUpdated = now;
                _db.Update(permit);
                CompleteTask(task, now);

                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = permit.Id,
                    RecommenderId = recommenderId,
                    AssignerId = "system",
                    Service = GovernmentPermitHelper.ServiceName,
                    ExaminationStatus = "recommendation",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });

                await _db.SaveChangesAsync();
                TempData["success"] = "Government permit application verified and sent to a recommender.";
                return RedirectToAction("ViewApplications", new { id = permit.Id });
            }

            if (reviewStage == "recommendation")
            {
                var secretaryId = await GetAvailableSecretaryIdAsync();
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive this government permit application.";
                    return RedirectToAction("ViewApplications", new { id = permit.Id });
                }

                permit.Status = "recommended";
                permit.RecommenderId = currentUserId;
                permit.DateRecommended = now;
                permit.DateUpdated = now;
                _db.Update(permit);
                CompleteTask(task, now);

                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = permit.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = GovernmentPermitHelper.ServiceName,
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });

                await _db.SaveChangesAsync();
                TempData["success"] = "Government permit application recommended and sent to the secretary for approval.";
                return RedirectToAction("ViewApplications", new { id = permit.Id });
            }

            permit.Status = "Approved";
            permit.ApproverId = currentUserId;
            permit.DateOfApproval = now;
            permit.DateUpdated = now;
            _db.Update(permit);

            if (task != null)
            {
                CompleteTask(task, now);
            }

            await _db.SaveChangesAsync();
            TempData["success"] = "Government permit application approved.";
            return RedirectToAction("ViewApplications", new { id = permit.Id });
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string id)
        {
            var permit = await _db.GovernmentPermit.FirstOrDefaultAsync(item => item.Id == id);
            if (permit == null)
            {
                TempData["error"] = "The government permit application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(permit.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This government permit application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            permit.Status = "Rejected";
            permit.DateUpdated = DateTime.Now;
            _db.Update(permit);

            if (task != null)
            {
                CompleteTask(task, DateTime.Now);
            }

            await _db.SaveChangesAsync();
            TempData["success"] = "Government permit application rejected.";
            return RedirectToReviewDashboard();
        }

        [HttpGet("Document")]
        public async Task<IActionResult> DocumentAsync(string id)
        {
            var permit = await _db.GovernmentPermit
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id || item.Reference == id);

            if (permit == null)
            {
                TempData["error"] = "The government permit document could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permit.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "The government permit document is only available after approval.";
                return RedirectToAction("Apply", new { id = permit.Id });
            }

            var currentUserId = _userManager.GetUserId(User);
            if (User.IsInRole("client") && !string.Equals(permit.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return GenerateGovernmentPermitPdf(permit);
        }

        [AllowAnonymous]
        [HttpGet("Verification")]
        public async Task<IActionResult> VerificationAsync(string searchref)
        {
            if (string.IsNullOrWhiteSpace(searchref))
            {
                return Content(BuildGovernmentPermitVerificationHtml(
                    false,
                    "No permit reference was supplied.",
                    null,
                    searchref), "text/html");
            }

            var normalizedReference = searchref.Trim();
            var permit = await _db.GovernmentPermit
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == normalizedReference || item.Reference == normalizedReference);

            if (permit == null)
            {
                return Content(BuildGovernmentPermitVerificationHtml(
                    false,
                    "The government permit could not be verified from the supplied reference.",
                    null,
                    normalizedReference), "text/html");
            }

            var isValid = string.Equals(permit.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            var message = isValid
                ? "This government permit is valid and matches an approved record in the system."
                : $"This government permit was found, but its current status is '{permit.Status ?? "Unknown"}'.";

            return Content(BuildGovernmentPermitVerificationHtml(isValid, message, permit, normalizedReference), "text/html");
        }

        private FileContentResult GenerateGovernmentPermitPdf(GovernmentPermit permit)
        {
            var reference = FirstNonEmpty(permit.Reference, permit.Id, "N/A");
            var titleOfAuthority = FirstNonEmpty(permit.TitleOfAuthority, "N/A");
            var ministry = FirstNonEmpty(permit.Ministry, "N/A");
            var locationName = FirstNonEmpty(permit.LocationName, "N/A");
            var issuedDate = permit.DateOfApproval ?? permit.DateUpdated;
            var periodStart = FormatPermitDate(issuedDate);
            var periodEnd = FormatPermitDate(BuildPermitPeriodEnd(issuedDate));
            var verificationUrl = VerificationLinkHelper.BuildLiveUrl($"GovernmentPermit/Verification?searchref={Uri.EscapeDataString(reference)}");
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
        <img class='qr-code' src='{qrCodeDataUri}' alt='Government permit verification QR code' />
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
      background: #fff;
      color: #000;
      font-family: ""Times New Roman"", Times, serif;
    }}

    .page {{
      position: relative;
      width: 210mm;
      height: 297mm;
      overflow: hidden;
      background: linear-gradient(135deg, rgba(244, 196, 48, 0.08), transparent 31%, rgba(0, 122, 61, 0.07) 64%, rgba(190, 32, 46, 0.08));
      box-sizing: border-box;
    }}

    .watermark {{
      position: absolute;
      top: 83mm;
      left: 62mm;
      width: 86mm;
      opacity: 0.035;
    }}

    .watermark img {{
      width: 100%;
    }}

    .permit-header {{
      position: absolute;
      top: 12mm;
      left: 0;
      width: 100%;
      text-align: center;
      z-index: 2;
    }}

    .permit-header img {{
      display: block;
      width: 34mm;
      max-height: 34mm;
      object-fit: contain;
      margin: 0 auto 1.5mm auto;
    }}

    .board-name {{
      width: 34mm;
      margin: 0 auto;
      font-family: Arial, Helvetica, sans-serif;
      font-size: 10px;
      font-weight: 800;
      line-height: 1.08;
      text-align: center;
      text-transform: uppercase;
      letter-spacing: 0;
    }}

    .heading {{
      position: absolute;
      top: 68mm;
      left: 0;
      width: 100%;
      text-align: center;
      z-index: 1;
    }}

    .forms-title {{
      font-size: 18px;
      line-height: 1.1;
    }}

    .permit-title {{
      font-family: Arial, Helvetica, sans-serif;
      font-size: 18px;
      font-weight: 700;
      line-height: 1.05;
    }}

    .issued {{
      font-size: 16px;
      line-height: 1.15;
    }}

    .permit-body {{
      position: absolute;
      top: 93mm;
      left: 15mm;
      right: 15mm;
      font-size: 12px;
      line-height: 1.5;
      z-index: 1;
    }}

    .grant {{
      margin-bottom: 8mm;
    }}

    .bold {{
      font-family: Arial, Helvetica, sans-serif;
      font-weight: 700;
      font-size: 18px;
      line-height: 1.05;
    }}

    .condition {{
      margin: 2.2mm 0;
      padding-left: 14mm;
      text-indent: -6mm;
    }}

    .permit-number {{
      margin-top: 3mm;
      padding-left: 14mm;
      text-indent: -6mm;
    }}

    .date-line {{
      position: absolute;
      left: 14mm;
      bottom: 86mm;
      font-size: 12px;
      z-index: 1;
    }}

    .qr-card {{
      position: absolute;
      right: 16mm;
      bottom: 39mm;
      width: 37mm;
      border: 1px solid rgba(0, 0, 0, 0.25);
      background: rgba(255, 255, 255, 0.88);
      padding: 3mm;
      text-align: center;
      z-index: 1;
    }}

    .qr-title {{
      font-family: Arial, Helvetica, sans-serif;
      font-size: 7.5px;
      font-weight: 700;
      color: #000;
      text-transform: uppercase;
      margin-bottom: 1.8mm;
    }}

    .qr-wrap {{
      position: relative;
      width: 26mm;
      height: 26mm;
      margin: 0 auto;
    }}

    .qr-code {{
      width: 26mm;
      height: 26mm;
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
      left: 17mm;
      bottom: 24mm;
      width: 58mm;
      text-align: left;
      font-family: Arial, Helvetica, sans-serif;
      font-size: 17px;
      z-index: 1;
    }}

    .signature-mark {{
      height: 27mm;
      font-family: ""Brush Script MT"", ""Segoe Script"", cursive;
      font-size: 36px;
      font-style: italic;
      transform: rotate(-7deg);
      transform-origin: center;
      color: #1b1b1b;
    }}

    .signature-line {{
      border-bottom: 2px dotted #000;
      width: 48mm;
      height: 1.5mm;
      margin-bottom: 1mm;
    }}

    .secretary {{
      font-weight: 700;
      white-space: nowrap;
    }}
  </style>
</head>
<body>
  <div class='page'>
    <div class='watermark'>{coatOfArmsMarkup}</div>
    <div class='permit-header'>
      {coatOfArmsMarkup}
      <div class='board-name'>LIQUOR LICENSING BOARD</div>
    </div>
    <div class='heading'>
      <div class='forms-title'>FORMS OF PERMITS</div>
      <div class='permit-title'>PERMITS</div>
      <div class='issued'>(Issued in terms of section 86(I) of the act)</div>
    </div>

    <div class='permit-body'>
      <div class='grant'>
        AUTHORITY is hereby given to <span class='bold'>{EncodeHtml(titleOfAuthority.ToUpperInvariant())}</span>
        for the sale or supply of liquor on the premises known as
        <span class='bold'>{EncodeHtml(locationName.ToUpperInvariant())}</span> during the period
        {EncodeHtml(periodStart)} to {EncodeHtml(periodEnd)} subject to the following conditions
      </div>

      <div class='condition'>
        1. Liquor for consumption on the premises may be sold or supplied to
        <span class='bold'>{EncodeHtml(ministry.ToUpperInvariant())}</span> <span class='bold'>GUESTS</span>
      </div>
      <div class='condition'>
        2. The hours of sale or supply of liquor on any day may be from
        <span class='bold'>10:00 AM to 11:00PM</span> but maybe curtailed on the order of
        <span class='bold'>SECRETARY L.L.B</span> and shall be extended for such periods and on such occasions as may be approved by him.
      </div>
      <div class='condition'>
        3. Liquor for consumption off the premises may be sold or supplied only to the following persons
        <span class='bold'>PERSONS ABOVE 18 YEARS</span>
      </div>
      <div class='permit-number'>
        4. <span class='bold'>Permit Number: {EncodeHtml(reference)}</span>
      </div>
    </div>

    <div class='date-line'>Date: .....{EncodeHtml(periodStart)}</div>
    {qrCodeMarkup}

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

            return File(pdf.BinaryData, "application/pdf", $"{SanitizeFileName(reference)}-government-permit.pdf");
        }

        private void PopulateApplyView(ApplicationUser currentUser, ApplicationInfo? sourceApplication, GovernmentPermit? permit)
        {
            ViewBag.User = currentUser;
            ViewBag.Application = sourceApplication;
            ViewBag.Permit = permit;
            ViewBag.Outlet = sourceApplication == null
                ? null
                : _db.OutletInfo
                    .Where(item => item.ApplicationId == sourceApplication.Id)
                    .OrderByDescending(item => item.Status == "active")
                    .ThenByDescending(item => item.DateUpdated)
                    .FirstOrDefault();
            ViewBag.License = sourceApplication == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(item => item.Id == sourceApplication.LicenseTypeID);
            ViewBag.Region = sourceApplication == null
                ? null
                : _db.LicenseRegions.FirstOrDefault(item => item.Id == sourceApplication.ApplicationType);
            ViewBag.Ministries = ZimbabweMinistries;
            ViewBag.Provinces = _db.Province
                .OrderBy(item => item.Name)
                .ToList();
            ViewBag.Payment = RefreshGovernmentPermitPaymentStatus(permit);
        }

        private async Task<GovernmentPermitReviewViewModel> BuildReviewModelAsync(GovernmentPermit permit, Tasks? task)
        {
            var application = await GetApplicationAsync(permit.ApplicationId);
            var license = application == null
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == application.LicenseTypeID);
            var region = application == null
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == application.ApplicationType);
            var reviewStage = GetReviewStage(task);

            return new GovernmentPermitReviewViewModel
            {
                Id = permit.Id ?? string.Empty,
                TaskId = task?.Id ?? string.Empty,
                ApplicationId = application?.Id ?? permit.ApplicationId ?? permit.Id ?? string.Empty,
                Reference = permit.Reference ?? string.Empty,
                Status = permit.Status,
                LLBNumber = application?.LLBNum,
                LicenseType = license?.LicenseName,
                LicenseRegion = region?.RegionName,
                TitleOfAuthority = permit.TitleOfAuthority,
                Ministry = permit.Ministry,
                LocationName = permit.LocationName,
                Address = permit.Address,
                Province = permit.Province,
                District = permit.District,
                Council = permit.Council,
                Payment = permit.Payment,
                PaymentStatus = permit.PaymentStatus,
                LG30 = permit.LG30,
                LetterFromTheSuperior = permit.LetterFromTheSuperior,
                RequestedOn = permit.DateAdded,
                CanReviewAction = task != null && string.Equals(task.Status, "assigned", StringComparison.OrdinalIgnoreCase),
                ReviewStageLabel = reviewStage switch
                {
                    "verification" => "Verifier examination",
                    "recommendation" => "Recommender examination",
                    _ => "Secretary approval"
                },
                ApproveButtonLabel = reviewStage switch
                {
                    "verification" => "Verify",
                    "recommendation" => "Recommend",
                    _ => "Approve"
                }
            };
        }

        private async Task<ApplicationInfo?> GetApplicationAsync(string? applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            return await _db.ApplicationInfo.AsNoTracking().FirstOrDefaultAsync(item => item.Id == applicationId);
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            return string.IsNullOrWhiteSpace(currentUserId)
                ? null
                : await _userManager.Users.FirstOrDefaultAsync(item => item.Id == currentUserId);
        }

        private async Task<decimal?> GetGovernmentPermitFeeAsync()
        {
            return await _db.PostFormationFees
                .AsNoTracking()
                .Where(item =>
                    item.Code == GovernmentPermitHelper.ProcessCode
                    || item.ProcessName == GovernmentPermitHelper.ServiceName
                    || item.ProcessName == "Government Department Permit")
                .OrderByDescending(item => item.Code == GovernmentPermitHelper.ProcessCode)
                .ThenByDescending(item => item.DateUpdated)
                .Select(item => (decimal?)item.Fee)
                .FirstOrDefaultAsync();
        }

        private Payments? GetLatestGovernmentPermitPayment(GovernmentPermit? permit)
        {
            if (permit == null || string.IsNullOrWhiteSpace(permit.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(item => item.ApplicationId == permit.Id
                    && (item.Service == GovernmentPermitHelper.ServiceName || item.Service == "government permit"))
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefault();
        }

        private Payments? RefreshGovernmentPermitPaymentStatus(GovernmentPermit? permit)
        {
            var payment = GetLatestGovernmentPermitPayment(permit);
            if (payment == null || string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                return payment;
            }

            var paynow = PaynowCurrencyHelper.CreatePaynow(payment);
            var status = paynow.PollTransaction(payment.PollUrl);
            var statusData = status.GetData();
            payment.PaynowRef = statusData["paynowreference"];
            payment.PaymentStatus = statusData["status"];
            payment.DateUpdated = DateTime.Now;
            _db.Update(payment);

            if (permit != null)
            {
                permit.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? permit.PaymentStatus;
                if (HasPaymentStatus(payment, "Paid") && IsGovernmentPermitPaymentStage(permit))
                {
                    permit.Status = "paid";
                }

                permit.DateUpdated = DateTime.Now;
                _db.Update(permit);
            }

            _db.SaveChanges();
            return payment;
        }

        private async Task<bool> TrySubmitGovernmentPermitForVerificationAsync(
            GovernmentPermit? permit,
            Payments? payment,
            bool showMessages)
        {
            if (permit == null || !IsGovernmentPermitPaymentStage(permit))
            {
                return false;
            }

            var paymentRequired = (permit.Payment ?? 0) > 0;
            var paymentComplete = !paymentRequired
                || (payment != null && HasPaymentStatus(payment, "Paid"))
                || string.Equals(permit.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(permit.PaymentStatus, "Not Required", StringComparison.OrdinalIgnoreCase);

            if (!paymentComplete)
            {
                return false;
            }

            var existingTask = await _db.Tasks.FirstOrDefaultAsync(task =>
                task.ApplicationId == permit.Id
                && task.Service == GovernmentPermitHelper.ServiceName
                && task.Status == "assigned");

            if (existingTask == null)
            {
                var verifierId = await GetAvailableVerifierIdAsync();
                if (string.IsNullOrWhiteSpace(verifierId))
                {
                    if (showMessages)
                    {
                        TempData["error"] = "No verifier is currently available to receive this government permit application.";
                    }

                    return false;
                }

                var now = DateTime.Now;
                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = permit.Id,
                    VerifierId = verifierId,
                    AssignerId = "system",
                    Service = GovernmentPermitHelper.ServiceName,
                    ExaminationStatus = "verification",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });
            }

            permit.Status = "awaiting verification";
            permit.PaymentStatus = payment?.PaymentStatus
                ?? payment?.Status
                ?? permit.PaymentStatus
                ?? (paymentRequired ? "Paid" : "Not Required");
            permit.DateUpdated = DateTime.Now;
            _db.Update(permit);
            await _db.SaveChangesAsync();

            if (showMessages)
            {
                TempData["success"] = "Government permit application submitted for verification.";
            }

            return true;
        }

        private static bool HasPaymentStatus(Payments payment, string expectedStatus)
        {
            return string.Equals(payment.Status, expectedStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(payment.PaymentStatus, expectedStatus, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActivePaymentTransaction(Payments payment)
        {
            return !HasPaymentStatus(payment, "Paid")
                && !HasPaymentStatus(payment, "Cancelled")
                && !HasPaymentStatus(payment, "Canceled")
                && !HasPaymentStatus(payment, "Rejected")
                && !HasPaymentStatus(payment, "Expired")
                && !HasPaymentStatus(payment, "Created")
                && !HasPaymentStatus(payment, "Awaiting Delivery");
        }

        private static bool IsGovernmentPermitPaymentStage(GovernmentPermit? permit)
        {
            var status = permit?.Status?.Trim().ToLowerInvariant();
            return status == "awaiting payment"
                || status == "payment pending"
                || status == "paid"
                || status == "payment not required";
        }

        private async Task<string?> GetAvailableVerifierIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("verifier");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetVerifier(_db, _userManager);
        }

        private async Task<string?> GetAvailableRecommenderIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("recommender");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetRecommender(_db, _userManager);
        }

        private async Task<string?> GetAvailableSecretaryIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("secretary");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetSecretary(_db, _userManager);
        }

        private async Task<Tasks?> GetReviewTaskAsync(string? permitId, bool includeCompleted)
        {
            if (string.IsNullOrWhiteSpace(permitId))
            {
                return null;
            }

            var query = _db.Tasks.Where(item =>
                item.ApplicationId == permitId
                && item.Service == GovernmentPermitHelper.ServiceName);

            if (!includeCompleted)
            {
                query = query.Where(item => item.Status == "assigned");
            }

            if (!CanBypassReviewAssignment())
            {
                var currentUserId = _userManager.GetUserId(User);
                if (User.IsInRole("verifier"))
                {
                    query = query.Where(item => item.VerifierId == currentUserId);
                }
                else if (User.IsInRole("recommender"))
                {
                    query = query.Where(item => item.RecommenderId == currentUserId);
                }
                else
                {
                    query = query.Where(item => item.ApproverId == currentUserId);
                }
            }

            return await query
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private bool CanBypassReviewAssignment()
        {
            return User.IsInRole("admin") || User.IsInRole("super user");
        }

        private IActionResult RedirectToReviewDashboard()
        {
            if (User.IsInRole("verifier"))
            {
                return RedirectToAction("Dashboard", "Verify");
            }

            if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }

            return RedirectToAction("Dashboard", "Approval");
        }

        private static string GetReviewStage(Tasks? task)
        {
            var stage = task?.ExaminationStatus?.Trim().ToLowerInvariant();
            if (stage == "verification" || stage == "recommendation" || stage == "approval")
            {
                return stage;
            }

            if (!string.IsNullOrWhiteSpace(task?.VerifierId))
            {
                return "verification";
            }

            if (!string.IsNullOrWhiteSpace(task?.RecommenderId))
            {
                return "recommendation";
            }

            return "approval";
        }

        private static void CompleteTask(Tasks task, DateTime completedAt)
        {
            task.Status = "completed";
            task.DateUpdated = completedAt;

            var stage = GetReviewStage(task);
            if (stage == "verification")
            {
                task.VerificationDate = completedAt;
            }
            else if (stage == "recommendation")
            {
                task.RecommendationDate = completedAt;
            }
            else
            {
                task.ApprovedDate = completedAt;
            }
        }

        private static async Task<string> SaveApplicationAttachmentAsync(IFormFile file)
        {
            var directory = Path.Combine("wwwroot", "ApplicationAttchments");
            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine("ApplicationAttchments", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(directory, fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            return relativePath;
        }

        private static string BuildGovernmentPermitVerificationHtml(
            bool isValid,
            string message,
            GovernmentPermit? permit,
            string? suppliedReference)
        {
            var outcomeClass = isValid ? "valid" : "invalid";
            var outcomeText = isValid ? "VALID" : "NOT VALID";
            var reference = FirstNonEmpty(permit?.Reference, suppliedReference, "N/A");
            var area = string.Join(", ", new[] { permit?.Council, permit?.District, permit?.Province }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

            return $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>Government Permit Verification</title>
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
    }}

    .valid {{ background: #0f5f36; }}
    .invalid {{ background: #b61f2c; }}
    .message {{ margin: 0 0 20px; line-height: 1.45; }}

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
    <h1>Government Permit Verification</h1>
    <div class='badge {outcomeClass}'>{outcomeText}</div>
    <p class='message'>{EncodeHtml(message)}</p>

    <table>
      <tbody>
        <tr><th>Reference</th><td>{EncodeHtml(reference)}</td></tr>
        <tr><th>Title of Authority</th><td>{EncodeHtml(FirstNonEmpty(permit?.TitleOfAuthority, "N/A"))}</td></tr>
        <tr><th>Ministry</th><td>{EncodeHtml(FirstNonEmpty(permit?.Ministry, "N/A"))}</td></tr>
        <tr><th>Location Name</th><td>{EncodeHtml(FirstNonEmpty(permit?.LocationName, "N/A"))}</td></tr>
        <tr><th>Address</th><td>{EncodeHtml(FirstNonEmpty(permit?.Address, "N/A"))}</td></tr>
        <tr><th>Area</th><td>{EncodeHtml(FirstNonEmpty(area, "N/A"))}</td></tr>
        <tr><th>Status</th><td>{EncodeHtml(permit?.Status ?? "Unknown")}</td></tr>
        <tr><th>Approved Date</th><td>{EncodeHtml(FormatPermitDate(permit?.DateOfApproval))}</td></tr>
      </tbody>
    </table>
  </main>
</body>
</html>";
        }

        private static string EncodeHtml(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
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

        private static string FormatPermitDate(DateTime? value)
        {
            return value.HasValue && value.Value != default
                ? value.Value.ToString("dd.MM.yyyy")
                : "N/A";
        }

        private static DateTime? BuildPermitPeriodEnd(DateTime? issuedDate)
        {
            if (!issuedDate.HasValue || issuedDate.Value == default)
            {
                return null;
            }

            var year = issuedDate.Value.Month > 6 ? issuedDate.Value.Year + 1 : issuedDate.Value.Year;
            return new DateTime(year, 6, 30);
        }

        private static string SanitizeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "government-permit";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), character) >= 0 ? '-' : character);
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
