using CAD2DModel.Commands;
using CAD2DModel.Interaction;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace CAD2DModel.DI;

/// <summary>
/// Extension methods for configuring CAD2D services
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Add all CAD2D core services to the service collection
    /// </summary>
    public static IServiceCollection AddCAD2DServices(this IServiceCollection services)
    {
        // Core geometry services
        services.AddSingleton<IGeometryEngine, GeometryEngine>();
        services.AddSingleton<ISnapService, SnapService>();
        services.AddSingleton<ISelectionService, SelectionService>();
        services.AddTransient(typeof(ISpatialIndex<>), typeof(SpatialIndex<>));
        
        // Contour service (real BEM-based implementation)
        // NOTE: The concrete implementation (Examine2DModel.Services.ContourService) must be registered
        // in the application's DI container. This placeholder registration can be overridden.
        services.AddSingleton<IContourService, MockContourService>();
        
        // Geometry rule engine and model
        services.AddSingleton<IGeometryRuleEngine>(sp => 
        {
            var engine = new GeometryRuleEngine();
            
            // Register default rules (sorted by priority)
            engine.RegisterRule(new MinimumVertexCountRule());
            engine.RegisterRule(new RemoveDuplicateVerticesRule(tolerance: 0.0001));
            engine.RegisterRule(new MinimumSegmentLengthRule(minimumLength: 0.001));
            engine.RegisterRule(new BoundaryIntersectionRule(tolerance: 1e-6));
            engine.RegisterRule(new CounterClockwiseWindingRule());
            
            return engine;
        });
        
        services.AddSingleton<IGeometryModel>(sp => 
        {
            var model = new GeometryModel();
            var ruleEngine = sp.GetRequiredService<IGeometryRuleEngine>();
            model.SetRuleEngine(ruleEngine);
            return model;
        });
        
        // Command and interaction services
        services.AddSingleton<ICommandManager>(sp => new Commands.CommandManager(maxUndoLevels: 100));
        services.AddSingleton<IModeManager>(sp => new Interaction.Implementations.ModeManager(
            sp.GetRequiredService<ICommandManager>(),
            sp.GetRequiredService<ISelectionService>(),
            sp.GetRequiredService<ISnapService>(),
            sp.GetRequiredService<IGeometryEngine>(),
            sp.GetRequiredService<IGeometryModel>()
        ));
        
        return services;
    }
}
