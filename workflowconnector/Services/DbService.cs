using Microsoft.EntityFrameworkCore;
using workflowconnector.Models;

namespace workflowconnector.Services
{
    public class DbService : DbContext
    {
        public DbService(DbContextOptions<DbService> options)
            : base(options)
        {
        }

        public DbSet<LogModel> Logs { get; set; }
    }
}
