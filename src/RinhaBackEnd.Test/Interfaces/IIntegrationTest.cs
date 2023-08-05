namespace RinhaBackEnd.Test.Interfaces;

public interface IIntegrationTest
{
    HttpClient Client { get; }

    Task ClearDatabaseAsync();
}
