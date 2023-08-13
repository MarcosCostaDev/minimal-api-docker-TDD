using RinhaBackEnd;
using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Requests;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using RinhaBackEnd.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("PeopleDbConnection"), ServiceLifetime.Scoped);

//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//});

builder.Services.AddSingleton<IConnectionMultiplexerPool>(options =>
{
    return ConnectionMultiplexerPoolFactory.Create(
                poolSize: 100,
                configuration: builder.Configuration.GetConnectionString("RedisConnection"),
                connectionSelectionStrategy: ConnectionSelectionStrategy.RoundRobin);
});



builder.Services.AddHostedService<QueueConsumerHostedService>();

var app = builder.Build();

//app.UseForwardedHeaders(new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//});

app.MapGet("/ping", () => "pong");

app.MapPost("/pessoas", async ([FromBody] PersonRequest request, 
                               [FromServices] NpgsqlConnection connection, 
                               [FromServices] IConnectionMultiplexerPool redis) =>
{
    var person = new Person(request.Apelido, request.Nome, request.Nascimento, request.Stack);

    if (!person.IsValid()) return Results.UnprocessableEntity(request);

    var result = person.ToPersonResponse();
    try
    {
        var pool = await redis.GetAsync();
        var db = pool.Connection.GetDatabase();

        var existedApelido = await db.StringGetAsync($"personApelido:{person.Apelido}");

        if (existedApelido.HasValue) return Results.UnprocessableEntity(request);

        var responseJson = person.ToPersonResponse().ToJson();

        await db.StringSetAsync($"personApelido:{person.Apelido}", ".");

        await db.StringSetAsync($"personId:{person.Id}", responseJson);

        await db.StreamAddAsync(EnvConsts.StreamName, new NameValueEntry[] { new(EnvConsts.StreamPersonKey, responseJson) });
    }
    catch (Exception ex)
    {
        return Results.UnprocessableEntity(request);
    }
    return Results.Created(new Uri($"/pessoas/{person.Id}", uriKind: UriKind.Relative), result);
});

app.MapGet("/pessoas/{id:guid}", async ([FromRoute(Name = "id")] Guid? id, 
                                        [FromServices] NpgsqlConnection connection, 
                                        [FromServices] IConnectionMultiplexerPool redis, CancellationToken cancellationToken) =>
{
    if (id == null || Guid.Empty == id.Value) return Results.BadRequest();

    var pool = await redis.GetAsync();
    var db = pool.Connection.GetDatabase();

    var result = await db.StringGetAsync($"personId:{id}");

    if (!result.HasValue)
    {
        var queryResult = await connection.QueryFirstOrDefaultAsync<PersonResponse>(@"SELECT
                                                                    ID, APELIDO, NOME, NASCIMENTO, STACK 
                                                                FROM 
                                                                    PEOPLE 
                                                                WHERE 
                                                                    ID = @ID", new { id }, commandType: System.Data.CommandType.Text); ;

        if (queryResult == null) return Results.NotFound();

        await db.StringGetSetAsync($"personId:{id}", queryResult.ToJson());

        return Results.Ok(queryResult);
    }

    return string.IsNullOrEmpty(result) ? Results.NotFound() : Results.Text(result, contentType: "application/json");
});

app.MapGet("/pessoas", async ([FromQuery(Name = "t")] string? search, NpgsqlConnection connection) =>
{
    if (string.IsNullOrWhiteSpace(search)) return Results.BadRequest();

    var query = @"SELECT
                      ID, APELIDO, NOME, NASCIMENTO, STACK 
                  FROM 
                      PEOPLE 
                  WHERE 
                      APELIDO LIKE @SEARCH
                      OR NOME LIKE @SEARCH
                      OR EXISTS (SELECT 1 FROM jsonb_array_elements(STACK) AS x(STACK)
                                          WHERE  STACK::text like @SEARCH)
                      limit 50;";

    var result = await connection.QueryAsync<PersonResponse>(query, new { search = $"%{search}%" }, commandType: System.Data.CommandType.Text);
    return Results.Ok(result);

});

app.MapGet("/contagem-pessoas", async (NpgsqlConnection connection) =>
{
    return Results.Ok(connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PEOPLE"));
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
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = context.Request.ContentType;
        context.Response.Body = context.Request.Body;
    });
});

app.Run();


public partial class Program { }