using RinhaBackEnd.Test.Interfaces;

namespace RinhaBackEnd.Test;

public class ContainerFixture : IIntegrationTest, IDisposable
{
    private IConfiguration Configuration;
    public ContainerFixture()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9999")
        };
        var builder = new ConfigurationBuilder()
                         .SetBasePath(Directory.GetCurrentDirectory())
                         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                         .AddJsonFile("appsettings.Benchmark.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();
    }
    private HttpClient _httpClient;
    public HttpClient Client
    {
        get
        {
            return _httpClient;
        }
        set
        {
            _httpClient = value;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Client.Dispose();
        }
    }

    public async Task ClearDatabaseAsync()
    {
        var buildOptions = new DbContextOptionsBuilder<PeopleDbContext>();

        buildOptions.UseNpgsql(Configuration.GetConnectionString("PeopleDbConnection"));

        using var appDbContext = new PeopleDbContext(buildOptions.Options);

        var people = await appDbContext.People.ToListAsync();
        var stack = await appDbContext.Stacks.ToListAsync();

        appDbContext.People.RemoveRange(people);

        await appDbContext.SaveChangesAsync();
    }
}
