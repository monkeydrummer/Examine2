using Examine2DModel.Strength;

namespace Examine2DModel.Tests.Strength;

[TestClass]
public class MohrCoulombCriterionTests
{
    [TestMethod]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Act
        var criterion = new MohrCoulombCriterion();
        
        // Assert
        Assert.AreEqual(0.0, criterion.Cohesion);
        Assert.AreEqual(30.0, criterion.FrictionAngle);
        Assert.AreEqual(0.0, criterion.TensileStrength);
        Assert.AreEqual("Mohr-Coulomb", criterion.Name);
    }
    
    [TestMethod]
    public void Constructor_WithParameters_SetsValues()
    {
        // Act
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 35.0, tensileStrength: 2.0);
        
        // Assert
        Assert.AreEqual(10.0, criterion.Cohesion);
        Assert.AreEqual(35.0, criterion.FrictionAngle);
        Assert.AreEqual(2.0, criterion.TensileStrength);
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_ZeroStress_ReturnsHighValue()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1: 0.0, sigma3: 0.0);
        
        // Assert
        Assert.IsTrue(sf > 10.0, "Strength factor should be high for zero stress");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_TensileFailure_ReturnsNegative()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0, tensileStrength: 1.0);
        
        // Act - large tensile stress (negative sigma3)
        double sf = criterion.CalculateStrengthFactor(sigma1: 0.0, sigma3: -5.0);
        
        // Assert
        Assert.AreEqual(-1.0, sf, "Should indicate tensile failure");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_SafeStressState_ReturnsGreaterThanOne()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        
        // Act - low stress state
        double sf = criterion.CalculateStrengthFactor(sigma1: 5.0, sigma3: 2.0);
        
        // Assert
        Assert.IsTrue(sf > 1.0, "Should be safe (SF > 1.0)");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_HighStress_ReturnsLessThanOne()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 1.0, frictionAngleDegrees: 20.0);
        
        // Act - very high stress
        double sf = criterion.CalculateStrengthFactor(sigma1: 100.0, sigma3: 10.0);
        
        // Assert
        Assert.IsTrue(sf < 1.0, "Should fail (SF < 1.0) under high stress");
    }
    
    [TestMethod]
    public void IsFailure_SafeState_ReturnsFalse()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        
        // Act
        bool failed = criterion.IsFailure(sigma1: 5.0, sigma3: 2.0);
        
        // Assert
        Assert.IsFalse(failed, "Should not be in failure");
    }
    
    [TestMethod]
    public void IsFailure_FailureState_ReturnsTrue()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 1.0, frictionAngleDegrees: 20.0);
        
        // Act
        bool failed = criterion.IsFailure(sigma1: 100.0, sigma3: 10.0);
        
        // Assert
        Assert.IsTrue(failed, "Should be in failure");
    }
    
    [TestMethod]
    public void CalculateFailureStress_KnownValues_ReturnsExpectedResult()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        double sigma3 = 5.0;
        
        // Act
        double sigma1f = criterion.CalculateFailureStress(sigma3);
        
        // Assert
        Assert.IsTrue(sigma1f > sigma3, "Failure stress should be greater than confining stress");
        
        // Verify it's approximately on the failure envelope
        double sf = criterion.CalculateStrengthFactor(sigma1f, sigma3);
        Assert.AreEqual(1.0, sf, 0.1, "Calculated failure stress should give SF ≈ 1.0");
    }
    
    [TestMethod]
    [DataRow(0.0, 30.0)]
    [DataRow(10.0, 20.0)]
    [DataRow(20.0, 35.0)]
    public void CalculateStrengthFactor_DifferentParameters_Varies(double cohesion, double phi)
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion, phi);
        double sigma1 = 20.0;
        double sigma3 = 5.0;
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1, sigma3);
        
        // Assert
        Assert.IsTrue(sf > 0, "Strength factor should be positive");
        Assert.IsTrue(sf < 100.0, "Strength factor should be capped");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_HigherCohesion_HigherSafetyFactor()
    {
        // Arrange
        var lowCohesion = new MohrCoulombCriterion(cohesion: 5.0, frictionAngleDegrees: 30.0);
        var highCohesion = new MohrCoulombCriterion(cohesion: 20.0, frictionAngleDegrees: 30.0);
        
        // Act
        double sfLow = lowCohesion.CalculateStrengthFactor(sigma1: 20.0, sigma3: 5.0);
        double sfHigh = highCohesion.CalculateStrengthFactor(sigma1: 20.0, sigma3: 5.0);
        
        // Assert
        Assert.IsTrue(sfHigh > sfLow, "Higher cohesion should give higher safety factor");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_HigherFriction_HigherSafetyFactor()
    {
        // Arrange
        var lowFriction = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 20.0);
        var highFriction = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 40.0);
        
        // Act
        double sfLow = lowFriction.CalculateStrengthFactor(sigma1: 20.0, sigma3: 5.0);
        double sfHigh = highFriction.CalculateStrengthFactor(sigma1: 20.0, sigma3: 5.0);
        
        // Assert
        Assert.IsTrue(sfHigh > sfLow, "Higher friction angle should give higher safety factor");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_KnownProblem_MatchesExpectedValue()
    {
        // Arrange: Example from Hoek's "Rock Mechanics" textbook
        // Sandstone with c=10 MPa, φ=30°, confining stress σ3=5 MPa
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        double sigma3 = 5.0;
        
        // Calculate expected failure stress using M-C equation
        // σ1f = σ3·Nφ + 2c·√Nφ where Nφ = (1+sinφ)/(1-sinφ)
        double phi_rad = 30.0 * Math.PI / 180.0;
        double nPhi = (1.0 + Math.Sin(phi_rad)) / (1.0 - Math.Sin(phi_rad));
        double sigma1f_expected = sigma3 * nPhi + 2.0 * 10.0 * Math.Sqrt(nPhi);
        
        // Test at half the failure stress (should have SF ≈ 2.0)
        double sigma1_test = sigma1f_expected / 2.0;
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1_test, sigma3);
        
        // Assert
        Assert.IsTrue(sf > 1.5 && sf < 2.5, $"Strength factor should be approximately 2.0, got {sf}");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_AtFailure_ReturnsOne()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        double sigma3 = 5.0;
        
        // Act: Calculate failure stress and verify SF = 1.0
        double sigma1f = criterion.CalculateFailureStress(sigma3);
        double sf = criterion.CalculateStrengthFactor(sigma1f, sigma3);
        
        // Assert
        Assert.AreEqual(1.0, sf, 0.05, "Strength factor at failure should be 1.0");
    }
    
    [TestMethod]
    public void CalculateFailureStress_NegativeConfinement_HandlesCorrectly()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 5.0, frictionAngleDegrees: 25.0, tensileStrength: 1.0);
        
        // Act: Tension (negative σ3)
        double sigma1f = criterion.CalculateFailureStress(sigma3: -0.5);
        
        // Assert: Should still return a valid value
        Assert.IsFalse(double.IsNaN(sigma1f), "Should handle tension correctly");
        Assert.IsTrue(sigma1f > -0.5, "Failure stress should be greater than confining stress");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_ZeroCohesionAndFriction_ReturnsVeryLow()
    {
        // Arrange: No strength (e.g., water, air)
        var criterion = new MohrCoulombCriterion(cohesion: 0.0, frictionAngleDegrees: 0.0);
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1: 1.0, sigma3: 0.0);
        
        // Assert: Should have very low or near-zero strength
        Assert.IsTrue(sf <= 1.0, "Material with no strength should fail under any stress");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_ExtremelyHighStress_DoesNotOverflow()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        
        // Act: Very high stresses (deep underground)
        double sf = criterion.CalculateStrengthFactor(sigma1: 1000.0, sigma3: 500.0);
        
        // Assert
        Assert.IsFalse(double.IsNaN(sf), "Should not produce NaN");
        Assert.IsFalse(double.IsInfinity(sf), "Should not produce infinity");
        Assert.IsTrue(sf >= 0, "SF should be non-negative");
    }
    
    [TestMethod]
    public void CalculateFailureStress_VariousConfinements_IncreaseMonotonically()
    {
        // Arrange
        var criterion = new MohrCoulombCriterion(cohesion: 10.0, frictionAngleDegrees: 30.0);
        
        // Act: Calculate failure stress at increasing confinement
        double sigma1f_0 = criterion.CalculateFailureStress(0);
        double sigma1f_5 = criterion.CalculateFailureStress(5);
        double sigma1f_10 = criterion.CalculateFailureStress(10);
        double sigma1f_20 = criterion.CalculateFailureStress(20);
        
        // Assert: Failure stress should increase with confinement
        Assert.IsTrue(sigma1f_5 > sigma1f_0, "Failure stress should increase with confinement");
        Assert.IsTrue(sigma1f_10 > sigma1f_5, "Failure stress should increase with confinement");
        Assert.IsTrue(sigma1f_20 > sigma1f_10, "Failure stress should increase with confinement");
    }
}
