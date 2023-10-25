using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Ccode.Contracts.StateQueryAdapter;
using Ccode.Contracts.StateStoreAdapter;

namespace Ccode.Services.Identity
{
	public interface IUserStorageAdapter
	{
		Task<long> AddUser(UserState state);
		Task<UserState> GetUser(string username);
	}

	public record UserState(string UserName, byte[] PasswordHash, byte[] Salt);

	public class IdentityService
	{
		private readonly IStateStoreAdapter _store;
		private readonly IStateQueryAdapter _query;

		public IdentityService(IStateStoreAdapter store, IStateQueryAdapter query)
		{
			_store = store;
			_query = query;
		}

		public async Task RegisterUser(string userName, string password, Domain.Context context)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("UserName cannot be empty", nameof(userName));

			if (string.IsNullOrWhiteSpace(password))
				throw new ArgumentException("Password cannot be empty", nameof(password));

			if (userName.Length < 3)
				throw new ArgumentException("UserName must be at least 3 characters long", nameof(userName));

			if ((await _query.GetUids<UserState>(nameof(UserState.UserName), userName)).Any())
				throw new ArgumentException("UserName already exists", nameof(userName));

			if (password.Length < 8)
				throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

			var user = HashUserPassword(userName, password);
			var uid = Guid.NewGuid();
			await _store.AddRoot(uid, user, context);
		}

		public async Task<AuthentificationResult> AuthenticateUser(string userName, string password, Domain.Context context)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("UserName cannot be empty", nameof(userName));

			if (string.IsNullOrWhiteSpace(password))
				throw new ArgumentException("Password cannot be empty", nameof(password));

			var users = await _query.Get<UserState>("userName", userName);

			if (!users.Any())
				return AuthentificationResult.InvalidUsername;

			if (users.Count() > 1)
				throw new InvalidOperationException("Multiple users with the same username");

			var user = users.Single();

			if (!VerifyUserPassowrd(user.State, password))
				return AuthentificationResult.InvalidPassword;

			return AuthentificationResult.Success;
		}

		private UserState HashUserPassword(string username, string password)
		{
			byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

			var hashed = KeyDerivation.Pbkdf2(
				password: password,
				salt: salt,
				prf: KeyDerivationPrf.HMACSHA256,
				iterationCount: 100000,
				numBytesRequested: 256 / 8);

			return new UserState(username, hashed, salt);
		}

		private bool VerifyUserPassowrd(UserState user, string password)
		{
			var hashed = KeyDerivation.Pbkdf2(
				password: password,
				salt: user.Salt,
				prf: KeyDerivationPrf.HMACSHA256,
				iterationCount: 100000,
				numBytesRequested: 256 / 8);
			return hashed.SequenceEqual(user.PasswordHash);
		}
	}
}
