namespace Ccode.Services.Identity
{
	public record IdentityState(string UserName, byte[] PasswordHash, byte[] Salt, IdentityStatus Status);
}
