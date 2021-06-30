using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DrawTogether.UI.Server.Areas.Identity.Data.DrawTogether.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DrawTogether.UI.Server.Areas.Identity.Data
{
    public class DTServerContext : IdentityDbContext<ServerUser>
    {
        public DTServerContext(DbContextOptions<DTServerContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
