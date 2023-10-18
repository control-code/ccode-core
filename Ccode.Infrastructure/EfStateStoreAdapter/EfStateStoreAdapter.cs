// Ignore Spelling: Ef

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ccode.Contracts.StateStoreAdapter;
using Ccode.Domain;

namespace Ccode.Infrastructure.StateStoreAdapter
{
	internal record EfEntity<TState>(Guid Uid, TState State);

	public class EfStateStoreAdapterConfig
	{
		public string ConnectionString { get; init; } = string.Empty;
	}

	public class EfStateStoreAdapter : IStateStoreAdapter
	{
		internal class EfContext : DbContext
		{
			private readonly string _connectionString;

			public EfContext()
			{
				_connectionString = "Server=.\\SQLExpress;Database=temp;Integrated Security=true;";
			}

			public EfContext(string connectionString)
			{
				_connectionString = connectionString;
			}

			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			{
				optionsBuilder.UseSqlServer(_connectionString);
				base.OnConfiguring(optionsBuilder);
			}
		}

		private readonly EfContext _dbContext;
		private readonly EfStateStoreAdapterConfig _config;

		public EfStateStoreAdapter(IOptions<EfStateStoreAdapterConfig> options) 
		{
			_config = options.Value;
			_dbContext = new EfContext(_config.ConnectionString);

		}

		public async Task<TState> Get<TState>(Guid uid) where TState : class
		{
			var set = _dbContext.Set<EfEntity<TState>>();
			var entity = await set.Where(e => e.Uid == uid).SingleAsync();
			return entity.State;
		}

		public async Task AddRoot<TRootState>(Guid uid, TRootState state, Context context) where TRootState : class
		{
			var entity = new EfEntity<TRootState>(uid, state);
			await _dbContext.Set<EfEntity<TRootState>>().AddAsync(entity);
			await _dbContext.SaveChangesAsync();
			_dbContext.ChangeTracker.Clear();
		}

		public async Task DeleteRoot<TRootState>(Guid uid, Context context)
		{
			var set = _dbContext.Set<EfEntity<TRootState>>();
			var entity = await set.Where(e => e.Uid == uid).SingleAsync();
			set.Remove(entity);
			await _dbContext.SaveChangesAsync();
		}
	}
}
