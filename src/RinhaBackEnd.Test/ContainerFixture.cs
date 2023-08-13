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
            BaseAddress = new Uri("http://127.0.0.1:9999")
        };
        var builder = new ConfigurationBuilder()
                         .SetBasePath(Directory.GetCurrentDirectory())
                         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                         .AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();
        _ = DownDockerComposeAsync();
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
            _ = DownDockerComposeAsync();
        }
    }

    public async Task ClearDatabaseAsync()
    {
        using var connection = new NpgsqlConnection(Configuration.GetConnectionString("PeopleDbConnection"));

        await connection.ExecuteAsync(@"DO $$
	                                        BEGIN
		                                        IF (select Count(1) from information_schema.tables where table_name = 'people') >0 
		                                        THEN truncate table people;
		                                        END IF;
	                                        END;
	                                    $$");
    }

    public async Task DownDockerComposeAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            System.Diagnostics.Process.Start("CMD.exe", "docker-compose rm -f & docker-compose down");
        else
            System.Diagnostics.Process.Start("bash", "docker-compose rm -f & docker-compose down");

        Thread.Sleep(5_000);
    }
}
