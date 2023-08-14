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
        string consumerId = Guid.NewGuid().ToString()[..5];
        using var scope = _serviceProvider.CreateScope();

        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexerPool>();
        var pool = await redis.GetAsync();
        var db = pool.Connection.GetDatabase();

        if (!(await db.KeyExistsAsync(EnvConsts.StreamName)) || (await db.StreamGroupInfoAsync(EnvConsts.StreamName)).All(x => x.Name != EnvConsts.StreamGroupName))
        {
            await db.StreamCreateConsumerGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, "0-0", true);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Thread.Sleep(1_000);

                var streamResult = await db.StreamReadGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, consumerId, count: 10);

                if (!streamResult.Any())
                {
                    Thread.Sleep(2_000);
                    continue;
                }

                db = pool.Connection.GetDatabase();

                var insertItems = streamResult.Select(item => {
                    var response = item[EnvConsts.StreamPersonKey].ToString().DeserializeTo<PersonResponse>();
                    return new
                    {
                        response.Id,
                        response.Apelido,
                        response.Nome,
                        response.Nascimento,
                        Stack = response.GetStack(),
                    };
                });

                var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

                await connection.ExecuteAsync("INSERT INTO PEOPLE (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES (@ID, @APELIDO, @NOME, @NASCIMENTO, @STACK)", insertItems, commandType: System.Data.CommandType.Text);

                await db.StreamAcknowledgeAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, streamResult.Select(p => p.Id).ToArray());
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Error on Postgres");
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Error on Redis");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "WTF WAS THIS?!");
            }
        }
    }
}
