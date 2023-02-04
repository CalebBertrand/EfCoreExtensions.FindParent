using Microsoft.EntityFrameworkCore;
using Tests.Entities;

namespace Tests
{
    public class MyDbContext : DbContext
    {
        public DbSet<Bolt> Bolts { get; set; }
        public DbSet<Tire> Tires { get; set; }
        public DbSet<Car> Cars { get; set; }

        public MyDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}