using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.HostedServices
{
    public class SavingCacheHostedService : BackgroundService
    {
        private readonly ConcurrentDictionary<Guid, PersonResponse> _dictionary;
        private readonly IConnectionMultiplexerPool _redis;

        public SavingCacheHostedService(ConcurrentDictionary<Guid, PersonResponse> dictionary, IConnectionMultiplexerPool redis)
        {
            _dictionary = dictionary;
            _redis = redis;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pool = await _redis.GetAsync();

            var sub = pool.Connection.GetSubscriber();

            await sub.SubscribeAsync(EnvConsts.StreamName, (channel, message) =>
            {
                var personResponse = message.ToString().DeserializeTo<PersonResponse>();
                _dictionary.TryAdd(personResponse.Id, personResponse);
            });
        }
    }
}
