using Examine2DSerialization.Configurations;
using Examine2DSerialization.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Examine2DSerialization.Data;

/// <summary>
/// EF Core DbContext for Examine2D application
/// </summary>
public class Examine2DContext : DbContext
{
    public Examine2DContext(DbContextOptions<Examine2DContext> options) : base(options)
    {
    }
    
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<BoundaryEntity> Boundaries => Set<BoundaryEntity>();
    public DbSet<VertexEntity> Vertices => Set<VertexEntity>();
    public DbSet<StressGridEntity> StressGrids => Set<StressGridEntity>();
    public DbSet<QueryEntity> Queries => Set<QueryEntity>();
    public DbSet<QueryPointEntity> QueryPoints => Set<QueryPointEntity>();
    public DbSet<MaterialPropertiesEntity> MaterialProperties => Set<MaterialPropertiesEntity>();
    public DbSet<StrengthCriterionEntity> StrengthCriteria => Set<StrengthCriterionEntity>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new ProjectEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BoundaryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VertexEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StressGridEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QueryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QueryPointEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MaterialPropertiesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StrengthCriterionEntityConfiguration());
    }
}

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class Examine2DContextFactory : IDesignTimeDbContextFactory<Examine2DContext>
{
    public Examine2DContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Examine2DContext>();
        optionsBuilder.UseSqlite("Data Source=examine2d.db");
        
        return new Examine2DContext(optionsBuilder.Options);
    }
}
