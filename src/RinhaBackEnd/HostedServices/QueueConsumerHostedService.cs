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
        using var scope = _serviceProvider.CreateScope();

        while (!stoppingToken.IsCancellationRequested)
        {
            var queue = _serviceProvider.GetRequiredService<ConcurrentQueue<PersonResponse>>();
            var peopleInQueue = queue.Dequeue(100).ToArray();

            if (!peopleInQueue.Any())
            {
               await Task.Delay(50, stoppingToken);
                continue;
            }
            NpgsqlConnection connection = null!;
            try
            {
                connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
                await connection.OpenAsync(stoppingToken);

                await using var batch = new NpgsqlBatch(connection);

                for (int i = 0; i < peopleInQueue.Length; i++)
                {
                    var cmd = new NpgsqlBatchCommand("INSERT INTO PESSOAS (ID, APELIDO, NOME, NASCIMENTO, STACK) VALUES ($1, $2, $3, $4, $5)");
                    cmd.Parameters.AddWithValue(peopleInQueue[i].Id);
                    cmd.Parameters.AddWithValue(peopleInQueue[i].Apelido);
                    cmd.Parameters.AddWithValue(peopleInQueue[i].Nome);
                    cmd.Parameters.AddWithValue(peopleInQueue[i].Nascimento);
                    cmd.Parameters.AddWithValue(peopleInQueue[i].Stack == null ? DBNull.Value : peopleInQueue[i].Stack);
                    batch.BatchCommands.Add(cmd);
                }

                await batch.ExecuteNonQueryAsync(stoppingToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Postgres error on insert");
            }
            finally
            {
                connection?.Close();
            }
        }
    }
}
