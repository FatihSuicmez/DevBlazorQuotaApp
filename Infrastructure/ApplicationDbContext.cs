// Infrastructure/ApplicationDbContext.cs

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuotaApp.Domain.Entities; 

namespace QuotaApp.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Province> Provinces { get; set; }
    public DbSet<County> Counties { get; set; }
    public DbSet<Neighbourhood> Neighbourhoods { get; set; }
    public DbSet<Street> Streets { get; set; }
    public DbSet<Site> Sites { get; set; }
    public DbSet<SearchLog> SearchLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SearchLog>()
            .HasIndex(q => new { q.UserId, q.CreatedAtUtc });

        builder.Entity<County>()
            .HasIndex(c => c.ProvinceId);

        builder.Entity<Neighbourhood>()
            .HasIndex(n => n.CountyId);

        builder.Entity<Street>()
            .HasIndex(s => new { s.NeighbourhoodId, s.Name });

        builder.Entity<Site>()
            .HasIndex(s => new { s.NeighbourhoodId, s.Name });
    }
}