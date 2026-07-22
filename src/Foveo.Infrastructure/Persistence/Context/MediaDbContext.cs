using Foveo.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Foveo.Infrastructure.Persistence.Context;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<Media> Media => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
}
