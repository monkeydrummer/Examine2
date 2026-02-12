namespace Examine2DSerialization.Entities;

/// <summary>
/// Base entity class for all database entities
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Project entity
/// </summary>
public class ProjectEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Units { get; set; } = "m"; // metric or imperial
    
    public ICollection<BoundaryEntity> Boundaries { get; set; } = new List<BoundaryEntity>();
    public ICollection<StressGridEntity> StressGrids { get; set; } = new List<StressGridEntity>();
    public ICollection<QueryEntity> Queries { get; set; } = new List<QueryEntity>();
}

/// <summary>
/// Boundary entity (polyline/excavation)
/// </summary>
public class BoundaryEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsClosed { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public string BoundaryType { get; set; } = "Excavation"; // Excavation, External, etc.
    
    public ProjectEntity? Project { get; set; }
    public ICollection<VertexEntity> Vertices { get; set; } = new List<VertexEntity>();
}

/// <summary>
/// Vertex entity
/// </summary>
public class VertexEntity : EntityBase
{
    public Guid BoundaryId { get; set; }
    public int Order { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    
    public BoundaryEntity? Boundary { get; set; }
}

/// <summary>
/// Stress grid entity
/// </summary>
public class StressGridEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public int XPoints { get; set; }
    public int YPoints { get; set; }
    
    public ProjectEntity? Project { get; set; }
}

/// <summary>
/// Query entity (point or polyline)
/// </summary>
public class QueryEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryType { get; set; } = "Point"; // Point, Polyline
    public bool IsVisible { get; set; } = true;
    
    public ProjectEntity? Project { get; set; }
    public ICollection<QueryPointEntity> Points { get; set; } = new List<QueryPointEntity>();
}

/// <summary>
/// Query point entity
/// </summary>
public class QueryPointEntity : EntityBase
{
    public Guid QueryId { get; set; }
    public int Order { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    
    public QueryEntity? Query { get; set; }
}

/// <summary>
/// Material properties entity
/// </summary>
public class MaterialPropertiesEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MaterialType { get; set; } = "Isotropic"; // Isotropic, TransverselyIsotropic
    
    // Isotropic properties
    public double? YoungModulus { get; set; }
    public double? PoissonRatio { get; set; }
    public double? Density { get; set; }
    
    // Transversely isotropic properties
    public double? E1 { get; set; }
    public double? E2 { get; set; }
    public double? Nu12 { get; set; }
    public double? Nu23 { get; set; }
    public double? G12 { get; set; }
    
    public ProjectEntity? Project { get; set; }
}

/// <summary>
/// Strength criterion entity
/// </summary>
public class StrengthCriterionEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CriterionType { get; set; } = "MohrCoulomb"; // MohrCoulomb, HoekBrown
    
    // Mohr-Coulomb
    public double? Cohesion { get; set; }
    public double? FrictionAngle { get; set; }
    
    // Hoek-Brown
    public double? Mb { get; set; }
    public double? S { get; set; }
    public double? A { get; set; }
    public double? Sci { get; set; }
    
    public ProjectEntity? Project { get; set; }
}
