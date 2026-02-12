using Examine2DModel.Analysis;
using Examine2DModel.Materials;
using Examine2DModel.Strength;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CAD2DViewModels.ViewModels;

/// <summary>
/// ViewModel for analysis settings and execution
/// </summary>
public partial class AnalysisViewModel : ObservableObject
{
    private readonly IBoundaryElementSolver _solver;
    
    [ObservableProperty]
    private PlaneStrainType _planeStrainType = PlaneStrainType.PlaneStrain;
    
    [ObservableProperty]
    private ElementType _elementType = ElementType.Linear;
    
    [ObservableProperty]
    private int _numberOfElements = 100;
    
    [ObservableProperty]
    private bool _isAnalyzing;
    
    [ObservableProperty]
    private double _analysisProgress;
    
    [ObservableProperty]
    private string _analysisStatus = "Ready";
    
    public AnalysisViewModel(IBoundaryElementSolver solver)
    {
        _solver = solver ?? throw new ArgumentNullException(nameof(solver));
    }
    
    [RelayCommand]
    private async Task RunAnalysis()
    {
        IsAnalyzing = true;
        AnalysisProgress = 0;
        AnalysisStatus = "Initializing...";
        
        try
        {
            var options = new SolverOptions
            {
                PlaneStrainType = PlaneStrainType,
                ElementType = ElementType,
                NumberOfElements = NumberOfElements
            };
            
            var config = new BoundaryConfiguration
            {
                // TODO: Populate from model
            };
            
            AnalysisStatus = "Running boundary element analysis...";
            AnalysisProgress = 50;
            
            var result = await _solver.SolveAsync(config, options);
            
            AnalysisProgress = 100;
            AnalysisStatus = "Analysis complete";
        }
        catch (Exception ex)
        {
            AnalysisStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
    
    [RelayCommand]
    private void CancelAnalysis()
    {
        // TODO: Implement cancellation
        IsAnalyzing = false;
        AnalysisStatus = "Cancelled";
    }
}
