using RinhaBackEnd.Test.Interfaces;

namespace RinhaBackEnd.Test;

public class ProgramFixture : WebApplicationFactory<Program>, IIntegrationTest, IDisposable
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

    protected override Microsoft.Extensions.Hosting.IHost CreateHost(IHostBuilder builder)
    {
        var appdb = new SqliteConnection(Configuration.GetConnectionString("PeopleDbConnection"));

        if (!File.Exists(appdb.DataSource))
        {
            var path = Path.Combine(Environment.CurrentDirectory, "App_Data");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        var app = base.CreateHost(builder);

        var dbContext = app.Services.GetRequiredService<PeopleDbContext>();

        dbContext.Database.EnsureCreated();

        return app;
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

    public async Task ClearDatabaseAsync()
    {
        using var scope = Server.Services.CreateScope();
        using var appDbContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();

        var people = await appDbContext.People.ToListAsync();
        var stack = await appDbContext.Stacks.ToListAsync();

        appDbContext.People.RemoveRange(people);

        await appDbContext.SaveChangesAsync();
    }
}
