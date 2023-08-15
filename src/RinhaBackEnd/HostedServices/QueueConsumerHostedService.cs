using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

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
        
        using var scope = _serviceProvider.CreateScope();
    
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1_000, stoppingToken);
            NpgsqlConnection connection = null!;
            try
            {
                var queue = _serviceProvider.GetRequiredService<ConcurrentQueue<PersonResponse>>();
                var peopleInQueue = queue.Dequeue(60).ToList();

                if (!peopleInQueue.Any())
                {
                    await Task.Delay(4_000, stoppingToken);
                    continue;
                }

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

                await batch.ExecuteNonQueryAsync(stoppingToken);
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
