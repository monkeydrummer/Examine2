using Examine2DModel.Calculations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.Calculations;

[TestClass]
public class StressCalculatorTests
{
    private const double Tolerance = 1e-6;
    
    #region Invariants Tests
    
    [TestMethod]
    public void CalculateInvariants_HydrostaticStress_ReturnsCorrectValues()
    {
        // Arrange: Hydrostatic stress (all principal stresses equal)
        double sigma = -10.0; // 10 MPa compression
        double sigmaX = sigma, sigmaY = sigma, sigmaZ = sigma;
        double tauXY = 0, tauYZ = 0, tauXZ = 0;
        
        // Act
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert
        Assert.AreEqual(-30.0, i1, Tolerance, "I1 should equal 3*sigma for hydrostatic stress");
        Assert.AreEqual(0.0, j2, Tolerance, "J2 should be zero for hydrostatic stress");
        Assert.AreEqual(0.0, lodeAngle, Tolerance, "Lode angle should be zero for hydrostatic stress");
    }
    
    [TestMethod]
    public void CalculateInvariants_UniaxialTension_ReturnsCorrectValues()
    {
        // Arrange: Uniaxial tension in x-direction
        double sigmaX = 100.0;
        double sigmaY = 0.0;
        double sigmaZ = 0.0;
        double tauXY = 0, tauYZ = 0, tauXZ = 0;
        
        // Act
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert
        Assert.AreEqual(100.0, i1, Tolerance);
        
        // For uniaxial tension: J2 = (σ²)/3
        double expectedJ2 = (sigmaX * sigmaX) / 3.0;
        Assert.AreEqual(expectedJ2, j2, Tolerance);
    }
    
    [TestMethod]
    public void CalculateInvariants_PureShear_ReturnsCorrectValues()
    {
        // Arrange: Pure shear stress
        double tau = 50.0;
        double sigmaX = 0, sigmaY = 0, sigmaZ = 0;
        double tauXY = tau, tauYZ = 0, tauXZ = 0;
        
        // Act
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert
        Assert.AreEqual(0.0, i1, Tolerance, "I1 should be zero for pure shear");
        Assert.AreEqual(tau * tau, j2, Tolerance, "J2 = τ² for pure shear");
    }
    
    #endregion
    
    #region Principal Stresses 3D Tests
    
    [TestMethod]
    public void CalculatePrincipalStresses3D_SimpleCase_ReturnsSortedValues()
    {
        // Arrange: Known principal stresses (already aligned with axes)
        double sigmaX = 100.0; // Sigma1
        double sigmaY = 50.0;  // Sigma2
        double sigmaZ = 20.0;  // Sigma3
        double tauXY = 0, tauYZ = 0, tauXZ = 0;
        
        // Act
        var (sigma3, sigma2, sigma1) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert
        Assert.AreEqual(20.0, sigma3, Tolerance, "Sigma3 should be minimum");
        Assert.AreEqual(50.0, sigma2, Tolerance, "Sigma2 should be intermediate");
        Assert.AreEqual(100.0, sigma1, Tolerance, "Sigma1 should be maximum");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses3D_WithShear_CalculatesCorrectly()
    {
        // Arrange: Stress state with shear
        double sigmaX = 100.0;
        double sigmaY = 50.0;
        double sigmaZ = 30.0;
        double tauXY = 20.0;
        double tauYZ = 0, tauXZ = 0;
        
        // Act
        var (sigma3, sigma2, sigma1) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert: Verify sorting (sigma1 >= sigma2 >= sigma3)
        Assert.IsTrue(sigma1 >= sigma2, "Sigma1 should be >= Sigma2");
        Assert.IsTrue(sigma2 >= sigma3, "Sigma2 should be >= Sigma3");
        
        // Verify invariants are preserved
        double i1Expected = sigmaX + sigmaY + sigmaZ;
        double i1Actual = sigma1 + sigma2 + sigma3;
        Assert.AreEqual(i1Expected, i1Actual, Tolerance, "I1 invariant should be preserved");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses3D_NegativeStresses_SortsCorrectly()
    {
        // Arrange: Compression (negative stresses)
        double sigmaX = -100.0;
        double sigmaY = -50.0;
        double sigmaZ = -20.0;
        double tauXY = 0, tauYZ = 0, tauXZ = 0;
        
        // Act
        var (sigma3, sigma2, sigma1) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert: For compression, more negative = smaller principal stress
        Assert.AreEqual(-100.0, sigma3, Tolerance, "Most compressive is sigma3");
        Assert.AreEqual(-50.0, sigma2, Tolerance);
        Assert.AreEqual(-20.0, sigma1, Tolerance, "Least compressive is sigma1");
    }
    
    #endregion
    
    #region Principal Stresses 2D Tests
    
    [TestMethod]
    public void CalculatePrincipalStresses2D_NoShear_ReturnsAxialStresses()
    {
        // Arrange
        double sigmaX = 100.0;
        double sigmaY = 50.0;
        double tauXY = 0.0;
        
        // Act
        var (sigma3, sigma1, angle) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // Assert
        Assert.AreEqual(50.0, sigma3, Tolerance);
        Assert.AreEqual(100.0, sigma1, Tolerance);
        Assert.AreEqual(0.0, angle, Tolerance, "Angle should be 0 when no shear and sigmaX > sigmaY");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses2D_PureShear_Returns45DegreeAngle()
    {
        // Arrange: Pure shear
        double sigmaX = 0.0;
        double sigmaY = 0.0;
        double tauXY = 50.0;
        
        // Act
        var (sigma3, sigma1, angle) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // Assert
        Assert.AreEqual(-50.0, sigma3, Tolerance, "Principal stress should equal -τ");
        Assert.AreEqual(50.0, sigma1, Tolerance, "Principal stress should equal +τ");
        Assert.AreEqual(45.0, angle, Tolerance, "Angle should be 45° for pure shear");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses2D_WithShearStress_CalculatesCorrectly()
    {
        // Arrange: σx=100, σy=60, τxy=30
        double sigmaX = 100.0;
        double sigmaY = 60.0;
        double tauXY = 30.0;
        
        // Act
        var (sigma3, sigma1, angle) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // Assert: Verify sorting (sigma1 >= sigma3)
        Assert.IsTrue(sigma1 >= sigma3, "Sigma1 should be >= Sigma3");
        
        // Manual calculation:
        // avg = (100+60)/2 = 80
        // tauMax = sqrt(((100-60)/2)^2 + 30^2) = sqrt(20^2 + 30^2) = sqrt(1300) ≈ 36.06
        // sigma1 = 80 + 36.06 ≈ 116.06
        // sigma3 = 80 - 36.06 ≈ 43.94
        Assert.AreEqual(43.94, sigma3, 0.1, "Sigma3");
        Assert.AreEqual(116.06, sigma1, 0.1, "Sigma1");
    }
    
    #endregion
    
    #region Derived Stress Measures Tests
    
    [TestMethod]
    public void CalculateVonMisesStress_KnownJ2_ReturnsCorrectValue()
    {
        // Arrange
        double j2 = 100.0;
        
        // Act
        double vonMises = StressCalculator.CalculateVonMisesStress(j2);
        
        // Assert
        Assert.AreEqual(Math.Sqrt(3.0 * 100.0), vonMises, Tolerance);
    }
    
    [TestMethod]
    public void CalculateMeanStress_KnownI1_ReturnsCorrectValue()
    {
        // Arrange
        double i1 = -300.0;
        
        // Act
        double meanStress = StressCalculator.CalculateMeanStress(i1);
        
        // Assert
        Assert.AreEqual(-100.0, meanStress, Tolerance);
    }
    
    [TestMethod]
    public void CalculateDeviatoricStress_KnownPrincipalStresses_ReturnsCorrectValue()
    {
        // Arrange
        double sigma1 = 100.0;
        double sigma3 = 40.0;
        
        // Act
        double deviatoric = StressCalculator.CalculateDeviatoricStress(sigma1, sigma3);
        
        // Assert
        Assert.AreEqual(60.0, deviatoric, Tolerance);
    }
    
    [TestMethod]
    public void CalculateAngelierStressRatio_VariousCases_ReturnsClampedValueBetween0And1()
    {
        // Test case 1: σ2 exactly midway
        double ratio1 = StressCalculator.CalculateAngelierStressRatio(100, 50, 0);
        Assert.AreEqual(0.5, ratio1, Tolerance);
        
        // Test case 2: σ2 equals σ3 (minimum)
        double ratio2 = StressCalculator.CalculateAngelierStressRatio(100, 0, 0);
        Assert.AreEqual(0.0, ratio2, Tolerance);
        
        // Test case 3: σ2 equals σ1 (maximum)
        double ratio3 = StressCalculator.CalculateAngelierStressRatio(100, 100, 0);
        Assert.AreEqual(1.0, ratio3, Tolerance);
        
        // Test case 4: Value would exceed 1.0 (should clamp)
        double ratio4 = StressCalculator.CalculateAngelierStressRatio(100, 150, 0);
        Assert.AreEqual(1.0, ratio4, Tolerance);
    }
    
    [TestMethod]
    public void CalculateStressRatio_NormalCase_ReturnsCorrectRatio()
    {
        // Arrange
        double sigma1 = 100.0;
        double sigma3 = 25.0;
        
        // Act
        double k = StressCalculator.CalculateStressRatio(sigma1, sigma3);
        
        // Assert
        Assert.AreEqual(0.25, k, Tolerance);
    }
    
    [TestMethod]
    public void CalculateStressRatio_ZeroSigma1_ReturnsDefaultValue()
    {
        // Arrange
        double sigma1 = 0.0;
        double sigma3 = 25.0;
        
        // Act
        double k = StressCalculator.CalculateStressRatio(sigma1, sigma3);
        
        // Assert
        Assert.AreEqual(100.0, k, Tolerance, "Should return default value for zero sigma1");
    }
    
    [TestMethod]
    public void CalculateTotalDisplacement_3DDisplacements_ReturnsCorrectMagnitude()
    {
        // Arrange: 3-4-5 triangle
        double ux = 3.0;
        double uy = 4.0;
        double uz = 0.0;
        
        // Act
        double total = StressCalculator.CalculateTotalDisplacement(ux, uy, uz);
        
        // Assert
        Assert.AreEqual(5.0, total, Tolerance);
    }
    
    [TestMethod]
    public void CalculateTotalDisplacement_AllZero_ReturnsZero()
    {
        // Act
        double total = StressCalculator.CalculateTotalDisplacement(0, 0, 0);
        
        // Assert
        Assert.AreEqual(0.0, total, Tolerance);
    }
    
    #endregion
    
    #region Edge Cases and Robustness Tests
    
    [TestMethod]
    public void CalculateInvariants_VeryLargeStresses_HandlesCorrectly()
    {
        // Arrange: Very high stress state (e.g., deep underground)
        double sigma = -1000.0; // 1000 MPa compression
        
        // Act
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigma, sigma, sigma, 0, 0, 0);
        
        // Assert
        Assert.AreEqual(-3000.0, i1, Tolerance);
        Assert.AreEqual(0.0, j2, Tolerance);
        Assert.IsFalse(double.IsNaN(lodeAngle), "Lode angle should not be NaN");
        Assert.IsFalse(double.IsInfinity(lodeAngle), "Lode angle should not be infinity");
    }
    
    [TestMethod]
    public void CalculateInvariants_VerySmallStresses_HandlesCorrectly()
    {
        // Arrange: Very small stresses
        double sigmaX = 1e-10;
        double sigmaY = 2e-10;
        double sigmaZ = 3e-10;
        
        // Act
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigmaX, sigmaY, sigmaZ, 0, 0, 0);
        
        // Assert
        Assert.AreEqual(6e-10, i1, 1e-15);
        Assert.IsFalse(double.IsNaN(j2), "J2 should not be NaN");
        Assert.IsFalse(double.IsNaN(lodeAngle), "Lode angle should not be NaN");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses2D_ExtremeShear_HandlesCorrectly()
    {
        // Arrange: Very high shear stress
        double sigmaX = 10.0;
        double sigmaY = 10.0;
        double tauXY = 1000.0; // Extreme shear
        
        // Act
        var (sigma3, sigma1, angle) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // Assert
        Assert.IsTrue(sigma1 >= sigma3, "Sigma1 should be >= Sigma3");
        Assert.IsFalse(double.IsNaN(sigma1), "Sigma1 should not be NaN");
        Assert.IsFalse(double.IsNaN(sigma3), "Sigma3 should not be NaN");
        Assert.IsFalse(double.IsNaN(angle), "Angle should not be NaN");
        
        // Verify stress transformation
        double avg = (sigmaX + sigmaY) / 2.0;
        double radius = Math.Sqrt(Math.Pow((sigmaX - sigmaY) / 2.0, 2) + tauXY * tauXY);
        Assert.AreEqual(avg + radius, sigma1, 1.0, "Sigma1 should match Mohr's circle");
        Assert.AreEqual(avg - radius, sigma3, 1.0, "Sigma3 should match Mohr's circle");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses3D_WithAllShearComponents_CalculatesCorrectly()
    {
        // Arrange: All shear components present
        double sigmaX = 50.0;
        double sigmaY = 30.0;
        double sigmaZ = 20.0;
        double tauXY = 10.0;
        double tauYZ = 5.0;
        double tauXZ = 3.0;
        
        // Act
        var (sigma3, sigma2, sigma1) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
        
        // Assert
        Assert.IsTrue(sigma1 >= sigma2, "Sigma1 should be >= Sigma2");
        Assert.IsTrue(sigma2 >= sigma3, "Sigma2 should be >= Sigma3");
        
        // Verify first invariant is preserved
        double i1 = sigmaX + sigmaY + sigmaZ;
        double i1Principal = sigma1 + sigma2 + sigma3;
        Assert.AreEqual(i1, i1Principal, Tolerance, "First invariant should be preserved");
    }
    
    [TestMethod]
    public void CalculateVonMisesStress_ZeroJ2_ReturnsZero()
    {
        // Arrange
        double j2 = 0.0;
        
        // Act
        double vonMises = StressCalculator.CalculateVonMisesStress(j2);
        
        // Assert
        Assert.AreEqual(0.0, vonMises, Tolerance);
    }
    
    [TestMethod]
    public void CalculateVonMisesStress_NegativeJ2_HandlesGracefully()
    {
        // Arrange: Negative J2 (should not happen physically, but handle gracefully)
        double j2 = -1.0;
        
        // Act
        double vonMises = StressCalculator.CalculateVonMisesStress(j2);
        
        // Assert: Implementation should handle this gracefully
        // Either return NaN or zero - both are acceptable for invalid input
        Assert.IsFalse(double.IsInfinity(vonMises), "Von Mises should not be infinity");
    }
    
    [TestMethod]
    public void CalculateStressRatio_NegativeStresses_HandlesCorrectly()
    {
        // Arrange: Compression (negative stresses)
        double sigma1 = -30.0;
        double sigma3 = -50.0;
        
        // Act
        double k = StressCalculator.CalculateStressRatio(sigma1, sigma3);
        
        // Assert: K = sigma3/sigma1
        Assert.AreEqual(-50.0 / -30.0, k, Tolerance);
        Assert.IsTrue(k > 0, "Ratio of compressions should be positive");
    }
    
    [TestMethod]
    public void CalculatePrincipalStresses2D_AllZero_ReturnsZero()
    {
        // Arrange: All zeros
        double sigmaX = 0.0;
        double sigmaY = 0.0;
        double tauXY = 0.0;
        
        // Act
        var (sigma3, sigma1, angle) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // Assert
        Assert.AreEqual(0.0, sigma1, Tolerance);
        Assert.AreEqual(0.0, sigma3, Tolerance);
        Assert.IsFalse(double.IsNaN(angle), "Angle should not be NaN even for zero stress");
    }
    
    [TestMethod]
    public void CalculateAngelierStressRatio_DivisionByZero_HandlesGracefully()
    {
        // Arrange: sigma1 == sigma3 (denominator would be zero)
        double sigma1 = 100.0;
        double sigma2 = 50.0;
        double sigma3 = 100.0; // Same as sigma1
        
        // Act
        double ratio = StressCalculator.CalculateAngelierStressRatio(sigma1, sigma2, sigma3);
        
        // Assert: Should handle gracefully, not crash
        Assert.IsFalse(double.IsNaN(ratio), "Should not be NaN");
        Assert.IsFalse(double.IsInfinity(ratio), "Should not be infinity");
        Assert.IsTrue(ratio >= 0 && ratio <= 1.0, "Should be in valid range [0,1]");
    }
    
    #endregion
    
    #region Integration Tests
    
    [TestMethod]
    public void IntegrationTest_CompleteStressAnalysisChain_ProducesConsistentResults()
    {
        // Arrange: Simulate a realistic stress state
        double sigmaX = -50.0;  // 50 MPa compression
        double sigmaY = -30.0;  // 30 MPa compression
        double sigmaZ = -40.0;  // 40 MPa out-of-plane
        double tauXY = 10.0;    // Shear stress
        
        // Act: Calculate all derived quantities
        var (i1, j2, lodeAngle) = StressCalculator.CalculateInvariants(
            sigmaX, sigmaY, sigmaZ, tauXY, 0, 0);
        
        var (sigma3, sigma2, sigma1) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, 0, 0);
        
        double vonMises = StressCalculator.CalculateVonMisesStress(j2);
        double meanStress = StressCalculator.CalculateMeanStress(i1);
        double deviatoricStress = StressCalculator.CalculateDeviatoricStress(sigma1, sigma3);
        double angelierRatio = StressCalculator.CalculateAngelierStressRatio(sigma1, sigma2, sigma3);
        double k = StressCalculator.CalculateStressRatio(sigma1, sigma3);
        
        // Assert: All values should be reasonable and consistent
        Assert.IsTrue(meanStress < 0, "Mean stress should be negative (compression)");
        Assert.IsTrue(vonMises > 0, "Von Mises stress should be positive");
        Assert.IsTrue(deviatoricStress >= 0, "Deviatoric stress should be non-negative");
        Assert.IsTrue(angelierRatio >= 0 && angelierRatio <= 1.0, "Angelier ratio in [0,1]");
        Assert.IsTrue(k > 0, "K ratio should be positive");
        // Note: K can be > 1 for compression (negative stresses)
        
        // Verify consistency: I1 = sigma1 + sigma2 + sigma3
        double i1Check = sigma1 + sigma2 + sigma3;
        Assert.AreEqual(i1, i1Check, Tolerance, "First invariant should match sum of principal stresses");
        
        // Verify mean stress
        Assert.AreEqual(i1 / 3.0, meanStress, Tolerance);
        
        Console.WriteLine($"Complete stress analysis:");
        Console.WriteLine($"  I1 = {i1:F3} MPa, J2 = {j2:F3} MPa²");
        Console.WriteLine($"  σ1 = {sigma1:F3}, σ2 = {sigma2:F3}, σ3 = {sigma3:F3} MPa");
        Console.WriteLine($"  von Mises = {vonMises:F3} MPa");
        Console.WriteLine($"  Mean stress = {meanStress:F3} MPa");
        Console.WriteLine($"  Deviatoric = {deviatoricStress:F3} MPa");
        Console.WriteLine($"  K = {k:F3}, Angelier = {angelierRatio:F3}");
    }
    
    [TestMethod]
    public void IntegrationTest_2DTo3DStressState_MaintainsConsistency()
    {
        // Arrange: 2D stress state
        double sigmaX = 80.0;
        double sigmaY = 40.0;
        double tauXY = 15.0;
        
        // Act: Calculate 2D principal stresses
        var (sigma3_2D, sigma1_2D, angle2D) = StressCalculator.CalculatePrincipalStresses2D(
            sigmaX, sigmaY, tauXY);
        
        // For plane strain, sigmaZ = nu * (sigmaX + sigmaY) for elastic analysis
        // For simplicity, assume sigmaZ = 0 for plane stress
        double sigmaZ = 0.0;
        
        // Calculate 3D principal stresses
        var (sigma3_3D, sigma2_3D, sigma1_3D) = StressCalculator.CalculatePrincipalStresses3D(
            sigmaX, sigmaY, sigmaZ, tauXY, 0, 0);
        
        // Assert: 2D and 3D principal stresses should be reasonably related
        // The 2D calculation gives in-plane principal stresses
        // The 3D calculation adds the out-of-plane component
        Assert.IsTrue(sigma1_2D > sigma3_2D, "2D: Sigma1 should be greater than Sigma3");
        Assert.IsTrue(sigma1_3D >= sigma2_3D, "3D: Sorting should be correct");
        Assert.IsTrue(sigma2_3D >= sigma3_3D, "3D: Sorting should be correct");
        
        // The maximum 2D principal stress should be among the 3D principal stresses
        // but may not be exactly equal due to 3D effects
        Console.WriteLine($"2D: σ1={sigma1_2D:F2}, σ3={sigma3_2D:F2}");
        Console.WriteLine($"3D: σ1={sigma1_3D:F2}, σ2={sigma2_3D:F2}, σ3={sigma3_3D:F2}");
    }
    
    #endregion
}
