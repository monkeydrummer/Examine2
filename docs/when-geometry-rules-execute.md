# When Geometry Rules Execute (Updated)

## Philosophy

Geometry rules are **ONLY applied explicitly** when:
1. An entity is finished being created interactively
2. After loading entities from a file
3. After specific geometry modifications (vertex moves, etc.)
4. When manually requested

Rules are **NEVER applied automatically** when adding entities to the model.

## When Rules Execute

### 1. After Interactive Creation
**When:** User finishes creating a boundary or polyline

**Where:** `FinishBoundary()` or `FinishPolyline()` in creation modes

```csharp
// In AddBoundaryMode.FinishBoundary()
_currentBoundary = new Boundary { ... };
_geometryModel.AddEntity(_currentBoundary);  // NO rules applied

// User clicks points...

// When user presses Enter or right-click->Finish:
_geometryModel.ApplyRulesToEntity(_currentBoundary);  // NOW rules apply!
```

### 2. After Vertex Modifications
**When:** Moving vertices via `MoveVertexCommand`

**Where:** `MoveVertexCommand.Execute()` and `Undo()`

```csharp
var cmd = new MoveVertexCommand(vertex, newLocation, parentEntity, model);
cmd.Execute();  // Applies rules to parentEntity
```

### 3. After Loading from File
**When:** Loading a model from DXF, JSON, or other format

```csharp
// Load all entities first
foreach (var entity in loadedEntities)
{
    model.AddEntity(entity);  // NO rules applied
}

// Apply rules once to all entities
model.ApplyAllRules();  // Apply all rules to entire model
```

### 4. Manual Application
**When:** You explicitly need to revalidate geometry

```csharp
// Apply rules to a specific entity
model.ApplyRulesToEntity(entity);

// Apply rules to all entities
model.ApplyAllRules();
```

## Rule Execution Order

Rules execute in priority order (lower numbers first):

1. **Priority 10**: `MinimumVertexCountRule` - Removes invalid entities
2. **Priority 50**: `RemoveDuplicateVerticesRule` - Cleans duplicate vertices
3. **Priority 100**: `MinimumSegmentLengthRule` - Removes short segments
4. **Priority 150**: `BoundaryIntersectionRule` - Adds intersection vertices
5. **Priority 200**: `CounterClockwiseWindingRule` - Ensures CCW winding

## Boundary Intersection Example

For boundaries to intersect and have vertices added at crossing points:

```csharp
// Create first boundary
var boundary1 = new Boundary { Intersectable = true };
boundary1.Vertices.Add(new Vertex(new Point2D(0, 0.5)));
boundary1.Vertices.Add(new Vertex(new Point2D(2, 0.5)));
boundary1.Vertices.Add(new Vertex(new Point2D(2, 1.5)));
boundary1.Vertices.Add(new Vertex(new Point2D(0, 1.5)));
model.AddEntity(boundary1);
model.ApplyRulesToEntity(boundary1);  // Apply rules

// Create second boundary
var boundary2 = new Boundary { Intersectable = true };
boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 0)));
boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 0)));
boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 2)));
boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 2)));
model.AddEntity(boundary2);
model.ApplyRulesToEntity(boundary2);  // Now intersections are detected!

// Both boundaries now have vertices at intersection points
```

## Why This Approach?

### Advantages:
- ✅ **Explicit control** - You decide when rules run
- ✅ **No surprises** - Rules don't interfere with construction
- ✅ **Better performance** - Rules only run when needed
- ✅ **Cleaner code** - No "under construction" flags needed
- ✅ **Flexible** - Easy to batch operations and apply rules once

### Where Rules Are Applied:
1. `AddBoundaryMode.FinishBoundary()` - After creating a boundary
2. `AddPolylineMode.FinishPolyline()` - After creating a polyline
3. `MoveVertexCommand.Execute()/Undo()` - After moving vertices
4. File loading code - After loading all entities
5. Anywhere you call `model.ApplyRulesToEntity()` or `model.ApplyAllRules()`

## Performance Tips

For batch operations:
```csharp
// Option 1: Disable rules temporarily
ruleEngine.Enabled = false;
// ... add many entities ...
ruleEngine.Enabled = true;
model.ApplyAllRules();

// Option 2: Just add first, apply once
foreach (var entity in entities)
{
    model.AddEntity(entity);
}
model.ApplyAllRules();  // Apply once to all
```

## Future Commands to Update

Consider applying rules in:
- `TrimCommand` - after trimming entities
- `ExtendCommand` - after extending entities
- `AddVertexCommand` - after adding a vertex
- `RemoveVertexCommand` - after removing a vertex
- Any command that modifies geometry
