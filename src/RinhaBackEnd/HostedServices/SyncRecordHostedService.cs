using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.HostedServices
{
    public class SyncRecordHostedService : BackgroundService
    {
        private readonly ConcurrentDictionary<Guid, PersonResponse> _localRecords;
        private readonly IConnectionMultiplexerPool _redis;

        public SyncRecordHostedService(ConcurrentDictionary<Guid, PersonResponse> localRecords, IConnectionMultiplexerPool redis)
        {
            _localRecords = localRecords;
            _redis = redis;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pool = await _redis.GetAsync();

            var subscriber = pool.Connection.GetSubscriber();

            subscriber.Subscribe("added-record", (channel, message) =>
            {
                var response = message.ToString().DeserializeTo<PersonResponse>();
                _localRecords.TryAdd(response.Id, response);
            });
        }
    }
}
