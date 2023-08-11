using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.HostedServices;

public class QueueConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public QueueConsumerHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var id = string.Empty;
        var consumerId = Guid.NewGuid().ToString()[..5];

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                using var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();

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

                using var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

                await connection.ExecuteAsync("INSERT INTO PEOPLE (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES (@ID, @APELIDO, @NOME, @NASCIMENTO, @STACK::jsonb)", new
                {
                    response.Id,
                    response.Apelido,
                    response.Nome,
                    response.Nascimento,
                    response.Stack
                }, commandType: System.Data.CommandType.Text);

            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error on redis stream");
            }
        }
    }
}
