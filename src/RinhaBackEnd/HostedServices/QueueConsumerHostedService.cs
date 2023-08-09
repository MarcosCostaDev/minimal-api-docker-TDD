using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using RinhaBackEnd.Infra.Contexts;
using System.Diagnostics;
using System;

namespace RinhaBackEnd.HostedServices;

public class QueueConsumerHostedService : IHostedService
{

    private readonly IConnectionMultiplexer _redis;
    private readonly IMapper _mapper;
    private readonly DbContextOptionsBuilder<PeopleDbContext> _buildOptions;

    public QueueConsumerHostedService(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration, IMapper mapper)
    {
        var buildOptions = new DbContextOptionsBuilder<PeopleDbContext>();

        _buildOptions = buildOptions.UseNpgsql(configuration.GetConnectionString("PeopleDbConnection"));

        _redis = connectionMultiplexer;
        _mapper = mapper;
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        var sub = _redis.GetSubscriber();

        sub.Subscribe("peopleInserted").OnMessage(async channelMessage =>
        {
            using var dbContext = new PeopleDbContext(_buildOptions.Options);
            var db = _redis.GetDatabase();
            channelMessage.Message.ToString();
            var json = await db.StringGetAsync(channelMessage.Message.ToString());
            var response = json.ToString().DeserializeTo<PersonResponse>();
            var person = _mapper.Map<Person>(response);

            if (response.Stack != null)
                foreach (var stackName in response.Stack)
                {
                    var stack = new Stack(stackName);

                    var personStack = new PersonStack(person, stack);

                    dbContext.PersonStacks.Add(personStack);
                }
            dbContext.Add(person);

            dbContext.SaveChanges();
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
