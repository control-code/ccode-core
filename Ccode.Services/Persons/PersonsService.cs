using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccode.Services.Persons
{
	public record PersoneState(string FirstName, string? SecondName, DateTime? DateOfBirth, Guid? identityUid);

	internal class PersonsService
	{
	}
}
