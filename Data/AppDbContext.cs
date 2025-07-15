
using LLB.Models;
using LLB.Models.DataModel;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LLB.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options ) : base ( options )
        {
        
        }
        public DbSet<Branches> Branches { get; set; }
        public DbSet<LicenseTypes> LicenseTypes { get; set; }
        public DbSet<ApplicationInfo> ApplicationInfo { get; set; }
        public DbSet<ManagersParticulars> ManagersParticulars { get; set; }
        public DbSet<LicenseRegion> LicenseRegions { get; set; }
        public DbSet<OutletInfo> OutletInfo { get; set; }
        //public DbSet<ManagersParticulars> ManagersParticulars { get; set; }
        public DbSet<AttachmentInfo> AttachmentInfo { get; set; }
        public DbSet<DirectorDetails> DirectorDetails { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<Queries> Queries{ get; set; }
        public DbSet<Tasks> Tasks { get; set; }
        public DbSet<DistrictCodes> DistrictCodes { get; set; }
        public DbSet<ReferenceNumbers> ReferenceNumbers { get; set; }
        public DbSet<ExchangeRate> ExchangeRate { get; set; }
        public DbSet<Renewals> Renewals { get; set; }
        public DbSet<RenewalRegion> RenewalRegion { get; set; }
        public DbSet<RenewalTypes> RenewalTypes { get; set; }
        public DbSet<PostFormationFees> PostFormationFees { get; set; }
        public DbSet<TransferRegion> TransferRegion { get; set; }
        public DbSet<TransferTypes> TransferTypes { get; set; }
        public DbSet<RemovalRegion> RemovalRegion { get; set; }
        public DbSet<RemovalTypes> RemovalTypes{ get; set; }
        public DbSet<TransferwmanagerRegion> TransferwmanagerRegion{ get; set; }
        public DbSet<TransferwmanagerTypes> TransferwmanagerTypes{ get; set; }
        public DbSet<Province> Province { get; set; }
        public DbSet<Council> Council { get; set; }
        public DbSet<District> District { get; set; }
        public DbSet<Inspection> Inspection { get; set; }
        public DbSet<Downloads> Downloads { get; set; }
        public DbSet<ExtendedHours> ExtendedHours { get; set; }
        public DbSet<TemporaryRetails> TemporaryRetails { get; set; }
        public DbSet<ExtraCounter> ExtraCounter { get; set; }
        



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //foreach (var foreignKey in optionsBuilder.Model.GetEntityTypes()
            //   .SelectMany(e => e.GetForeignKeys()))
            //{
            //    foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
            //}
        }
    }
}
