﻿using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Ccode.Services.Identity
{
	public interface IUserStorageAdapter
	{
		Task<long> AddUser(UserState state);
		Task<UserState> GetUser(string username);
	}

	public record UserState(string Username, byte[] PasswordHash, byte[] Salt);

	public class IdentityService
	{
		private readonly IUserStorageAdapter _userStorageAdapter;

		public IdentityService(IUserStorageAdapter userStorageAdapter)
		{
			_userStorageAdapter = userStorageAdapter;
		}

		public Task RegisterUser(string username, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
				throw new ArgumentException("Username cannot be empty", nameof(username));

			if (string.IsNullOrWhiteSpace(password))
				throw new ArgumentException("Password cannot be empty", nameof(password));

			if (username.Length < 3)
				throw new ArgumentException("Username must be at least 3 characters long", nameof(username));

			if (_userStorageAdapter.GetUser(username) != null)
				throw new ArgumentException("Username already exists", nameof(username));

			if (password.Length < 8)
				throw new ArgumentException("Password must be at least 8 characters long", nameof(password));


			var user = HashUserPassword(username, password);
			return _userStorageAdapter.AddUser(user);
		}

		public async Task<AuthentificationResult> AuthenticateUser(string username, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
				throw new ArgumentException("Username cannot be empty", nameof(username));

			if (string.IsNullOrWhiteSpace(password))
				throw new ArgumentException("Password cannot be empty", nameof(password));

			var user = await _userStorageAdapter.GetUser(username);

			if (user == null)
				return AuthentificationResult.InvalidUsername;

			if (!VerifyUserPassowrd(user, password))
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
			return hashed == user.PasswordHash;
		}
	}
}