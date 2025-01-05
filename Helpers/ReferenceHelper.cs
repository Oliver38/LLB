using System;
using System.Linq;
using LLB.Data;

namespace LLB.Helpers
{
    public static class ReferenceHelper
    {
        
        public static string GenerateReferenceNumber(AppDbContext db)
        {
            // Fetch the first reference number record
            var refnum = db.ReferenceNumbers.FirstOrDefault();

            if (refnum == null)
            {
                throw new InvalidOperationException("Reference number record is not initialized.");
            }

            // Increment the reference number
            refnum.Number += 1;

            // Save the updated reference number to the database
            db.ReferenceNumbers.Update(refnum);
            db.SaveChanges();

            // Generate a formatted reference string
            var reference = $"D{refnum.Number:D4}";
            return reference;
        }
    }
}
