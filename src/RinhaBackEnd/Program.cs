using RinhaBackEnd;
using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Requests;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using RinhaBackEnd.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("PeopleDbConnection"), ServiceLifetime.Singleton);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(options => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")));

builder.Services.AddHostedService<QueueConsumerHostedService>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapGet("/ping", () => "pong");

app.MapPost("/pessoas", async ([FromBody] PersonRequest request, NpgsqlConnection connection, IConnectionMultiplexer redis) =>
{
    var contract = new Contract<Notification>();

    var person = new Person(request.Apelido, request.Nome, request.Nascimento, request.Stack);

    contract.AddNotifications(person);

    if (!contract.IsValid) return Results.UnprocessableEntity(request);

    var result = person.ToPersonResponse();
    try
    {
        var db = redis.GetDatabase();
        //var sub = redis.GetSubscriber();
        db.StringSet($"{person.Id}", person.ToPersonResponse().ToJson());
        //await sub.PublishAsync("peopleInserted", person.Id.ToString());

        if (!(await db.KeyExistsAsync(EnvConsts.StreamName)) || (await db.StreamGroupInfoAsync(EnvConsts.StreamName)).All(x => x.Name != EnvConsts.StreamGroupName))
        {
            await db.StreamCreateConsumerGroupAsync(EnvConsts.StreamName, EnvConsts.StreamGroupName, "0-0", true);
        }

        await db.StreamAddAsync(EnvConsts.StreamName, new NameValueEntry[] { new(EnvConsts.StreamPersonKey, person.Id.ToString()) });
    }
    catch (Exception)
    {
        return Results.UnprocessableEntity(request);
    }
    return Results.Created(new Uri($"/pessoas/{person.Id}", uriKind: UriKind.Relative), result);
});

app.MapGet("/pessoas/{id:guid}", async ([FromRoute(Name = "id")] Guid id, NpgsqlConnection connection, IConnectionMultiplexer redis) =>
{
    var result = await redis.GetOrCreateStringAsync(id.ToString(), async () =>
    {

        var result = connection.QueryFirstOrDefault<Person>(@"SELECT
                                                                    ID, APELIDO, NOME, NASCIMENTO, STACK 
                                                                FROM 
                                                                    PEOPLE 
                                                                WHERE 
                                                                    ID = @ID", new { id }, commandType: System.Data.CommandType.Text);

        return result.ToPersonResponse();
    });

    return string.IsNullOrEmpty(result) ? Results.NotFound() : Results.Text(result, contentType: "application/json");
});

app.MapGet("/pessoas", async ([FromQuery(Name = "t")] string search, NpgsqlConnection connection) =>
{
    if (string.IsNullOrEmpty(search)) return Results.BadRequest();

    var query = @"SELECT
                      ID, APELIDO, NOME, NASCIMENTO, STACK 
                  FROM 
                      PEOPLE 
                  WHERE 
                      APELIDO LIKE @SEARCH
                      OR APELIDO LIKE @SEARCH
                      OR EXISTS (SELECT 1 FROM jsonb_array_elements(STACK) AS x(STACK)
                                          WHERE  STACK::text like @SEARCH)
                      limit 50;";

    var result = await connection.QueryAsync<PersonResponse>(query, new { search = $"%{search}%" }, commandType: System.Data.CommandType.Text);
    return Results.Ok(result);


});

app.MapGet("/contagem-pessoas", async (NpgsqlConnection connection) =>
{
    await connection.OpenAsync();

    return Results.Ok(connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PEOPLE"));
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDeveloperExceptionPage();

app.Use(next => context =>
{
    context.Request.EnableBuffering();
    return next(context);
});
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(await context.Request.GetRawBodyAsync());
    });
});

app.Run();


public partial class Program { }