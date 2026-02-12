# Geometry Rules System

## Overview

The Geometry Rules system provides a flexible, extensible framework for applying validation and correction rules to geometry entities in the CAD model. Rules execute in priority order and can modify entities or remove them from the model.

## Architecture

### Core Interfaces

**`IGeometryRule`** - Defines a single geometry rule
- `string Name` - Human-readable rule name
- `int Priority` - Execution priority (lower numbers execute first)
- `bool AppliesTo(IEntity entity)` - Check if rule applies to an entity
- `void Apply(IEntity entity, IGeometryModel model)` - Apply the rule

**`IGeometryRuleEngine`** - Manages and executes rules
- `void RegisterRule(IGeometryRule rule)` - Add a new rule
- `void UnregisterRule(IGeometryRule rule)` - Remove a rule
- `void ApplyRules(IEntity entity, IGeometryModel model)` - Apply rules to one entity
- `void ApplyAllRules(IGeometryModel model)` - Apply rules to all entities
- `bool Enabled { get; set; }` - Enable/disable the engine

### Implementation

**`GeometryRuleEngine`** (`src/CAD2DModel/Services/Implementations/GeometryRuleEngine.cs`)
- Maintains a sorted list of rules by priority
- Ensures rules execute in correct order
- Can be enabled/disabled globally

## Built-in Rules

### 1. MinimumVertexCountRule (Priority: 10)
**Location**: `src/CAD2DModel/Services/Implementations/Rules/MinimumVertexCountRule.cs`
- **Purpose**: Remove invalid entities that don't have enough vertices
- **Logic**: 
  - Polylines must have at least 2 vertices
  - Boundaries must have at least 3 vertices
  - Removes invalid entities from the model

### 2. RemoveDuplicateVerticesRule (Priority: 50)
**Location**: `src/CAD2DModel/Services/Implementations/Rules/RemoveDuplicateVerticesRule.cs`
- **Purpose**: Clean up duplicate consecutive vertices
- **Configuration**: Tolerance (default: 0.0001)
- **Logic**:
  - Checks consecutive vertices using distance tolerance
  - For boundaries, checks wrap-around (last to first)
  - Maintains minimum vertex count

### 3. MinimumSegmentLengthRule (Priority: 100)
**Location**: `src/CAD2DModel/Services/Implementations/Rules/MinimumSegmentLengthRule.cs`
- **Purpose**: Remove segments that are too short
- **Configuration**: Minimum length (default: 0.001)
- **Logic**:
  - Checks each segment length
  - Removes vertices that create segments below threshold
  - For boundaries, maintains minimum of 3 vertices

### 4. CounterClockwiseWindingRule (Priority: 200)
**Location**: `src/CAD2DModel/Services/Implementations/Rules/CounterClockwiseWindingRule.cs`
- **Purpose**: Ensure boundaries have counter-clockwise winding order
- **Importance**: Critical for boundary element analysis where normal direction matters
- **Logic**:
  - Uses shoelace formula to calculate signed area
  - Reverses vertex order if clockwise

## Configuration

Rules are automatically registered during DI configuration in `ServiceConfiguration.AddCAD2DServices()`:

```csharp
services.AddSingleton<IGeometryRuleEngine>(sp => 
{
    var engine = new GeometryRuleEngine();
    
    // Register default rules (sorted by priority)
    engine.RegisterRule(new MinimumVertexCountRule());
    engine.RegisterRule(new RemoveDuplicateVerticesRule(tolerance: 0.0001));
    engine.RegisterRule(new MinimumSegmentLengthRule(minimumLength: 0.001));
    engine.RegisterRule(new CounterClockwiseWindingRule());
    
    return engine;
});
```

## Usage

### Apply Rules to a Single Entity

```csharp
// After modifying an entity
var ruleEngine = serviceProvider.GetRequiredService<IGeometryRuleEngine>();
var geometryModel = serviceProvider.GetRequiredService<IGeometryModel>();

ruleEngine.ApplyRules(entity, geometryModel);
```

### Apply Rules to All Entities

```csharp
// After loading a model or batch operations
ruleEngine.ApplyAllRules(geometryModel);
```

### Disable Rules Temporarily

```csharp
ruleEngine.Enabled = false;
// Perform operations without rule enforcement
ruleEngine.Enabled = true;
```

## Creating Custom Rules

To create a new geometry rule:

1. Create a class implementing `IGeometryRule`
2. Set an appropriate priority (consider existing rule priorities)
3. Implement `AppliesTo()` to filter entities
4. Implement `Apply()` with the rule logic
5. Register the rule in `ServiceConfiguration`

### Example: Snap to Grid Rule

```csharp
public class SnapToGridRule : IGeometryRule
{
    private readonly double _gridSize;
    
    public string Name => "Snap to Grid";
    public int Priority => 150; // After cleanup, before winding
    
    public SnapToGridRule(double gridSize = 1.0)
    {
        _gridSize = gridSize;
    }
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Polyline || entity is Boundary;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        if (entity is Polyline polyline)
        {
            foreach (var vertex in polyline.Vertices)
            {
                vertex.Location = new Point2D(
                    Math.Round(vertex.Location.X / _gridSize) * _gridSize,
                    Math.Round(vertex.Location.Y / _gridSize) * _gridSize
                );
            }
        }
        // Similar for Boundary
    }
}
```

## Integration Points

### When to Apply Rules

Rules should be applied:
- **After entity creation** - Ensure new entities are valid
- **After vertex manipulation** - Move, add, delete operations
- **After import** - DXF/DWG files may contain invalid geometry
- **Before analysis** - Ensure geometry meets BEM requirements
- **After undo/redo** - Maintain consistency

### Commands Integration

Commands that modify geometry should apply rules:

```csharp
public override void Execute()
{
    // Modify geometry
    // ...
    
    // Apply rules if needed
    _ruleEngine.ApplyRules(_entity, _geometryModel);
}
```

## Performance Considerations

- Rules execute in priority order on each entity
- Use `AppliesTo()` efficiently to filter entities quickly
- For batch operations, use `ApplyAllRules()` once instead of repeatedly calling `ApplyRules()`
- Consider disabling rules during intensive operations and applying once at the end
- Rules can remove entities from the model, so iterate with `.ToList()` to avoid modification during iteration

## Future Enhancements

Potential future rules:
- **Self-intersection detection** - Detect and optionally fix self-intersecting boundaries
- **Coincident boundaries** - Detect overlapping boundaries
- **Minimum angle rule** - Ensure vertices don't create sharp angles
- **Arc simplification** - Replace near-linear segments with single segment
- **Boundary orientation** - Ensure holes are clockwise if outer boundary is CCW
