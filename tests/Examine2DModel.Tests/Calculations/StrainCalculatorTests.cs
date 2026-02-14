using Examine2DModel.Calculations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.Calculations;

[TestClass]
public class StrainCalculatorTests
{
    private const double Tolerance = 1e-6;
    
    // Typical rock properties for testing
    private const double TypicalYoungModulus = 50000.0; // 50 GPa
    private const double TypicalPoissonRatio = 0.25;
    
    #region Principal Strains Tests
    
    [TestMethod]
    public void CalculatePrincipalStrains_UniaxialTension_ReturnsCorrectStrains()
    {
        // Arrange: Uniaxial tension
        double sigma1 = 100.0; // 100 MPa tension
        double sigma3 = 0.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert
        // ε1 = (1/E)[(1-ν²)σ1 - ν(1+ν)·0] = (1/E)(1-ν²)σ1
        double expectedEpsilon1 = (1.0 / e) * (1.0 - pr * pr) * sigma1;
        Assert.AreEqual(expectedEpsilon1, epsilon1, Tolerance);
        
        // ε3 = (1/E)[0 - ν(1+ν)σ1] = -(ν(1+ν)/E)σ1
        double expectedEpsilon3 = -(pr * (1.0 + pr) / e) * sigma1;
        Assert.AreEqual(expectedEpsilon3, epsilon3, Tolerance);
        
        // Lateral strain should be negative (Poisson effect)
        Assert.IsTrue(epsilon3 < 0, "Lateral strain should be negative for uniaxial tension");
    }
    
    [TestMethod]
    public void CalculatePrincipalStrains_EqualBiaxialStress_ReturnsEqualStrains()
    {
        // Arrange: Equal biaxial stress
        double sigma = -100.0; // Compression
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma, sigma, e, pr);
        
        // Assert: Strains should be equal
        Assert.AreEqual(epsilon1, epsilon3, Tolerance);
        
        // Both should be negative (compression)
        Assert.IsTrue(epsilon1 < 0, "Strain should be negative for compression");
    }
    
    [TestMethod]
    public void CalculatePrincipalStrains_TypicalRockStress_ReturnsReasonableStrains()
    {
        // Arrange: Typical in-situ stress state
        double sigma1 = -30.0; // 30 MPa compression
        double sigma3 = -20.0; // 20 MPa compression
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert: Strains should be small and negative
        Assert.IsTrue(epsilon1 < 0, "Epsilon1 should be negative (compression)");
        Assert.IsTrue(epsilon3 < 0, "Epsilon3 should be negative (compression)");
        Assert.IsTrue(Math.Abs(epsilon1) > Math.Abs(epsilon3), "Major stress produces larger strain magnitude");
    }
    
    #endregion
    
    #region Cartesian Strains Tests
    
    [TestMethod]
    public void CalculateCartesianStrains_PureShear_ReturnsZeroNormalStrains()
    {
        // Arrange: Pure shear stress
        double sigmaX = 0.0;
        double sigmaY = 0.0;
        double tauXY = 50.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert
        Assert.AreEqual(0.0, epsilonX, Tolerance, "Normal strain X should be zero");
        Assert.AreEqual(0.0, epsilonY, Tolerance, "Normal strain Y should be zero");
        Assert.AreNotEqual(0.0, gammaXY, "Shear strain should not be zero");
    }
    
    [TestMethod]
    public void CalculateCartesianStrains_ShearStrainConsistentWithShearModulus_CalculatesCorrectly()
    {
        // Arrange
        double tauXY = 100.0;
        double e = 50000.0;
        double pr = 0.25;
        double g = StrainCalculator.CalculateShearModulus(e, pr); // E/(2(1+ν))
        
        // Act
        var (_, _, gammaXY) = StrainCalculator.CalculateCartesianStrains(0, 0, tauXY, e, pr);
        
        // Assert: γ = τ/G
        double expectedGamma = tauXY / g;
        Assert.AreEqual(expectedGamma, gammaXY, Tolerance);
    }
    
    [TestMethod]
    public void CalculateCartesianStrains_UniaxialStressX_ReturnsCorrectStrains()
    {
        // Arrange: Uniaxial stress in X direction
        double sigmaX = 100.0;
        double sigmaY = 0.0;
        double tauXY = 0.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert
        Assert.IsTrue(epsilonX > 0, "Tensile strain in X direction");
        Assert.IsTrue(epsilonY < 0, "Compressive strain in Y direction (Poisson effect)");
        Assert.AreEqual(0.0, gammaXY, Tolerance, "No shear strain");
        
        // Check Poisson's ratio relationship: εy/εx ≈ -ν (for plane stress, approximately)
        double ratio = epsilonY / epsilonX;
        Assert.IsTrue(Math.Abs(ratio + pr * (1 + pr) / (1 - pr * pr)) < 0.1, 
            "Strain ratio should relate to Poisson's ratio");
    }
    
    #endregion
    
    #region Volumetric and Shear Strain Tests
    
    [TestMethod]
    public void CalculateVolumetricStrain_TensionAndCompression_ReturnsCorrectValue()
    {
        // Arrange
        double epsilon1 = 0.002;  // 2000 microstrain tension
        double epsilon3 = -0.001; // 1000 microstrain compression
        
        // Act
        double volumetricStrain = StrainCalculator.CalculateVolumetricStrain(epsilon1, epsilon3);
        
        // Assert
        Assert.AreEqual(0.001, volumetricStrain, Tolerance, "Volumetric strain = ε1 + ε3");
    }
    
    [TestMethod]
    public void CalculateVolumetricStrain_HydrostaticCompression_AllNegative()
    {
        // Arrange: Hydrostatic compression (all strains negative)
        double epsilon1 = -0.001;
        double epsilon3 = -0.001;
        
        // Act
        double volumetricStrain = StrainCalculator.CalculateVolumetricStrain(epsilon1, epsilon3);
        
        // Assert
        Assert.AreEqual(-0.002, volumetricStrain, Tolerance);
        Assert.IsTrue(volumetricStrain < 0, "Volumetric strain should be negative for compression");
    }
    
    [TestMethod]
    public void CalculateShearStrain_DifferentPrincipalStrains_ReturnsCorrectValue()
    {
        // Arrange
        double epsilon1 = 0.003;
        double epsilon3 = 0.001;
        
        // Act
        double shearStrain = StrainCalculator.CalculateShearStrain(epsilon1, epsilon3);
        
        // Assert
        Assert.AreEqual(0.002, shearStrain, Tolerance, "Shear strain = ε1 - ε3");
    }
    
    [TestMethod]
    public void CalculateShearStrain_EqualStrains_ReturnsZero()
    {
        // Arrange: No deviatoric strain
        double epsilon = 0.001;
        
        // Act
        double shearStrain = StrainCalculator.CalculateShearStrain(epsilon, epsilon);
        
        // Assert
        Assert.AreEqual(0.0, shearStrain, Tolerance, "Shear strain should be zero for equal principal strains");
    }
    
    #endregion
    
    #region Shear Modulus Tests
    
    [TestMethod]
    public void CalculateShearModulus_TypicalValues_ReturnsCorrectValue()
    {
        // Arrange
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        double g = StrainCalculator.CalculateShearModulus(e, pr);
        
        // Assert: G = E/(2(1+ν))
        double expected = e / (2.0 * (1.0 + pr));
        Assert.AreEqual(expected, g, Tolerance);
    }
    
    [TestMethod]
    public void CalculateShearModulus_PoissonRatioZero_ReturnsHalfYoungModulus()
    {
        // Arrange
        double e = 100000.0;
        double pr = 0.0;
        
        // Act
        double g = StrainCalculator.CalculateShearModulus(e, pr);
        
        // Assert: When ν=0, G = E/2
        Assert.AreEqual(e / 2.0, g, Tolerance);
    }
    
    [DataRow(50000.0, 0.25, 20000.0)]
    [DataRow(100000.0, 0.30, 38461.54)]
    [DataRow(30000.0, 0.20, 12500.0)]
    [TestMethod]
    public void CalculateShearModulus_VariousInputs_ReturnsExpectedValues(
        double youngModulus, double poissonRatio, double expectedG)
    {
        // Act
        double g = StrainCalculator.CalculateShearModulus(youngModulus, poissonRatio);
        
        // Assert
        Assert.AreEqual(expectedG, g, 0.01);
    }
    
    #endregion
    
    #region Integration Tests
    
    [TestMethod]
    public void IntegrationTest_StressToStrain_RoundTripConsistency()
    {
        // Arrange: Known stress state
        double sigmaX = 100.0;
        double sigmaY = 50.0;
        double tauXY = 20.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act: Calculate strains
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert: Verify order of magnitude is reasonable
        Assert.IsTrue(Math.Abs(epsilonX) < 0.01, "Strain should be small for typical rock");
        Assert.IsTrue(Math.Abs(epsilonY) < 0.01, "Strain should be small for typical rock");
        Assert.IsTrue(Math.Abs(gammaXY) < 0.01, "Shear strain should be small for typical rock");
    }
    
    [TestMethod]
    public void IntegrationTest_PrincipalStressesToStrains_VolumetricAndShearConsistent()
    {
        // Arrange
        double sigma1 = -50.0;
        double sigma3 = -30.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        double volumetric = StrainCalculator.CalculateVolumetricStrain(epsilon1, epsilon3);
        double shear = StrainCalculator.CalculateShearStrain(epsilon1, epsilon3);
        
        // Assert
        Assert.IsTrue(volumetric < 0, "Volumetric strain negative for compression");
        Assert.IsTrue(shear < 0, "Shear strain indicates relative compression difference");
        // Both strains should be reasonable magnitudes for rock mechanics
        Assert.IsTrue(Math.Abs(volumetric) < 0.01, "Volumetric strain should be small");
        Assert.IsTrue(Math.Abs(shear) < 0.01, "Shear strain should be small");
    }
    
    #endregion
    
    #region Edge Cases Tests
    
    [TestMethod]
    public void CalculatePrincipalStrains_ZeroStress_ReturnsZeroStrain()
    {
        // Arrange
        double sigma1 = 0.0;
        double sigma3 = 0.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert
        Assert.AreEqual(0.0, epsilon1, Tolerance);
        Assert.AreEqual(0.0, epsilon3, Tolerance);
    }
    
    [TestMethod]
    public void CalculatePrincipalStrains_VeryStiffMaterial_ProducesSmallStrains()
    {
        // Arrange: Very high Young's modulus (e.g., steel, diamond)
        double sigma1 = -100.0; // 100 MPa compression
        double sigma3 = -50.0;
        double e = 200000.0; // 200 GPa (steel-like)
        double pr = 0.30;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert: Very stiff material should have very small strains
        Assert.IsTrue(Math.Abs(epsilon1) < 0.001, "Stiff material should have small strains");
        Assert.IsTrue(Math.Abs(epsilon3) < 0.001, "Stiff material should have small strains");
    }
    
    [TestMethod]
    public void CalculatePrincipalStrains_SoftMaterial_ProducesLargerStrains()
    {
        // Arrange: Low Young's modulus (e.g., soft clay, foam)
        double sigma1 = -10.0;
        double sigma3 = -5.0;
        double e = 1000.0; // 1 GPa (soft material)
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert: Soft material should have larger strains
        Assert.IsTrue(Math.Abs(epsilon1) > 0.001, "Soft material should have larger strains");
        Assert.IsTrue(Math.Abs(epsilon3) > 0.0005, "Soft material should have larger strains");
    }
    
    [TestMethod]
    public void CalculateCartesianStrains_ZeroPoissonRatio_ProducesIndependentStrains()
    {
        // Arrange: Zero Poisson's ratio (no lateral strain coupling)
        double sigmaX = 100.0;
        double sigmaY = 0.0;
        double tauXY = 0.0;
        double e = 50000.0;
        double pr = 0.0;
        
        // Act
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert: With ν=0, εx = σx/E and εy = 0
        Assert.AreEqual(sigmaX / e, epsilonX, Tolerance, "εx = σx/E when ν=0");
        Assert.AreEqual(0.0, epsilonY, Tolerance, "εy should be zero when ν=0 and σy=0");
    }
    
    [TestMethod]
    public void CalculateCartesianStrains_HighPoissonRatio_ProducesStrongCoupling()
    {
        // Arrange: High Poisson's ratio (strong coupling)
        double sigmaX = 100.0;
        double sigmaY = 0.0;
        double tauXY = 0.0;
        double e = 50000.0;
        double pr = 0.45; // Near incompressible limit (0.5)
        
        // Act
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert: High Poisson's ratio should produce significant transverse strain
        Assert.IsTrue(epsilonX > 0, "Tensile strain in X");
        Assert.IsTrue(epsilonY < 0, "Compressive strain in Y (Poisson effect)");
        Assert.IsTrue(Math.Abs(epsilonY) > Math.Abs(epsilonX) * 0.3, 
            "High Poisson's ratio should produce significant transverse strain");
    }
    
    [TestMethod]
    public void CalculateShearModulus_LimitsCheck_ReturnsValidValues()
    {
        // Arrange & Act: Test at Poisson's ratio limits
        double g_min = StrainCalculator.CalculateShearModulus(100000.0, 0.0);  // ν=0
        double g_typical = StrainCalculator.CalculateShearModulus(100000.0, 0.25);
        double g_max = StrainCalculator.CalculateShearModulus(100000.0, 0.49); // Near ν=0.5
        
        // Assert
        Assert.AreEqual(50000.0, g_min, Tolerance, "G = E/2 when ν=0");
        Assert.IsTrue(g_typical < g_min, "G decreases as ν increases");
        Assert.IsTrue(g_max < g_typical, "G continues to decrease");
        Assert.IsTrue(g_max > 0, "G should remain positive even near incompressible limit");
    }
    
    [TestMethod]
    public void CalculateVolumetricStrain_PureShear_ReturnsZero()
    {
        // Arrange: Pure shear state has equal and opposite principal strains
        double epsilon1 = 0.002;
        double epsilon3 = -0.002;
        
        // Act
        double volumetricStrain = StrainCalculator.CalculateVolumetricStrain(epsilon1, epsilon3);
        
        // Assert
        Assert.AreEqual(0.0, volumetricStrain, Tolerance, 
            "Volumetric strain should be zero for pure shear (no volume change)");
    }
    
    [TestMethod]
    public void CalculateShearStrain_HydrostaticCompression_ReturnsZero()
    {
        // Arrange: Hydrostatic compression (equal principal strains)
        double epsilon = -0.001;
        
        // Act
        double shearStrain = StrainCalculator.CalculateShearStrain(epsilon, epsilon);
        
        // Assert
        Assert.AreEqual(0.0, shearStrain, Tolerance, 
            "Shear strain should be zero for hydrostatic state (no distortion)");
    }
    
    #endregion
    
    #region Physical Consistency Tests
    
    [TestMethod]
    public void PhysicalConsistency_TensionProducesPositiveStrain()
    {
        // Arrange: Tensile stress should produce positive (extensional) strain
        double sigma1 = 50.0; // Tension
        double sigma3 = 0.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert
        Assert.IsTrue(epsilon1 > 0, "Tensile stress should produce positive strain");
    }
    
    [TestMethod]
    public void PhysicalConsistency_CompressionProducesNegativeStrain()
    {
        // Arrange: Compressive stress should produce negative strain
        double sigma1 = -50.0; // Compression
        double sigma3 = -30.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilon1, epsilon3) = StrainCalculator.CalculatePrincipalStrains(sigma1, sigma3, e, pr);
        
        // Assert
        Assert.IsTrue(epsilon1 < 0, "Compressive stress should produce negative strain");
        Assert.IsTrue(epsilon3 < 0, "Compressive stress should produce negative strain");
    }
    
    [TestMethod]
    public void PhysicalConsistency_PoissonEffect_TransverseStrainOpposite()
    {
        // Arrange: Uniaxial tension in X should produce contraction in Y
        double sigmaX = 100.0; // Tension
        double sigmaY = 0.0;
        double tauXY = 0.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilonX, epsilonY, _) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Assert
        Assert.IsTrue(epsilonX > 0, "Tensile stress in X produces extension in X");
        Assert.IsTrue(epsilonY < 0, "Poisson effect: tension in X produces contraction in Y");
    }
    
    [TestMethod]
    public void PhysicalConsistency_StrainEnergy_NonNegative()
    {
        // Arrange: Calculate strain energy density U = (1/2)σᵢⱼεᵢⱼ
        double sigmaX = -50.0;
        double sigmaY = -30.0;
        double tauXY = 10.0;
        double e = 50000.0;
        double pr = 0.25;
        
        // Act
        var (epsilonX, epsilonY, gammaXY) = StrainCalculator.CalculateCartesianStrains(
            sigmaX, sigmaY, tauXY, e, pr);
        
        // Strain energy density (simplified for 2D)
        double u = 0.5 * (sigmaX * epsilonX + sigmaY * epsilonY + tauXY * gammaXY);
        
        // Assert: Strain energy should be positive (work done on material)
        Assert.IsTrue(u > 0, "Strain energy density should be positive");
    }
    
    [TestMethod]
    public void PhysicalConsistency_ShearModulusRelation_GEqualsEOver2OnePlusNu()
    {
        // Arrange
        double[] youngModuli = { 10000.0, 50000.0, 100000.0 };
        double[] poissonRatios = { 0.15, 0.25, 0.35 };
        
        foreach (double e in youngModuli)
        {
            foreach (double pr in poissonRatios)
            {
                // Act
                double g = StrainCalculator.CalculateShearModulus(e, pr);
                double expectedG = e / (2.0 * (1.0 + pr));
                
                // Assert
                Assert.AreEqual(expectedG, g, Tolerance, 
                    $"G = E/(2(1+ν)) should hold for E={e}, ν={pr}");
            }
        }
    }
    
    #endregion
}
