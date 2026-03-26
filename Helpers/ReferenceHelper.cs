using System;
using System.Linq;
using LLB.Data;

namespace LLB.Helpers
{
    public static class ReferenceHelper
    {
        public static string GenerateReferenceNumber(AppDbContext db)
        {
            var nextNumber = GetNextReferenceNumber(db);
            return $"D{nextNumber:D4}";
        }

        public static string GeneratePostFormationReferenceNumber(AppDbContext db, string serviceCode)
        {
            var nextNumber = GetNextReferenceNumber(db);
            var normalizedCode = NormalizePostFormationCode(serviceCode);
            return $"PF-{normalizedCode}-{nextNumber:D5}";
        }

        private static int GetNextReferenceNumber(AppDbContext db)
        {
            var refnum = db.ReferenceNumbers.FirstOrDefault();
            if (refnum == null)
            {
                throw new InvalidOperationException("Reference number record is not initialized.");
            }

            refnum.Number = (refnum.Number ?? 0) + 1;
            db.ReferenceNumbers.Update(refnum);
            db.SaveChanges();
            return refnum.Number.Value;
        }

        private static string NormalizePostFormationCode(string serviceCode)
        {
            if (string.IsNullOrWhiteSpace(serviceCode))
            {
                return "GEN";
            }

            var normalized = new string(serviceCode
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "GEN";
            }

            return normalized.Length <= 4 ? normalized : normalized[..4];
        }
    }
}
