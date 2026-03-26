using LLB.Data;
using LLB.Models;

namespace LLB.Helpers
{
    public static class DownloadStatusHelper
    {
        public const string LicenseDocumentType = "License";
        public const string DuplicateTaskService = "license duplicate";
        public const string DuplicatePaymentService = "license duplicate";
        public const string LegacyDuplicatePaymentService = "Download";

        public const string DownloadOpenStatus = "Open";
        public const string DownloadClosedStatus = "Closed";

        public const string DuplicateOpenStatus = "Open";
        public const string DuplicateClosedStatus = "Closed";
        public const string DuplicateAwaitingPaymentStatus = "Awaiting Payment";
        public const string DuplicateUnderReviewStatus = "Under Review";
        public const string DuplicateRejectedStatus = "Rejected";

        public static Downloads? GetLicenseDownload(AppDbContext db, string? llbNumber)
        {
            if (string.IsNullOrWhiteSpace(llbNumber))
            {
                return null;
            }

            return db.Downloads
                .Where(x => x.LLBNUM == llbNumber && x.DocumentType == LicenseDocumentType)
                .FirstOrDefault();
        }

        public static Downloads? GetOrCreateLicenseDownload(AppDbContext db, ApplicationInfo? application, string? userId)
        {
            if (application == null || string.IsNullOrWhiteSpace(application.LLBNum))
            {
                return null;
            }

            var download = GetLicenseDownload(db, application.LLBNum);
            if (download != null)
            {
                if (string.IsNullOrWhiteSpace(download.UserId) && !string.IsNullOrWhiteSpace(userId))
                {
                    download.UserId = userId;
                    download.DateUpdated = DateTime.Now;
                    db.Update(download);
                    db.SaveChanges();
                }

                return download;
            }

            var createdDownload = new Downloads
            {
                Id = Guid.NewGuid().ToString(),
                LLBNUM = application.LLBNum,
                UserId = string.IsNullOrWhiteSpace(userId) ? application.UserID : userId,
                DocumentType = LicenseDocumentType,
                Status = DownloadOpenStatus,
                PaymentStatus = DuplicateOpenStatus,
                PaymentRef = string.Empty,
                DownloadCount = 0,
                DateApplied = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            db.Add(createdDownload);
            db.SaveChanges();
            return createdDownload;
        }

        public static Downloads? OpenLicenseDownload(AppDbContext db, ApplicationInfo? application, string? userId)
        {
            var download = GetOrCreateLicenseDownload(db, application, userId);
            if (download == null)
            {
                return null;
            }

            download.Status = DownloadOpenStatus;
            download.PaymentStatus = DuplicateOpenStatus;
            download.DateUpdated = DateTime.Now;
            db.Update(download);
            db.SaveChanges();
            return download;
        }

        public static void CloseLicenseDownload(AppDbContext db, Downloads? download)
        {
            if (download == null)
            {
                return;
            }

            download.Status = DownloadClosedStatus;
            download.PaymentStatus = DuplicateClosedStatus;
            download.DateUpdated = DateTime.Now;
            download.DownloadCount = (download.DownloadCount ?? 0) + 1;
            db.Update(download);
            db.SaveChanges();
        }
    }
}
