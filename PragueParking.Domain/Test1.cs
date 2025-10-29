using Microsoft.VisualStudio.TestTools.UnitTesting;
using PragueParking.Domain;
using System;

namespace PragueParking.Tests
{
    [TestClass]
    public class PricingServiceTests
    {
        [TestMethod]
        public void CalculateFee_LessOrEqual_FreeMinutes_ReturnsZero()
        {
            // Arrange
            var now = DateTime.UtcNow; // ta nu en gång
            var v = new Car("ABC123", checkInUtc: now.AddMinutes(-10)); // exakt 10 min
            decimal pricePerHourCar = 20m;
            decimal pricePerHourMc = 10m;
            int freeMinutes = 10;

            // Act
            var fee = PricingService.CalculateFee(v, now, pricePerHourCar, pricePerHourMc, freeMinutes);

            // Assert
            Assert.AreEqual(0m, fee);
        }

        [TestMethod]
        public void CalculateFee_Car_65Minutes_Returns40CZK()
        {
            // Arrange
            var now = DateTime.UtcNow; // ta nu en gång
            var v = new Car("DEF456", checkInUtc: now.AddMinutes(-65)); // 65 min
            decimal pricePerHourCar = 20m;
            decimal pricePerHourMc = 10m;
            int freeMinutes = 10;

            // Act
            var fee = PricingService.CalculateFee(v, now, pricePerHourCar, pricePerHourMc, freeMinutes);

            // Assert
            Assert.AreEqual(40m, fee); // 2 timmar × 20 = 40
        }
    }
}
