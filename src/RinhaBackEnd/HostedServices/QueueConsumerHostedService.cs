using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using System.Collections.Concurrent;

namespace RinhaBackEnd.HostedServices;

public class QueueConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueConsumerHostedService> _logger;

    public QueueConsumerHostedService(IServiceProvider serviceProvider, ILogger<QueueConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string consumerId = Guid.NewGuid().ToString()[..5];
        using var scope = _serviceProvider.CreateScope();

        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexerPool>();
        var pool = await redis.GetAsync();
        var db = pool.Connection.GetDatabase();
        var queue = scope.ServiceProvider.GetRequiredService<ConcurrentQueue<PersonResponse>>();

        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(1_000);
            NpgsqlConnection connection = null;
            try
            {
                var peopleInQueue = queue.Dequeue(20);

                if (!peopleInQueue.Any())
                {
                    Thread.Sleep(2_000);
                    continue;
                }

                db = pool.Connection.GetDatabase();

                connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
                await connection.OpenAsync(stoppingToken);

                await using var batch = new NpgsqlBatch(connection);

                for (int i = 0; i < peopleInQueue.Count(); i++)
                {
                    var response = peopleInQueue.ElementAt(i);
                    var cmd = new NpgsqlBatchCommand("INSERT INTO PEOPLE (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES ($1, $2, $3, $4, $5)");
                    cmd.Parameters.AddWithValue(response.Id);
                    cmd.Parameters.AddWithValue(response.Apelido);
                    cmd.Parameters.AddWithValue(response.Nome);
                    cmd.Parameters.AddWithValue(response.Nascimento);
                    cmd.Parameters.AddWithValue(response.GetStack());
                    batch.BatchCommands.Add(cmd);
                }

                _ = batch.ExecuteNonQueryAsync(stoppingToken);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Error on Postgres");
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Error on Redis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WTF WAS THIS?!");
            }
            finally
            {
                connection?.Close();
            }
        }
    }

}
