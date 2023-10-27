using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Ccode.Services.Identity;

namespace Ccode.Controllers.Identity
{
	[ApiController]
	[Route("api/identity")]
	public class IdentityController : ControllerBase
	{
		public record RegisterUserRequest(string UserName, string Password);
		public record AuthentificationUserRequest(string UserName, string Password);

		private readonly ILogger<IdentityController> _logger;
		private readonly IdentityService _service;

		public IdentityController(IdentityService service, ILogger<IdentityController> logger)
		{
			_logger = logger;
			_service = service;
		}

		[HttpPost("register")]
		public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
		{
			var context = new Domain.Context(Guid.Empty, Guid.NewGuid());
			try
			{
				await _service.RegisterUser(request.UserName, request.Password, context);
				return Ok();
			}
			catch (ArgumentException e)
			{
				return BadRequest(e.Message);
			}
		}

		[HttpPost("authenticate")]
		public async Task<IActionResult> AuthenticateUser([FromBody] AuthentificationUserRequest request)
		{
			var context = new Domain.Context(Guid.Empty, Guid.NewGuid());
			try
			{
				var result = await _service.AuthenticateUser(request.UserName, request.Password, context);
				if (result != AuthentificationResult.Success)
				{
					return Unauthorized();
				}

				// Create a cookie to store user authentication information.
				var claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, request.UserName),
				};

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

				var authProperties = new AuthenticationProperties
				{
					IsPersistent = true,
				};

				await HttpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					new ClaimsPrincipal(claimsIdentity),
					authProperties
				);

				return Ok();
			}
			catch (ArgumentException e)
			{
				return BadRequest(e.Message);
			}
		}

		[Authorize]
		[HttpPost("logout")]
		public Task<IActionResult> Logout()
		{
			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return Task.FromResult<IActionResult>(Ok());
		}
	}
}
