# Auto External Boundary Feature

## Overview
Added a new "Auto External Boundary" feature that automatically creates an external boundary around existing geometry with configurable expansion options.

## What Was Added

### 1. Dialog Files
- **`src/Examine2DView/Dialogs/AutoExternalBoundaryDialog.xaml`**
  - WPF dialog for configuring auto external boundary options
  - Contains sliders for:
    - Expansion Factor (1.1x to 5.0x, default 2.0x)
    - Minimum Margin (0 to 50m, default 5m)
  - Real-time value display for both parameters

- **`src/Examine2DView/Dialogs/AutoExternalBoundaryDialog.xaml.cs`**
  - Code-behind with properties to expose user selections
  - Standard OK/Cancel dialog pattern

### 2. Menu Integration
- **`src/Examine2DView/MainWindow.xaml`**
  - Added "Create _Auto External Boundary..." menu item under Model menu
  - Added "Auto External..." toolbar button for quick access

### 3. Implementation
- **`src/Examine2DView/MainWindow.xaml.cs`**
  - Added `AutoExternalBoundaryCommand` property
  - Implemented `CreateAutoExternalBoundary()` method with:
    - Validation to check for existing geometry
    - Dialog display for user configuration
    - Extent calculation from all non-external boundaries
    - Smart margin calculation (uses larger of expansion factor or minimum margin)
    - Automatic removal of existing external boundary (with confirmation)
    - External boundary creation with adaptive mesh resolution
    - Command system integration for undo/redo support
    - Success message with boundary details

## How It Works

1. **User Action**: Select "Model → Create Auto External Boundary..." or click "Auto External..." toolbar button

2. **Validation**: 
   - Checks if any boundaries exist
   - If external boundary exists, prompts to replace

3. **Configuration Dialog**:
   - User adjusts expansion factor (how many times larger than existing geometry)
   - User adjusts minimum margin (absolute margin in model units)
   - The larger of the two values is used for each axis

4. **Boundary Creation**:
   - Calculates bounds of all existing (non-external) boundaries
   - Applies margins based on user settings
   - Creates rectangular external boundary
   - Sets adaptive mesh resolution (1/50th of smaller dimension)
   - Integrates with command system for undo/redo

5. **Feedback**:
   - Success message shows:
     - Expansion factor used
     - Margins applied (X and Y)
     - Final boundary size

## Usage Example

If you have a circular excavation of 10m diameter:
- Existing bounds: ~10m × 10m
- With 2.0x expansion factor: External boundary = 20m × 20m (5m margin on each side)
- With 5m minimum margin: Ensures at least 5m space around geometry
- Final margin uses whichever is larger

## Technical Details

- Uses existing `ExternalBoundary` class from `CAD2DModel.Geometry`
- Leverages `Bounds.Union()` for multi-boundary extent calculation
- Integrates with `AddEntityCommand` for undo/redo support
- Follows MVVM pattern (minimal code-behind, logic in command methods)
- Consistent with existing dialog patterns in the codebase

## Testing

Build status: ✅ Successfully builds with no errors

To test:
1. Create some boundaries in the model
2. Select "Model → Create Auto External Boundary..."
3. Adjust sliders to desired settings
4. Click OK
5. Verify external boundary is created around your geometry

## Files Changed/Added

### New Files:
- `src/Examine2DView/Dialogs/AutoExternalBoundaryDialog.xaml`
- `src/Examine2DView/Dialogs/AutoExternalBoundaryDialog.xaml.cs`

### Modified Files:
- `src/Examine2DView/MainWindow.xaml` (menu and toolbar)
- `src/Examine2DView/MainWindow.xaml.cs` (command and implementation)
