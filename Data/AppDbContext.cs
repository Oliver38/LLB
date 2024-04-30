
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
        public DbSet<LicenseTypes> licenseTypes { get; set; }
     
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
