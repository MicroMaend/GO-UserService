using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GOCore;
using System.Linq;
using System.Threading.Tasks;
using Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Security.Claims;

namespace UserService.Controllers
{
    [ApiController]
    [Route("user")]
    [Authorize] // Kræver et gyldigt JWT for alle endpoints i denne controller
    public class UserController : ControllerBase
    {
        private readonly IUserDBService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserDBService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Hent bruger baseret på ID
        [HttpGet("{userId}", Name = "GetUserById")]
        public async Task<IActionResult> GetById(Guid userId)
        {
            _logger.LogInformation("Henter bruger med ID: {UserId}", userId);
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Bruger med ID {UserId} blev ikke fundet", userId);
                return NotFound(new { message = "User not found" });
            }

            // Tjek om den aktuelle bruger er admin eller forsøger at hente sin egen bruger
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin") || currentUserId == userId.ToString())
            {
                return Ok(user); // Returner brugerdata
            }
            else
            {
                return Forbid(); // Returner 403 Forbidden, hvis brugeren ikke har adgang
            }
        }

        // Hent bruger baseret på brugernavn
        [HttpGet("name/{userName}", Name = "GetUserByUserName")]
        public async Task<IActionResult> GetByUserName(string userName)
        {
            _logger.LogInformation("Henter bruger med navn: {UserName}", userName);
            var user = await _userService.GetUserByNameAsync(userName);

            if (user == null)
            {
                _logger.LogWarning("Bruger med navn {UserName} blev ikke fundet", userName);
                return NotFound(new { message = "User not found" });
            }
            //kun admin kan hente brugere ud fra navn.
            if (User.IsInRole("Admin"))
            {
                return Ok(user);
            }
            else
            {
                return Forbid();
            }

        }

        // Opret bruger - Alle kan tilgå dette endpoint
        [HttpPost("add")]
        [AllowAnonymous]
        public async Task<IActionResult> Add([FromBody] User user)
        {
            user.Id = Guid.NewGuid();
            _logger.LogInformation("Tilføjede ny bruger: {UserName}, ID: {UserId}", user.Name, user.Id);
            var createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtRoute("GetUserById", new { userId = createdUser.Id }, createdUser);
        }

        // Hent alle brugere - Kun admins
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("Henter alle brugere.");
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // Opdater bruger - Admin eller brugeren selv
        [HttpPut("{userId}")]
        public async Task<IActionResult> Update(Guid userId, [FromBody] User updatedUser)
        {
            _logger.LogInformation("Opdaterer bruger med ID: {UserId}", userId);
            updatedUser.Id = userId;

            // Tjek om den aktuelle bruger er admin eller forsøger at opdatere sin egen bruger
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin") || currentUserId == userId.ToString())
            {
                var updated = await _userService.UpdateUserAsync(userId.ToString(), updatedUser);
                if (updated == null)
                {
                    _logger.LogWarning("Bruger med ID {UserId} blev ikke opdateret", userId);
                    return NotFound(new { message = "User not found or could not be updated" });
                }
                _logger.LogInformation("Bruger med ID {UserId} blev opdateret", userId);
                return Ok(updated);
            }
            else
            {
                return Forbid(); // Returner 403 Forbidden
            }
        }

        // Slet bruger - Kun admins
        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")] // Kun brugere i "Admin" rollen kan tilgå dette endpoint
        public async Task<IActionResult> Delete(Guid userId)
        {
            _logger.LogInformation("Sletter bruger med ID: {UserId}", userId);
            var success = await _userService.DeleteUserAsync(userId.ToString());

            if (!success)
            {
                _logger.LogWarning("Bruger med ID {UserId} blev ikke slettet", userId);
                return NotFound(new { message = "User not found or could not be deleted" });
            }

            _logger.LogInformation("Bruger med ID {UserId} blev slettet", userId);
            return NoContent();
        }

        // Version info - Alle autentificerede brugere kan se version
        [HttpGet("version")]
        public async Task<IActionResult> GetVersion()
        {
            var properties = new Dictionary<string, string>
            {
                { "service", "HaaV User Service" }
            };

            try
            {
                var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
                properties.Add("version", ver ?? "unknown");

                var hostName = System.Net.Dns.GetHostName();
                var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
                var ipa = ips.FirstOrDefault()?.MapToIPv4().ToString() ?? "unknown";

                properties.Add("hosted-at-address", ipa);
                _logger.LogInformation("Version info hentet: {Version}, Hosted p: {IPAddress}", ver, ipa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af version info");
                properties.Add("hosted-at-address", "Could not resolve IP-address");
            }

            return Ok(properties);
        }
    }
}