# How to Use Intersectable Boundaries

## Overview

By default, boundaries in the system have `Intersectable = false`. This means they won't be checked for intersections with other boundaries. To enable automatic vertex insertion at intersection points, you need to mark boundaries as intersectable.

## Making Boundaries Intersectable

### Method 1: During Creation (Context Menu)

When creating a boundary using `AddBoundaryMode`:

1. Right-click to open the context menu
2. Click **"Make Intersectable"** (shows checkmark when enabled)
3. Continue creating the boundary
4. When finished, the boundary will be created with `Intersectable = true`

### Method 2: After Creation (Context Menu)

To make existing boundaries intersectable:

1. Enter **Select Mode** (toolbar or menu)
2. **Select one or more boundaries** (Ctrl+Click for multiple)
3. **Right-click** to open context menu
4. Click **"Make Intersectable"** to toggle
   - If checked: boundaries become non-intersectable
   - If unchecked: boundaries become intersectable
5. Rules automatically re-run to detect intersections

### Method 3: Programmatically

```csharp
var boundary = new Boundary { Intersectable = true };
// ... add vertices ...
model.AddEntity(boundary);
model.ApplyRulesToEntity(boundary);  // Detects intersections
```

## How Intersection Detection Works

### When Rules Run

1. **After finishing boundary creation**: `ApplyRulesToEntity()` is called
2. **After toggling Intersectable**: `ApplyAllRules()` is called to detect new intersections
3. **After moving vertices**: Rules re-run if entity/model provided to `MoveVertexCommand`

### What Happens

The `BoundaryIntersectionRule` (Priority 150):

1. Finds all other boundaries where `Intersectable = true`
2. For each pair of segments that cross:
   - Calculates the exact intersection point
   - Inserts a new vertex at that location
   - Skips intersections at endpoints (boundaries already share a vertex)
   - Skips duplicate vertices (if intersection already exists)

### Example Workflow

```csharp
// Create first excavation boundary
var excavation = new Boundary { Intersectable = true, Name = "Main Tunnel" };
excavation.Vertices.Add(new Vertex(new Point2D(0, 0)));
excavation.Vertices.Add(new Vertex(new Point2D(10, 0)));
excavation.Vertices.Add(new Vertex(new Point2D(10, 5)));
excavation.Vertices.Add(new Vertex(new Point2D(0, 5)));
model.AddEntity(excavation);
model.ApplyRulesToEntity(excavation);

// Create second excavation boundary that crosses the first
var shaft = new Boundary { Intersectable = true, Name = "Ventilation Shaft" };
shaft.Vertices.Add(new Vertex(new Point2D(5, -2)));
shaft.Vertices.Add(new Vertex(new Point2D(7, -2)));
shaft.Vertices.Add(new Vertex(new Point2D(7, 7)));
shaft.Vertices.Add(new Vertex(new Point2D(5, 7)));
model.AddEntity(shaft);
model.ApplyRulesToEntity(shaft);  // Intersections detected!

// Both boundaries now have vertices at (5,0), (7,0), (5,5), and (7,5)
```

## Use Cases

### When to Use Intersectable Boundaries

**Use `Intersectable = true` for:**
- Excavation boundaries that represent connected openings
- Boundaries that will be meshed for BEM analysis
- Tunnel networks where proper connectivity is required
- Any boundaries where intersections matter for analysis

**Use `Intersectable = false` (default) for:**
- Reference lines or construction geometry
- Boundaries that represent separate, non-connected features
- Temporary or guide boundaries
- Boundaries where performance is critical (intersection checking is expensive)

### Performance Considerations

- Intersection detection is O(n²) for segment pairs
- Only intersectable boundaries are checked
- Intersections only calculated when rules are applied
- For many boundaries, consider applying rules in batches

## Visualizing Intersectable Boundaries

You might want to render intersectable boundaries differently:

```csharp
// In your render code
if (boundary.Intersectable)
{
    // Draw with special color/style
    context.DrawLine(..., color: "blue", width: 3);
}
```

## Integration with BEM Analysis

For boundary element analysis, intersectable boundaries ensure:
- Proper mesh connectivity at intersections
- Correct node placement where boundaries meet
- Valid boundary element discretization
- Accurate stress analysis at tunnel intersections

The counter-clockwise winding rule (Priority 200) ensures the normal vectors point in the correct direction for analysis.
