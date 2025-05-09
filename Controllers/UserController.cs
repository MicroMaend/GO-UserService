using Microsoft.AspNetCore.Mvc;
using GOCore;
using System.Linq;
using System.Threading.Tasks;
using Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserDBService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserDBService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Hent bruger baseret på UserName
        [HttpGet("{UserName}", Name = "GetCustomerByUserName")]
        public async Task<IActionResult> Get(Customer UserName)
        {
            _logger.LogInformation("Henter bruger med ID: {UserName}", UserName);
            var customer = await _userService.GetCustomerByIdAsync(UserName.ToString());

            if (customer == null)
            {
                _logger.LogWarning("Bruger med ID {customerId} blev ikke fundet", UserName);
                return NotFound(new { message = "User not found" });
            }

            return Ok(customer);
        }

        // Opret bruger
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] Customer customer)
        {
            customer.Id = Guid.NewGuid(); // Generer ID automatisk
            _logger.LogInformation("Tilføjede ny bruger: {UserName}, ID: {customerId}", customer.Name, customer.Id);
            var createdCustomer = await _userService.CreateCustomerAsync(customer);

            return CreatedAtRoute("GetCustomerById", new { customerId = createdCustomer.Id }, createdCustomer);
        }

        // Hent alle brugere
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCustomers()
        {
            _logger.LogInformation("Henter alle brugere.");
            var customers = await _userService.GetAllCustomersAsync();
            return Ok(customers);
        }

        // Opdater bruger
        [HttpPut("{customerId}")]
        public async Task<IActionResult> Update(Guid customerId, [FromBody] Customer updatedCustomer)
        {
            _logger.LogInformation("Opdaterer bruger med ID: {customerId}", customerId);
            updatedCustomer.Id = customerId; // Sørg for at ID er korrekt, når den opdateres

            var updated = await _userService.UpdateCustomerAsync(customerId.ToString(), updatedCustomer);
            if (updated == null)
            {
                _logger.LogWarning("Bruger med ID {UserId} blev ikke opdateret", customerId);
                return NotFound(new { message = "User not found or could not be updated" });
            }

            _logger.LogInformation("Bruger med ID {UserId} blev opdateret", customerId);
            return Ok(updated);
        }

        // Slet bruger
        [HttpDelete("{customerId}")]
        public async Task<IActionResult> Delete(Guid customerId)
        {
            _logger.LogInformation("Sletter bruger med ID: {UserId}", customerId);
            var success = await _userService.DeleteCustomerAsync(customerId.ToString());

            if (!success)
            {
                _logger.LogWarning("Bruger med ID {UserId} blev ikke slettet", customerId);
                return NotFound(new { message = "User not found or could not be deleted" });
            }

            _logger.LogInformation("Bruger med ID {UserId} blev slettet", customerId);
            return NoContent(); // Returner 204 No Content når brugeren er slettet
        }

        // Hent version info
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
                _logger.LogInformation("Version info hentet: {Version}, Hosted på: {IPAddress}", ver, ipa);
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