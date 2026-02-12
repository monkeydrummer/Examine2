using CAD2DModel.Commands;
using CAD2DModel.Interaction;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
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
        
        // Geometry rule engine
        services.AddSingleton<IGeometryRuleEngine, GeometryRuleEngine>();
        services.AddSingleton<IGeometryModel, GeometryModel>();
        
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

// Placeholder implementations for services not yet implemented

internal class GeometryRuleEngine : IGeometryRuleEngine
{
    public bool Enabled { get; set; } = true;
    public void RegisterRule(IGeometryRule rule) => throw new NotImplementedException();
    public void UnregisterRule(IGeometryRule rule) => throw new NotImplementedException();
    public void ApplyRules(Geometry.IEntity entity, IGeometryModel model) => throw new NotImplementedException();
    public void ApplyAllRules(IGeometryModel model) => throw new NotImplementedException();
}
