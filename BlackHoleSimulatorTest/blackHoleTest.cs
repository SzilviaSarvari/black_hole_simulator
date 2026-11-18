
using Microsoft.VisualStudio.TestTools.UnitTesting;
using black_hole_simulator;

namespace blackHoleTest
{
    [TestClass]
    public class BlackHoleTests
    {
        [TestMethod]
        public void CalculateSchwarzschildRadius_OneSolarMass_ReturnsExpectedRadius()
        {
            // Arrange
            BlackHole blackHole = new BlackHole(PhysicsConstants.SolarMass);

            // Act
            double radiusTest = blackHole.CalculateSchwarzschildRadius();
            // Assert
            Assert.AreEqual(2953.34, radiusTest, 0.1);
        }

        [TestMethod]
        public void BlackHole_NegativeMass_ThrowsException()
        {
            // Arrange
           // BlackHole blackHole = null;
            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(
        () => new BlackHole(-1000)
    );
        }

        [TestMethod]
        public void GravitationalAccelerationTest()
        {
            // Arrange
            BlackHole blackHole = new BlackHole(PhysicsConstants.SolarMass);

            double distance1 = 1_000_000;
            double distance2 = 2_000_000;

            // Act
            double acceleration1 = blackHole.GravitationalAccelerationCalculation(distance1);
            double acceleration2 = blackHole.GravitationalAccelerationCalculation(distance2);

            //Assert
            Assert.AreEqual(acceleration1 / 4, acceleration2, 0.01);

        }

        [TestMethod]
        public void GravitationalAcceleration_NegativeDistance_ThrowsException()
        {
            // Arrange
            BlackHole blackHole = new BlackHole(PhysicsConstants.SolarMass);

            // Act
            double negativeDistance = -1000;

            // Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(
        () => blackHole.GravitationalAccelerationCalculation(negativeDistance)
    );
        }

        [TestMethod]
        public void BlackHole_Radii_HaveCorrectProportions()
        {
            // Arrange
            BlackHole blackHole =
                new BlackHole(PhysicsConstants.SolarMass * 10);

            // Act
            double eventHorizon =
                blackHole.CalculateSchwarzschildRadius();

            double photonSphere =
                blackHole.CalculatePhotonSphereRadius();

            double isco =
                blackHole.CalculateInnermostStableOrbit();

            // Assert
            Assert.AreEqual(1.5 * eventHorizon, photonSphere, 0.01);
            Assert.AreEqual(3.0 * eventHorizon, isco, 0.01);

        }

    }

}

