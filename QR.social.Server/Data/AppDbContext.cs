using Microsoft.EntityFrameworkCore;
using QR.social.Server.Models;
using System.Collections.Generic;

namespace QR.social.Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<QRCodeEntity> QRCodes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
