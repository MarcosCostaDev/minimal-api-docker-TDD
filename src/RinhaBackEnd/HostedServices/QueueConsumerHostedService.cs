﻿using RinhaBackEnd.Dtos.Response;
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
        var id = string.Empty;
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
                Thread.Sleep(5_000);

                //db ??= pool.Connection.GetDatabase();

                if (!string.IsNullOrWhiteSpace(id))
                {
                    await db.StreamAcknowledgeAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, id);
                    id = string.Empty;
                }

                var streamResult = await db.StreamReadGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, consumerId);

                if (!streamResult.Any())
                {
                    Thread.Sleep(1_000);
                    continue;
                }

                id = streamResult.First().Id;

                var response = streamResult.First()[EnvConsts.StreamPersonKey].ToString().DeserializeTo<PersonResponse>();

                var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

                await connection.ExecuteAsync("INSERT INTO PEOPLE (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES (@ID, @APELIDO, @NOME, @NASCIMENTO, @STACK::jsonb)", new
                {
                    response.Id,
                    response.Apelido,
                    response.Nome,
                    response.Nascimento,
                    Stack = response.GetStack(),
                }, commandType: System.Data.CommandType.Text);
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
