using Microsoft.Extensions.Hosting;
using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using System.Threading;

namespace RinhaBackEnd.HostedServices;

public class QueueConsumerHostedService : BackgroundService
{

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QueueConsumerHostedService> _logger;
    private readonly NpgsqlConnection _connection;

    public QueueConsumerHostedService(IConnectionMultiplexer connectionMultiplexer, NpgsqlConnection connection)
    {
        _connection = connection;
        _redis = connectionMultiplexer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        var id = string.Empty;
        var consumerId = Guid.NewGuid().ToString()[..5];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var db = _redis.GetDatabase();

                if (!string.IsNullOrEmpty(id))
                {
                    await db.StreamAcknowledgeAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, id);
                    id = string.Empty;
                }

                if (!(await db.KeyExistsAsync(EnvConsts.StreamName)) || (await db.StreamGroupInfoAsync(EnvConsts.StreamName)).All(x => x.Name != EnvConsts.StreamGroupName))
                {
                    await db.StreamCreateConsumerGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, "0-0", true);
                }

                var streamResult = await db.StreamReadGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, consumerId);

                if (!streamResult.Any()) continue;

                id = streamResult.First().Id;

                var json = await db.StringGetAsync(streamResult.First()[EnvConsts.StreamPersonKey].ToString());
                var response = json.ToString().DeserializeTo<PersonResponse>();

                await _connection.ExecuteAsync("INSERT INTO PEOPLE (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES (@ID, @APELIDO, @NOME, @NASCIMENTO, @STACKS::jsonb)", response, commandType: System.Data.CommandType.Text);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error on redis stream");
            }
        }
    }
}
