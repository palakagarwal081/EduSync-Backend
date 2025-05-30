using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduSync.Backend.Data;
using EduSync.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using EduSync.Backend.DTOs;

namespace EduSync.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ProjectDBContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ProjectDBContext context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _hasher = new PasswordHasher<User>();
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {dto.Email}");
                var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User not found for email {dto.Email}");
                    return Unauthorized("User not found");
                }

                var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning($"Login failed: Invalid password for user {dto.Email}");
                    return Unauthorized("Invalid password");
                }

                _logger.LogInformation($"User authenticated successfully: {user.Email}");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var keyString = _config["Jwt:Key"];
                if (string.IsNullOrEmpty(keyString))
                {
                    _logger.LogError("JWT Key is not configured");
                    return StatusCode(500, "JWT Key is not configured");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds);

                _logger.LogInformation($"JWT token generated for user {user.Email}");

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    role = user.Role,
                    userId = user.UserId,
                    name = user.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "An error occurred during login");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                _logger.LogInformation("Starting registration process");
                
                if (dto == null)
                {
                    _logger.LogWarning("Registration attempt with null DTO");
                    return BadRequest("Invalid registration data");
                }

                // Validate input
                if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(dto.Name))
                {
                    _logger.LogWarning("Registration attempt with missing required fields");
                    return BadRequest("Email, password, and name are required");
                }

                // Validate email format
                if (!new EmailAddressAttribute().IsValid(dto.Email))
                {
                    _logger.LogWarning($"Registration attempt with invalid email format: {dto.Email}");
                    return BadRequest("Invalid email format");
                }

                // Validate password length
                if (dto.Password.Length < 6)
                {
                    _logger.LogWarning("Registration attempt with password too short");
                    return BadRequest("Password must be at least 6 characters long");
                }

                // Validate password confirmation
                if (dto.Password != dto.ConfirmPassword)
                {
                    _logger.LogWarning("Registration attempt with mismatched passwords");
                    return BadRequest("The password and confirmation password do not match");
                }

                // Validate role
                if (string.IsNullOrEmpty(dto.Role) || (dto.Role != "Student" && dto.Role != "Instructor"))
                {
                    _logger.LogWarning($"Registration attempt with invalid role: {dto.Role}");
                    return BadRequest("Role must be either 'Student' or 'Instructor'");
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    _logger.LogWarning($"Registration attempt with existing email: {dto.Email}");
                    return BadRequest("User with this email already exists");
                }

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Name = dto.Name,
                    Email = dto.Email,
                    PasswordHash = _hasher.HashPassword(null, dto.Password),
                    Role = dto.Role
                };

                _context.Users.Add(user);

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"User registered successfully: {user.Email}");

                    // Generate JWT token
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var keyString = _config["Jwt:Key"];
                    if (string.IsNullOrEmpty(keyString))
                    {
                        _logger.LogError("JWT Key is not configured");
                        return StatusCode(500, "JWT Key is not configured");
                    }

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: _config["Jwt:Issuer"],
                        audience: _config["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: creds);

                    // Return token and redirect information
                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        role = user.Role,
                        userId = user.UserId,
                        name = user.Name,
                        redirectTo = user.Role == "Student" ? "/student-dashboard" : "/instructor-dashboard"
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error during registration. Inner exception: {InnerException}", dbEx.InnerException?.Message);
                    return StatusCode(500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration. Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterUserDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [MinLength(6)]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }
    }
}