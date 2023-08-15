using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Requests;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using RinhaBackEnd.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = 10_000;
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    serverOptions.AllowSynchronousIO = true;
});

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("PeopleDbConnection")!, ServiceLifetime.Scoped);

builder.Services.AddSingleton<IConnectionMultiplexerPool>(options =>
{
    return ConnectionMultiplexerPoolFactory.Create(
                poolSize: 50,
                configuration: builder.Configuration.GetConnectionString("RedisConnection"),
                connectionSelectionStrategy: ConnectionSelectionStrategy.RoundRobin);
});

builder.Services.AddSingleton(options => new ConcurrentQueue<PersonResponse>());

builder.Services.AddSingleton(options => new ConcurrentDictionary<string, PersonResponse>());

builder.Services.AddHostedService<QueueConsumerHostedService>();

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.MapPost("/pessoas", async ([FromBody] PersonRequest? request,
                               [FromServices] NpgsqlConnection connection,
                               [FromServices] IConnectionMultiplexerPool redis,
                               [FromServices] ConcurrentQueue<PersonResponse> peopleToBeInserted) =>
{
    if (request == null) return Results.UnprocessableEntity(request);

    var person = new Person(request.Apelido!, request.Nome!, request.Nascimento!, request.Stack);

    if (!person.IsValid()) return Results.UnprocessableEntity(request);

    var pool = await redis.GetAsync();

    var db = pool.Connection.GetDatabase();

    var existedApelido = await db.StringGetAsync($"personApelido:{person.Apelido}");

    if (existedApelido.HasValue) return Results.UnprocessableEntity(request);

    var result = person.ToPersonResponse();

    peopleToBeInserted.Enqueue(result);

    var jsonResult = result.ToJson();
    await db.StringSetAsync(new KeyValuePair<RedisKey, RedisValue>[]
    {
        new($"personApelido:{person.Apelido}", "."),
        new($"personId:{person.Id}", jsonResult)
    });

    _ = db.KeyExpireAsync($"personApelido:{person.Apelido}", TimeSpan.FromMinutes(10));

    return Results.Created(new Uri($"/pessoas/{person.Id}", uriKind: UriKind.Relative), result);
});

app.MapGet("/pessoas/{id:guid}", async ([FromRoute(Name = "id")] Guid? id,
                                        [FromServices] NpgsqlConnection connection,
                                        [FromServices] IConnectionMultiplexerPool redis) =>
{
    if (id == null || Guid.Empty == id.Value) return Results.BadRequest();

    var pool = await redis.GetAsync();

    var db = pool.Connection.GetDatabase();

    var result = await db.StringGetAsync($"personId:{id}");

    if (result.HasValue) return Results.Text(result, contentType: "application/json");

    var queryResult = await connection.QueryFirstOrDefaultAsync<PersonResponseQuery>(@"SELECT
                                                                    ID, APELIDO, NOME, NASCIMENTO, STACK 
                                                                FROM 
                                                                    PEOPLE 
                                                                WHERE 
                                                                    ID = @ID", new { id },
                                                                    commandType: System.Data.CommandType.Text);

    if (queryResult == null) return Results.NotFound();

    await db.StringSetAsync($"personId:{id}", queryResult.ToJson());

    return Results.Ok(queryResult);
});

app.MapGet("/pessoas", async ([FromQuery(Name = "t")] string? search, [FromServices] NpgsqlConnection connection) =>
{
    if (string.IsNullOrWhiteSpace(search)) return Results.BadRequest();

    var query = @"SELECT
                      ID, APELIDO, NOME, NASCIMENTO, STACK 
                  FROM 
                      PEOPLE 
                  WHERE 
                      APELIDO LIKE @SEARCH
                      OR NOME LIKE @SEARCH
                      OR STACK LIKE @SEARCH
                      limit 50;";

    var result = await connection.QueryAsync<PersonResponse>(query, new { search = $"%{search}%" }, commandType: System.Data.CommandType.Text);
    return Results.Ok(result);

});

app.MapGet("/contagem-pessoas", async ([FromServices] NpgsqlConnection connection) =>
{
    return Results.Ok(await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM PEOPLE"));
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Use(next => context =>
{
    context.Request.EnableBuffering();
    return next(context);
});
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = context.Request.ContentType;
        context.Response.Body = context.Request.Body;
        return Task.CompletedTask;
    });
});

app.Run();


public partial class Program { }