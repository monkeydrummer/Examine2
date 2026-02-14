# Future Enhancements

This document tracks potential improvements and features for future implementation.

## High Priority

### Snapshot-Based Undo/Redo System

**Current Limitation**: The command-based undo/redo system doesn't capture changes made by geometry rules to other entities. For example, when adding a new boundary that intersects existing boundaries, the intersection vertices added to the existing boundaries are not tracked by the undo system.

**Proposed Solution**: Implement a snapshot-based undo/redo system that captures the entire model state before and after each operation.

**Benefits**:
- Automatically captures ALL state changes, including side effects from geometry rules
- Simpler to implement and maintain than tracking individual changes
- More reliable for complex operations
- No need to manually track cascading changes

**Implementation Approach**:
1. Before each operation: Serialize and save a snapshot of the entire geometry model
2. On Undo: Deserialize and restore the previous snapshot
3. On Redo: Deserialize and restore the next snapshot
4. Maintain a circular buffer of snapshots with configurable size limit
5. Use efficient binary serialization to minimize memory usage
6. Consider incremental snapshots (only save changed entities) for large models

**Trade-offs**:
- Increased memory usage (storing multiple model states)
- Potentially slower for very large models
- Requires efficient serialization/deserialization implementation
- May need compression for large models

**Priority**: High - This would significantly improve the user experience when working with intersectable boundaries and other geometry rule features.

**Status**: Documented for future implementation

---

## Medium Priority

*Additional enhancements can be added here as they are identified*

---

## Low Priority

*Additional enhancements can be added here as they are identified*

---

## Completed Enhancements

### Polyline Drawing Mode Refactoring ✅
- **Date Completed**: 2026-02-14
- **Description**: Extracted common line drawing logic into `PolylineDrawingModeBase` to eliminate ~1000 lines of duplicated code
- **Benefits**: Arc/circle drawing now available in all polyline-based modes, keyboard shortcuts (U, A, C, L), consistent behavior
- **Files**: `PolylineDrawingModeBase.cs`, `AddBoundaryMode.cs`, `AddPolylineMode.cs`, `AddPolylineAnnotationMode.cs`
