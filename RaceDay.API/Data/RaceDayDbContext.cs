using Microsoft.EntityFrameworkCore;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions options)
        : base(options)
        {
        }
    }
}