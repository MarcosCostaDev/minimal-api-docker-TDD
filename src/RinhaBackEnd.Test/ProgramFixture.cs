using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace RinhaBackEnd.Test;

public class ProgramFixture : WebApplicationFactory<Program>, IDisposable
{
    private HttpClient _httpClient;
    public HttpClient Client
    {
        get
        {
            if (_httpClient == null)
                _httpClient = base.Server.CreateClient();
            return _httpClient;
        }
        set
        {
            _httpClient = value;
        }
    }
    private IConfiguration Configuration;

    public ProgramFixture() : base()
    {
        var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();


        DropTestDatabases();
        CreateDatabaseFolder();
    }

    private void CreateDatabaseFolder()
    {
        var appdb = new SqliteConnection(Configuration.GetConnectionString("PeopleDbConnection"));
        if (!File.Exists(appdb.DataSource))
        {
            var path = Path.Combine(Environment.CurrentDirectory, "App_Data");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
    private void DropTestDatabases()
    {
        var appdb = new SqliteConnection(Configuration.GetConnectionString("PeopleDbConnection"));

        if (File.Exists(appdb.DataSource))
            File.Delete(appdb.DataSource);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Environment.CurrentDirectory)
                        .UseEnvironment("Testing")
                        .UseConfiguration(Configuration);

        builder.ConfigureTestServices(services =>
        {
            var serviceProvider = services.BuildServiceProvider();

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PeopleDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<PeopleDbContext>(options =>
            {
                var sqliteConnectionString = Configuration.GetConnectionString("PeopleDbConnection");

                sqliteConnectionString = sqliteConnectionString.Replace("./", Environment.CurrentDirectory + "/", StringComparison.OrdinalIgnoreCase);

                options.UseSqlite(sqliteConnectionString);

            }, ServiceLifetime.Scoped);

            var cacheServiceMock = new Mock<IDistributedCache>();
            services.Replace(ServiceDescriptor.Singleton(typeof(IDistributedCache), _ => cacheServiceMock.Object));
        });

        base.ConfigureWebHost(builder);
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var appdb = new SqliteConnection(Configuration.GetConnectionString("PeopleDbConnection"));

        if (!File.Exists(appdb.DataSource))
        {
            var path = Path.Combine(Environment.CurrentDirectory, "App_Data");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        return base.CreateHost(builder);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Server.Dispose();
            Client.Dispose();
        }
    }

}

[CollectionDefinition("API", DisableParallelization = true)]
public class StartupFixtureCollection : ICollectionFixture<ProgramFixture>
{
}