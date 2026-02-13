using CAD2DModel.Geometry;
using CAD2DModel.Results;

namespace CAD2DModel.Services;

/// <summary>
/// Service for generating and managing contour data
/// </summary>
public interface IContourService
{
    /// <summary>
    /// Current contour settings
    /// </summary>
    ContourSettings Settings { get; }
    
    /// <summary>
    /// Generate contour data for a specific result field within the external boundary
    /// </summary>
    /// <param name="externalBoundary">The boundary defining the analysis region</param>
    /// <param name="excavations">Interior boundaries (excavations)</param>
    /// <param name="field">The result field to compute</param>
    /// <returns>Contour data ready for visualization</returns>
    ContourData GenerateContours(ExternalBoundary externalBoundary, IEnumerable<Boundary> excavations, ResultField field);
    
    /// <summary>
    /// Get the current cached contour data
    /// </summary>
    ContourData? CurrentContourData { get; }
    
    /// <summary>
    /// Invalidate cached contours (call when geometry changes)
    /// </summary>
    void InvalidateContours();
    
    /// <summary>
    /// Event raised when contour data has been regenerated
    /// </summary>
    event EventHandler? ContoursUpdated;
}
