using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace AuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private static readonly List<User> _users = new List<User>
        {
            new User { Id = 1, Username = "admin", Password = "admin", Role = "Admin" },
            new User { Id = 2, Username = "user", Password = "user", Role = "User" }
        };

        private const string SecretKey = "your_secret_key_here_1234567890_1234567890_1234567890_"; // 256-bit key
        private readonly SymmetricSecurityKey _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        private readonly ILogger<UserController> _logger;

        // Constructor with dependency injection
        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        // Register endpoint
        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            // Here you would typically hash the password before saving it
            user.Id = _users.Count + 1;
            _users.Add(user);
            _logger.LogInformation($"User '{user.Username}' registered successfully with ID: {user.Id}");
            return Created($"/user/{user.Id}", user);
        }

        // Login endpoint
        [HttpPost("login")]
        public IActionResult Login(UserCredential credentials)
        {
            var user = _users.FirstOrDefault(u => u.Username == credentials.Username && u.Password == credentials.Password);
            if (user == null)
            {
                _logger.LogWarning($"Failed login attempt for username: {credentials.Username}");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation($"User '{user.Username}' logged in successfully.");

            return Ok(new { User = user.Username, Token = tokenString });
        }

        // Get all users endpoint (only accessible by Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            _logger.LogInformation("Admin requested to get all users.");
            return Ok(_users);
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            _logger.LogInformation("Request received to get all users.");
            return Ok(_users.Where(e => e.Role == "User"));
        }
        [HttpGet("logs")]
        public IActionResult GetLogs(string level = null)
        {
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Utilities", "Logger", "log.txt");
            string logContent = System.IO.File.ReadAllText(logFilePath);

            // Split the log content into individual lines
            string[] logEntries = logContent.Split('\n');

            // Get distinct log levels
            var distinctLogLevels = logEntries
                .Select(entry =>
                {
                    string[] parts = entry.Split('[', ']');
                    return parts.Length >= 3 ? parts[1].Trim() : null;
                })
                .Where(level => !string.IsNullOrEmpty(level))
                .Distinct()
                .OrderBy(level => level)
                .ToList();

            // Create a StringBuilder to construct the HTML content
            StringBuilder htmlBuilder = new StringBuilder();

            // Start building the HTML content
            htmlBuilder.Append("<!DOCTYPE html>");
            htmlBuilder.Append("<html>");
            htmlBuilder.Append("<head>");
            htmlBuilder.Append("<title>Log Viewer</title>");
            htmlBuilder.Append("<link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css'>");
            htmlBuilder.Append("</head>");
            htmlBuilder.Append("<body>");

            // Add dropdown list for log levels with Bootstrap styling
            htmlBuilder.Append("<div class='container'>");
            htmlBuilder.Append("<form action='/api/user/logs' method='get'>");
            htmlBuilder.Append("<div class='form-group'>");
            htmlBuilder.Append("<div class='row mt-2'><label for='logLevel' class='col-3'>Select Log Level:</label>");
            htmlBuilder.Append("<select id='logLevel' name='level' class='form-control col-9' onchange='this.form.submit()'></div>");
            htmlBuilder.Append("<option value=''>All Levels</option>");
            foreach (var logLevel in distinctLogLevels)
            {
                htmlBuilder.Append($"<option value='{logLevel.ToLower()}'{(logLevel.Equals(level, StringComparison.OrdinalIgnoreCase) ? " selected" : "")}>{logLevel}</option>");
            }
            htmlBuilder.Append("</select>");
            htmlBuilder.Append("</div>");
            htmlBuilder.Append("</form>");

            // Add log entries with Bootstrap styling
            htmlBuilder.Append("<div class='mt-3'>");
            foreach (string entry in logEntries)
            {
                // Split each log entry into timestamp, log level, and message
                string[] parts = entry.Split('[', ']');

                if (parts.Length >= 3)
                {
                    // Extract timestamp, log level, and message
                    string timestamp = parts[0].Trim();
                    string levelString = parts[1].Trim();
                    string message = parts[2].Trim();

                    // Check if the log entry matches the specified log level filter
                    if (string.IsNullOrEmpty(level) || levelString.Equals(level, StringComparison.OrdinalIgnoreCase))
                    {
                        // Append timestamp, log level, and message to the HTML content with Bootstrap styling
                        htmlBuilder.Append($"<div class='alert alert-{GetBootstrapLogLevel(levelString)}' role='alert'>");
                        htmlBuilder.Append($"<span class='font-weight-bold'>{timestamp} [{levelString}]</span> {message}");
                        htmlBuilder.Append("</div>");
                    }
                }
                else
                {
                    // If the log entry format is incorrect, just append it as is
                    htmlBuilder.Append($"<p>{entry}</p>");
                }
            }
            htmlBuilder.Append("</div>");
            htmlBuilder.Append("</div>");

            // Finish building the HTML content
            htmlBuilder.Append("</body>");
            htmlBuilder.Append("</html>");

            return Content(htmlBuilder.ToString(), "text/html");
        }

        // Helper method to map log levels to Bootstrap alert classes
        private string GetBootstrapLogLevel(string level)
        {
            switch (level.ToLower())
            {
                case "error":
                    return "danger";
                case "warning":
                    return "warning";
                case "info":
                    return "info";
                case "success":
                    return "success";
                case "debug":
                    return "secondary";
                default:
                    return "light";
            }
        }

        [HttpGet("request-response-logs")]
        public IActionResult GetResandResLogs()
        {
            string logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Utilities", "ReqandResLogging");
            string logFilePath = Path.Combine(logFolderPath, "reqandres.txt");

            // Check if the log file exists
            if (!System.IO.File.Exists(logFilePath))
            {
                return Content("Log file not found.", "text/plain");
            }

            string logContent = System.IO.File.ReadAllText(logFilePath);

            // Add Bootstrap styling to the plain text log content
            string styledLogContent = $@"<div class='container'>
                                            <div class='card mt-3'>
                                                <div class='card-body'>
                                                    <pre>{logContent}</pre>
                                                </div>
                                            </div>
                                        </div>";

            return Content(styledLogContent, "text/html");
        }

    }
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class UserCredential
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
