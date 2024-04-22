using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthorizationAPI.Controllers;
using AuthorizationAPI;
using NUnit.Framework;
using System.Collections.Generic;

namespace AuthorizationAPI.Tests
{
    [TestFixture]
    public class WeatherControllerTests
    {
        private WeatherForecastController _controller;

        [SetUp]
        public void Setup()
        {
            // Arrange: Create a mock logger
            var mockLogger = new Mock<ILogger<WeatherForecastController>>();
            _controller = new WeatherForecastController(mockLogger.Object);
        }

        [Test]
        public void GetForUser_ReturnsWeatherForecast()
        {
            // Arrange: No additional setup needed

            // Act
            var result = _controller.GetForUser();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult?.Value);
            Assert.IsInstanceOf<IEnumerable<WeatherForecast>>(okResult.Value);

            IEnumerable<WeatherForecast>? weatherForecasts = (IEnumerable<WeatherForecast>)okResult.Value;
            Assert.That(weatherForecasts.Count(), Is.EqualTo(5));
        }

        [Test]
        public void GetForUser_ReturnsWeatherForecast_ForAdminUser()
        {
            // Arrange
            // Create a mock user with the "Admin" role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Set the user for the controller's HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = _controller.GetForUser();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult?.Value);
            Assert.IsInstanceOf<IEnumerable<WeatherForecast>>(okResult.Value);

            IEnumerable<WeatherForecast> weatherForecasts = okResult.Value as IEnumerable<WeatherForecast>;
            Assert.That(weatherForecasts.Count(), Is.EqualTo(5));
        }
    }
}
