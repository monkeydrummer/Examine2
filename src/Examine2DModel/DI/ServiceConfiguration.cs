using CAD2DModel.Services;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using Examine2DModel.Contours;
using Examine2DModel.Materials;
using Examine2DModel.Query;
using Examine2DModel.Strength;
using Microsoft.Extensions.DependencyInjection;

namespace Examine2DModel.DI;

/// <summary>
/// Extension methods for configuring Examine2D analysis services
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Add all Examine2D analysis services to the service collection
    /// </summary>
    public static IServiceCollection AddExamine2DServices(this IServiceCollection services)
    {
        // Analysis services - using transient for BEM solver since it needs material properties
        // Material will be provided at runtime when creating the solver
        services.AddTransient<IMatrixSolver, MatrixSolverService>();
        
        // Note: IBoundaryElementSolver is created on-demand with specific material properties
        // rather than registered in DI. Use BoundaryElementSolver constructor directly.
        
        // Contour generation - Real BEM-based implementation
        services.AddSingleton<IContourService, Services.ContourService>();
        services.AddSingleton<IContourGenerator, ContourGenerator>();
        
        // Query services
        services.AddSingleton<IFieldQueryService, FieldQueryService>();
        
        return services;
    }
}

internal class ContourGenerator : IContourGenerator
{
    public ContourData Generate(Stress.StressField field, ContourOptions options) => throw new NotImplementedException();
    public Task<ContourData> GenerateAsync(Stress.StressField field, ContourOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

internal class FieldQueryService : IFieldQueryService
{
    public QueryPointResult EvaluateAtPoint(CAD2DModel.Geometry.Point2D location, Stress.StressField field) => throw new NotImplementedException();
    public List<QuerySampleResult> EvaluateAlongPolyline(CAD2DModel.Geometry.Polyline polyline, Stress.StressField field, int sampleCount) => throw new NotImplementedException();
}
