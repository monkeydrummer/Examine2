using Examine2DModel.Strength;

namespace Examine2DModel.Tests.Strength;

[TestClass]
public class HoekBrownCriterionTests
{
    [TestMethod]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Act
        var criterion = new HoekBrownCriterion();
        
        // Assert
        Assert.AreEqual(10.0, criterion.M);
        Assert.AreEqual(1.0, criterion.S);
        Assert.AreEqual(50.0, criterion.Sci);
        Assert.AreEqual("Hoek-Brown (Original)", criterion.Name);
    }
    
    [TestMethod]
    public void Constructor_WithParameters_SetsValues()
    {
        // Act
        var criterion = new HoekBrownCriterion(m: 15.0, s: 0.5, sci: 80.0);
        
        // Assert
        Assert.AreEqual(15.0, criterion.M);
        Assert.AreEqual(0.5, criterion.S);
        Assert.AreEqual(80.0, criterion.Sci);
    }
    
    [TestMethod]
    public void GetTensileStrength_IntactRock_ReturnsCorrectValue()
    {
        // Arrange - Intact rock (s = 1.0)
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        double tensileStrength = criterion.GetTensileStrength();
        
        // Assert
        // σt = -s·σci/m = -1.0 * 50 / 10 = -5.0
        Assert.AreEqual(-5.0, tensileStrength, 0.001);
    }
    
    [TestMethod]
    public void GetTensileStrength_FracturedRock_ReturnsLowerValue()
    {
        // Arrange - Fractured rock (s = 0.01)
        var criterion = new HoekBrownCriterion(m: 1.0, s: 0.01, sci: 50.0);
        
        // Act
        double tensileStrength = criterion.GetTensileStrength();
        
        // Assert
        // σt = -s·σci/m = -0.01 * 50 / 1.0 = -0.5
        Assert.AreEqual(-0.5, tensileStrength, 0.001);
        Assert.IsTrue(tensileStrength < 0, "Tensile strength should be negative (tension)");
    }
    
    [TestMethod]
    public void CalculateFailureStress_ZeroConfinement_ReturnsUCS()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        double sigma1f = criterion.CalculateFailureStress(sigma3: 0.0);
        
        // Assert
        // For σ3 = 0: σ1 = σci·√s = 50·√1 = 50
        Assert.AreEqual(50.0, sigma1f, 0.1, "At zero confinement, should return UCS for intact rock");
    }
    
    [TestMethod]
    public void CalculateFailureStress_PositiveConfinement_IncreasesWithConfinement()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        double sigma1_low = criterion.CalculateFailureStress(sigma3: 5.0);
        double sigma1_high = criterion.CalculateFailureStress(sigma3: 10.0);
        
        // Assert
        Assert.IsTrue(sigma1_high > sigma1_low, "Failure stress should increase with confinement");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_ZeroStress_ReturnsHighValue()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1: 0.0, sigma3: 0.0);
        
        // Assert
        Assert.IsTrue(sf > 10.0, "Strength factor should be high for zero stress");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_TensileFailure_ReturnsNegative()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        double tensileStrength = criterion.GetTensileStrength();
        
        // Act - stress beyond tensile strength
        double sf = criterion.CalculateStrengthFactor(sigma1: 0.0, sigma3: tensileStrength - 1.0);
        
        // Assert
        Assert.AreEqual(-1.0, sf, "Should indicate tensile failure");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_OnFailureEnvelope_ReturnsApproximatelyOne()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        double sigma3 = 10.0;
        
        // Act
        double sigma1f = criterion.CalculateFailureStress(sigma3);
        double sf = criterion.CalculateStrengthFactor(sigma1f, sigma3);
        
        // Assert
        Assert.AreEqual(1.0, sf, 0.15, "Strength factor should be ≈1.0 on failure envelope");
    }
    
    [TestMethod]
    public void IsFailure_SafeState_ReturnsFalse()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        bool failed = criterion.IsFailure(sigma1: 30.0, sigma3: 10.0);
        
        // Assert
        Assert.IsFalse(failed, "Should not be in failure for safe stress state");
    }
    
    [TestMethod]
    public void IsFailure_HighStress_ReturnsTrue()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 5.0, s: 0.1, sci: 30.0);
        
        // Act
        bool failed = criterion.IsFailure(sigma1: 100.0, sigma3: 5.0);
        
        // Assert
        Assert.IsTrue(failed, "Should be in failure for high stress");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_IntactVsFractured_DifferentResults()
    {
        // Arrange
        var intact = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        var fractured = new HoekBrownCriterion(m: 2.0, s: 0.01, sci: 50.0);
        
        // Act
        double sfIntact = intact.CalculateStrengthFactor(sigma1: 30.0, sigma3: 5.0);
        double sfFractured = fractured.CalculateStrengthFactor(sigma1: 30.0, sigma3: 5.0);
        
        // Assert
        Assert.IsTrue(sfIntact > sfFractured, "Intact rock should have higher safety factor than fractured");
    }
    
    [TestMethod]
    [DataRow(5.0, 1.0, 40.0)]
    [DataRow(10.0, 0.5, 50.0)]
    [DataRow(15.0, 1.0, 60.0)]
    public void CalculateStrengthFactor_VariousParameters_ReturnsPositive(double m, double s, double sci)
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m, s, sci);
        
        // Act
        double sf = criterion.CalculateStrengthFactor(sigma1: 20.0, sigma3: 5.0);
        
        // Assert
        Assert.IsTrue(sf > 0, "Strength factor should be positive");
        Assert.IsTrue(sf < 100.0, "Strength factor should be capped");
    }
    
    [TestMethod]
    public void CalculateFailureStress_MatchesHoekBrownEquation()
    {
        // Arrange: Hoek-Brown failure criterion: σ1 = σ3 + σci·√(m·σ3/σci + s)
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        double sigma3 = 10.0;
        
        // Act
        double sigma1f = criterion.CalculateFailureStress(sigma3);
        
        // Assert: Verify against H-B equation
        double expectedSigma1f = sigma3 + 50.0 * Math.Sqrt(10.0 * sigma3 / 50.0 + 1.0);
        Assert.AreEqual(expectedSigma1f, sigma1f, 0.1, "Should match Hoek-Brown equation");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_HigherM_HigherSafetyFactor()
    {
        // Arrange
        var lowM = new HoekBrownCriterion(m: 5.0, s: 1.0, sci: 50.0);
        var highM = new HoekBrownCriterion(m: 20.0, s: 1.0, sci: 50.0);
        
        // Act
        double sfLow = lowM.CalculateStrengthFactor(sigma1: 60.0, sigma3: 10.0);
        double sfHigh = highM.CalculateStrengthFactor(sigma1: 60.0, sigma3: 10.0);
        
        // Assert
        Assert.IsTrue(sfHigh > sfLow, "Higher m parameter should give higher safety factor");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_HigherS_HigherSafetyFactor()
    {
        // Arrange
        var lowS = new HoekBrownCriterion(m: 10.0, s: 0.1, sci: 50.0);
        var highS = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act
        double sfLow = lowS.CalculateStrengthFactor(sigma1: 40.0, sigma3: 10.0);
        double sfHigh = highS.CalculateStrengthFactor(sigma1: 40.0, sigma3: 10.0);
        
        // Assert
        Assert.IsTrue(sfHigh > sfLow, "Higher s parameter should give higher safety factor");
    }
    
    [TestMethod]
    public void CalculateFailureStress_VeryHighConfinement_ScalesCorrectly()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act: Test at very high confinement
        double sigma1f_100 = criterion.CalculateFailureStress(100.0);
        double sigma1f_200 = criterion.CalculateFailureStress(200.0);
        
        // Assert: Should scale according to H-B equation (square root relationship)
        Assert.IsTrue(sigma1f_200 > sigma1f_100, "Failure stress increases with confinement");
        
        // The ratio should not be linear (due to square root)
        double ratio = sigma1f_200 / sigma1f_100;
        Assert.IsTrue(ratio < 2.0, "Ratio should be less than 2 due to square root scaling");
        Assert.IsTrue(ratio > 1.0, "Ratio should be greater than 1");
    }
    
    [TestMethod]
    public void GetTensileStrength_DifferentParameters_VariesCorrectly()
    {
        // Arrange: Different rock qualities
        var intact = new HoekBrownCriterion(m: 25.0, s: 1.0, sci: 100.0);
        var weathered = new HoekBrownCriterion(m: 5.0, s: 0.1, sci: 100.0);
        var heavily_fractured = new HoekBrownCriterion(m: 1.0, s: 0.001, sci: 100.0);
        
        // Act
        double tensileIntact = intact.GetTensileStrength();
        double tensileWeathered = weathered.GetTensileStrength();
        double tensileFractured = heavily_fractured.GetTensileStrength();
        
        // Assert: Tensile strength decreases with rock mass degradation
        Assert.IsTrue(tensileIntact < 0, "Tensile strength is negative (tension)");
        Assert.IsTrue(tensileWeathered < 0, "Tensile strength is negative (tension)");
        Assert.IsTrue(tensileFractured < 0, "Tensile strength is negative (tension)");
        
        // The tensile strength formula is σt = -s·σci/m
        // Higher s and lower m → higher (less negative) tensile strength
        // For intact: -1*100/25 = -4
        // For weathered: -0.1*100/5 = -2
        // For fractured: -0.001*100/1 = -0.1
        // So actually weathered has HIGHER tensile strength than intact!
        Assert.IsTrue(Math.Abs(tensileWeathered) < Math.Abs(tensileIntact), 
            "Weathered has lower magnitude (higher) tensile strength due to formula σt = -s·σci/m");
        Assert.IsTrue(Math.Abs(tensileFractured) < Math.Abs(tensileWeathered), 
            "Fractured has even lower magnitude tensile strength");
    }
    
    [TestMethod]
    public void CalculateStrengthFactor_NearTensileLimit_ApproachesOne()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        double tensileStrength = criterion.GetTensileStrength();
        
        // Act: Test just above tensile limit
        double sigma3_safe = tensileStrength + 0.1;
        double sigma1_safe = criterion.CalculateFailureStress(sigma3_safe);
        double sf = criterion.CalculateStrengthFactor(sigma1_safe, sigma3_safe);
        
        // Assert: At failure envelope, SF ≈ 1.0
        Assert.AreEqual(1.0, sf, 0.2, "At failure envelope, SF should be approximately 1.0");
    }
    
    [TestMethod]
    public void IsFailure_VariousStressStates_CorrectlyIdentifies()
    {
        // Arrange
        var criterion = new HoekBrownCriterion(m: 10.0, s: 1.0, sci: 50.0);
        
        // Act & Assert: Safe state
        double sigma1_safe = 30.0;
        double sigma3_safe = 10.0;
        Assert.IsFalse(criterion.IsFailure(sigma1_safe, sigma3_safe), "Should be safe");
        
        // Failure state
        double sigma1_fail = criterion.CalculateFailureStress(sigma3_safe) + 10.0;
        Assert.IsTrue(criterion.IsFailure(sigma1_fail, sigma3_safe), "Should be in failure");
        
        // Tensile failure
        double tensileStrength = criterion.GetTensileStrength();
        Assert.IsTrue(criterion.IsFailure(0, tensileStrength - 1.0), "Should be tensile failure");
    }
}
