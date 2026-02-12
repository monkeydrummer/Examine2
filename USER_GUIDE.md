# Examine2D - User Guide

## Quick Start

### Running the Application

```bash
dotnet run --project src/Examine2DView/Examine2DView.csproj
```

The application will open showing:
- A circular excavation (green polygon)
- A query line (blue polyline)
- An external bounding box (green rectangle)
- A grid with origin axes

---

## Mouse Controls

### Navigation (Works in ALL modes)

| Action | Result |
|--------|--------|
| **Mouse Wheel Up** | Zoom in (towards cursor) |
| **Mouse Wheel Down** | Zoom out (from cursor) |
| **Middle Mouse + Drag** | Pan viewport |

💡 **Tip**: Zoom is cursor-centric - the point under your cursor stays fixed while zooming!

### Selection (Idle Mode)

| Action | Result |
|--------|--------|
| **Click on entity** | Select single entity |
| **Ctrl + Click** | Add/remove from selection |
| **Click + Drag (left→right)** | Window selection (fully contained) |
| **Click + Drag (right→left)** | Crossing selection (any intersection) |
| **Click on empty space** | Clear selection |

💡 **Tip**: Drag direction matters! Left-to-right is stricter (window), right-to-left is more forgiving (crossing).

---

## Keyboard Shortcuts

### Selection

| Key | Action |
|-----|--------|
| **Delete** | Delete selected entities |
| **Escape** | Clear selection & return to Idle |

### Navigation

| Key | Action |
|-----|--------|
| **Ctrl + Z** | Undo last command |
| **Ctrl + Y** | Redo last undone command |

### Modifier Keys

| Key | Effect |
|-----|--------|
| **Ctrl** | Add to selection (multi-select) |
| **Shift** | Reserved for ortho snap |
| **Alt** | Reserved for alternate actions |

---

## Visual Indicators

### Entity Colors

- **Green** - Boundaries (excavations, external box)
- **Blue** - Polylines (query lines, temporary geometry)
- **Light Green Fill** - Boundary interior (semi-transparent)

### Selection Indicators

- **Red circles** - Selected vertices (larger than normal)
- **Normal circles** - Unselected vertices

### Grid

- **Light gray lines** - Grid lines (adjusts to zoom)
- **Darker gray lines** - Origin axes (X and Y at 0,0)

---

## Status Bar

Bottom of the window shows:

1. **Status Text** - Current mode prompt (e.g., "Select entities or drag selection box")
2. **Coordinates** - Mouse position in world coordinates
3. **Scale** - Current zoom level

---

## Interaction Modes

The application uses a modal interaction system. The current mode determines how mouse/keyboard input is handled.

### IdleMode (Default)

**Purpose**: Selection and basic navigation

**Available Actions**:
- Click to select entities
- Drag selection boxes
- Multi-select with Ctrl
- Delete entities
- Right-click for context menu (future)

**Status Prompt**: "Select entities or drag selection box"

### AddBoundaryMode (Future)

**Purpose**: Create new boundary entities

**Planned Actions**:
- Click to add vertices
- Snap to grid/vertices
- Right-click or Escape to finish

### MoveVertexMode (Future)

**Purpose**: Edit existing geometry

**Planned Actions**:
- Click vertex to select
- Drag to new position
- Snap to grid/other vertices
- Right-click or Escape to finish

---

## Sample Scene

The application starts with three entities:

### 1. Circular Excavation
- **Type**: Boundary (closed polygon)
- **Color**: Green
- **Size**: Radius ≈ 5 units
- **Vertices**: 16 points
- **Purpose**: Represents an underground opening

### 2. Query Line
- **Type**: Polyline (open)
- **Color**: Blue
- **Path**: Zigzag pattern through excavation
- **Purpose**: For sampling results along a path

### 3. External Bounding Box
- **Type**: Boundary (rectangle)
- **Color**: Green
- **Size**: 30×30 units
- **Purpose**: Defines analysis region boundary

---

## Tips & Tricks

### Zoom to Fit

Coming soon! Will auto-zoom to show all geometry.

### Selection Best Practices

1. **For precise selection**: Click directly on the entity
2. **For area selection**: Use window selection (left→right)
3. **For quick grab**: Use crossing selection (right→left)
4. **For adding more**: Hold Ctrl while clicking

### Smooth Panning

1. Click middle mouse button
2. Drag (no need to hold any modifier keys)
3. Release when done
4. Works from any mode!

### Grid Reference

The grid spacing adapts automatically:
- **Zoomed out**: Wider spacing
- **Zoomed in**: Tighter spacing
- **Origin (0,0)**: Marked by darker axes

Use the grid to estimate distances and maintain orthogonal geometry.

---

## Troubleshooting

### Nothing Happens When I Click

**Possible Causes**:
- You might be clicking on empty space
- Entity might be outside the visible area

**Solutions**:
- Try zooming out with mouse wheel
- Look for green/blue geometry
- Pan around with middle mouse

### Selection Box Doesn't Appear

**Possible Causes**:
- You might be clicking on an entity (which selects it immediately)
- Middle mouse is being used (which pans instead)

**Solutions**:
- Click on empty space and drag with LEFT mouse button
- Make sure you're not accidentally holding middle button

### Can't Deselect

**Solutions**:
- Press **Escape** to clear all selections
- Click on empty space (not on any entity)

---

## What's Coming Next

### Phase 3: Advanced Interaction
- [ ] Interactive boundary creation
- [ ] Vertex editing with snapping
- [ ] Geometry transformation tools
- [ ] Copy/paste/duplicate

### Phase 4: Analysis Features
- [ ] Boundary element mesh generation
- [ ] Stress field calculation
- [ ] Contour visualization
- [ ] Real-time result updates

### Phase 5: Productivity
- [ ] Annotation tools
- [ ] Measurement tools
- [ ] Layer management
- [ ] DXF import/export

---

## Support

For issues, questions, or feature requests:
- Check `CANVAS_INTEGRATION_COMPLETE.md` for technical details
- Review `PHASE2_COMPLETE.md` for architecture overview
- See `IMPLEMENTATION_STATUS.md` for project status

---

## Controls Reference Card

```
┌────────────────────────────────────────┐
│         EXAMINE2D CONTROLS             │
├────────────────────────────────────────┤
│  NAVIGATION                            │
│    Mouse Wheel     Zoom                │
│    Middle + Drag   Pan                 │
│                                        │
│  SELECTION                             │
│    Click           Select              │
│    Ctrl + Click    Multi-select        │
│    Drag (L→R)      Window selection    │
│    Drag (R→L)      Crossing selection  │
│                                        │
│  EDITING                               │
│    Delete          Remove entity       │
│    Escape          Clear / Cancel      │
│    Ctrl + Z        Undo                │
│    Ctrl + Y        Redo                │
└────────────────────────────────────────┘
```

---

**Version**: Canvas Integration Complete  
**Date**: February 2026  
**Status**: Fully Functional ✅
