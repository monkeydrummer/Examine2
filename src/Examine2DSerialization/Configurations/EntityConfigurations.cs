using Examine2DSerialization.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Examine2DSerialization.Configurations;

/// <summary>
/// Project entity configuration
/// </summary>
public class ProjectEntityConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Units).IsRequired().HasMaxLength(20);
        
        builder.HasMany(p => p.Boundaries)
            .WithOne(b => b.Project)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(p => p.StressGrids)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(p => p.Queries)
            .WithOne(q => q.Project)
            .HasForeignKey(q => q.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Boundary entity configuration
/// </summary>
public class BoundaryEntityConfiguration : IEntityTypeConfiguration<BoundaryEntity>
{
    public void Configure(EntityTypeBuilder<BoundaryEntity> builder)
    {
        builder.ToTable("Boundaries");
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BoundaryType).IsRequired().HasMaxLength(50);
        
        builder.HasMany(b => b.Vertices)
            .WithOne(v => v.Boundary)
            .HasForeignKey(v => v.BoundaryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Vertex entity configuration
/// </summary>
public class VertexEntityConfiguration : IEntityTypeConfiguration<VertexEntity>
{
    public void Configure(EntityTypeBuilder<VertexEntity> builder)
    {
        builder.ToTable("Vertices");
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.X).IsRequired();
        builder.Property(v => v.Y).IsRequired();
        builder.Property(v => v.Order).IsRequired();
        
        builder.HasIndex(v => new { v.BoundaryId, v.Order });
    }
}

/// <summary>
/// Stress grid entity configuration
/// </summary>
public class StressGridEntityConfiguration : IEntityTypeConfiguration<StressGridEntity>
{
    public void Configure(EntityTypeBuilder<StressGridEntity> builder)
    {
        builder.ToTable("StressGrids");
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
    }
}

/// <summary>
/// Query entity configuration
/// </summary>
public class QueryEntityConfiguration : IEntityTypeConfiguration<QueryEntity>
{
    public void Configure(EntityTypeBuilder<QueryEntity> builder)
    {
        builder.ToTable("Queries");
        builder.HasKey(q => q.Id);
        
        builder.Property(q => q.Name).IsRequired().HasMaxLength(200);
        builder.Property(q => q.QueryType).IsRequired().HasMaxLength(50);
        
        builder.HasMany(q => q.Points)
            .WithOne(p => p.Query)
            .HasForeignKey(p => p.QueryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Query point entity configuration
/// </summary>
public class QueryPointEntityConfiguration : IEntityTypeConfiguration<QueryPointEntity>
{
    public void Configure(EntityTypeBuilder<QueryPointEntity> builder)
    {
        builder.ToTable("QueryPoints");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.X).IsRequired();
        builder.Property(p => p.Y).IsRequired();
        builder.Property(p => p.Order).IsRequired();
        
        builder.HasIndex(p => new { p.QueryId, p.Order });
    }
}

/// <summary>
/// Material properties entity configuration
/// </summary>
public class MaterialPropertiesEntityConfiguration : IEntityTypeConfiguration<MaterialPropertiesEntity>
{
    public void Configure(EntityTypeBuilder<MaterialPropertiesEntity> builder)
    {
        builder.ToTable("MaterialProperties");
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.MaterialType).IsRequired().HasMaxLength(50);
    }
}

/// <summary>
/// Strength criterion entity configuration
/// </summary>
public class StrengthCriterionEntityConfiguration : IEntityTypeConfiguration<StrengthCriterionEntity>
{
    public void Configure(EntityTypeBuilder<StrengthCriterionEntity> builder)
    {
        builder.ToTable("StrengthCriteria");
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.CriterionType).IsRequired().HasMaxLength(50);
    }
}
