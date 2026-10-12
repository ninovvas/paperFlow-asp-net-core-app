using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PaperFlow.Data
{
    public class PowerFlowDbContext : IdentityDbContext
    {
        public PowerFlowDbContext(
            DbContextOptions<PowerFlowDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
