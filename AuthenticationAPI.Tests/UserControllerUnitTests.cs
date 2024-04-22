using AuthenticationAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AuthenticationAPI.Tests
{
    [TestFixture]
    public class UserControllerUnitTests
    {
        private UserController _controller;
        private List<User> _users;

        [SetUp]
        public void Setup()
        {
            // Arrange: Create a mock logger
            var mockLogger = new Mock<ILogger<UserController>>();
            _controller = new UserController(mockLogger.Object);

            // Arrange: Create a mock controller and initialize users
            _users = new List<User>
            {
                 new User { Id = 1, Username = "admin", Password = "admin", Role = "Admin" },
                 new User { Id = 2, Username = "user", Password = "user", Role = "User" }
            };
        }
       
        [Test]
        public void Register_ValidUser_ReturnsCreated()
        {
            // Arrange
            var newUser = new User { Username = "testuser", Password = "testpassword", Role = "user" };

            // Act
            var result = _controller.Register(newUser) as CreatedResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual($"/user/{newUser.Id}", result.Location);
            Assert.AreEqual(newUser, result.Value);
        }

        [Test]
        public void Register_InvalidUser_ReturnsBadRequest()
        {
            // Arrange
            var invalidUser = new User { Username = "invaliduser" }; // Missing password and role

            // Act
            var result = _controller.Register(invalidUser) as BadRequestResult;

            // Assert
            Assert.Null(result);
        }

        [Test]
        public void Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var validCredentials = new UserCredential { Username = "user", Password = "user" };

            // Act
            var result = _controller.Login(validCredentials) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.ToString().Contains("Token"));
        }

        [Test]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var invalidCredentials = new UserCredential { Username = "invaliduser", Password = "wrongpassword" };

            // Act
            var result = _controller.Login(invalidCredentials) as UnauthorizedObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("{ message = Invalid username or password }", result.Value?.ToString());
        }

        [Test]
        public void GetAllUsers_AdminRole_ReturnsOkWithUsers()
        {
            // Arrange
            var mockClaimsPrincipal = new Mock<ClaimsPrincipal>();
            mockClaimsPrincipal.Setup(cp => cp.IsInRole("Admin")).Returns(true);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = mockClaimsPrincipal.Object } };

            // Act
            var result = _controller.GetAllUsers() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsInstanceOf<List<User>>(result.Value);
        }
    }
}