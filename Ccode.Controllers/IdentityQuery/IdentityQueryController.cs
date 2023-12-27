using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ccode.Services.Identity;
using Ccode.Contracts.StateQueryAdapter;

namespace Ccode.Controllers.IdentityQuery
{
	public record ColumnDefinition(string FieldName, string Title);

	public record Response<T>(IEnumerable<ColumnDefinition> Columns, IEnumerable<EntityState<T>> Rows) where T : class;

	[ApiController]
	[Route("api/identity-query")]
	public class IdentityQueryController : ControllerBase
	{
		public record UserInfo(string UserName, IdentityStatus Status);

		private readonly IdentityService _service;

		public IdentityQueryController(IdentityService service)
		{
			_service = service;
		}

		[Authorize]
		[HttpGet("")]
		public async Task<IActionResult> GetAll()
		{
			var context = new Domain.Context(Guid.Empty, Guid.NewGuid(), -1);
			var users = await _service.GetAllUsers(context);

			if (users == null)
			{
				return Unauthorized();
			}

			var response = new Response<UserInfo>(
				new List<ColumnDefinition>
				{
					new ColumnDefinition(nameof(IdentityState.UserName), "User Name"),
					new ColumnDefinition(nameof(IdentityState.Status), "Status")
				},
				users.Select(u => new EntityState<UserInfo>(u.Uid, new UserInfo(u.State.UserName, u.State.Status)))
			);

			return Ok(response);
		}
	}
}
