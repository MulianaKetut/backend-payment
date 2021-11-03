using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentAPI.Models;

namespace PaymentAPI.Contexts
{
    public class AppDbContext : IdentityDbContext
    {
        public virtual DbSet<PaymentDetail> PaymentDetails { get; set; }

        public virtual DbSet<RefreshToken> RefreshToken { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) :
            base(options)
        {
        }
    }
}