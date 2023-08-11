using RinhaBackEnd.Test.Interfaces;
using System.Runtime.InteropServices;

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
            _ = DownDockerCompose();
        }
    }

    public async Task ClearDatabaseAsync()
    {
        var connection = new NpgsqlConnection(Configuration.GetConnectionString("PeopleDbConnection"));

        await connection.ExecuteAsync("truncate table people;");
    }

    public async Task DownDockerCompose()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            System.Diagnostics.Process.Start("CMD.exe", "docker-compose rm -f & docker-compose down");
        else
            System.Diagnostics.Process.Start("bash", "docker-compose rm -f & docker-compose down");

        Thread.Sleep(3_000);
    }
}
