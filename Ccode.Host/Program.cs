using Microsoft.AspNetCore.Authentication.Cookies;
using Ccode.Contracts.StateQueryAdapter;
using Ccode.Contracts.StateStoreAdapter;
using Ccode.Infrastructure.MongoStateStoreAdapter;
using Ccode.Services.Identity;
using Microsoft.Extensions.DependencyInjection;
using Ccode.Contracts.StateEventAdapter;
using Ccode.Services.Users;

namespace Ccode.Host
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.Configure<MongoStateStoreAdapterConfig>(builder.Configuration.GetSection("MongoStateStoreAdapter"));
			builder.Services.AddSingleton<MongoStateStoreAdapter>();
			builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<MongoStateStoreAdapter>());
			builder.Services.AddSingleton<IStateStoreAdapter>(x => x.GetRequiredService<MongoStateStoreAdapter>());
			builder.Services.AddSingleton<IStateQueryAdapter>(x => x.GetRequiredService<MongoStateStoreAdapter>());
			builder.Services.AddSingleton<IStateEventAdapter>(x => x.GetRequiredService<MongoStateStoreAdapter>());
			builder.Services.AddSingleton<IdentityService>();
			builder.Services.AddSingleton<UsersService>();
			builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<UsersService>());

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}
	}
}