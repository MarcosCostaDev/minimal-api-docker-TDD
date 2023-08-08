using RinhaBackEnd.CustomConfig;
using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Requests;
using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using RinhaBackEnd.Infra.Contexts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PeopleDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PeopleDbConnection"));
}, ServiceLifetime.Scoped);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "RinhaBackend";

});

builder.Services.AddCustomAutoMapper();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapGet("/ping", () => "pong");

app.MapPost("/pessoas", async ([FromBody] PersonRequest request, PeopleDbContext dbContext, IMapper mapper, IDistributedCache cache) =>
{
    var contract = new Contract<Notification>();

    var person = new Person(request.Apelido, request.Nome, request.Nascimento);

    dbContext.People.Add(person);

    contract.AddNotifications(person);

    if (request.Stack != null)
        foreach (var stackName in request.Stack)
        {
            var stack = new Stack(stackName);

            var personStack = new PersonStack(person, stack);

            dbContext.PersonStacks.Add(personStack);

            contract.AddNotifications(personStack);
        }

    if (!contract.IsValid) return Results.UnprocessableEntity(request);
    PersonResponse result;
    try
    {
        await dbContext.SaveChangesAsync();
        result = mapper.Map<PersonResponse>(person);
        cache.SetString(person.Id.ToString(), result.ToJson());
    }
    catch (Exception)
    {
        return Results.UnprocessableEntity(request);
    }
    return Results.Created(new Uri($"/pessoas/{person.Id}", uriKind: UriKind.Relative), result);
});

app.MapGet("/pessoas/{id:guid}", async ([FromRoute(Name = "id")] Guid id, PeopleDbContext dbContext, IMapper mapper, IDistributedCache cache) =>
{
    var result = await cache.GetOrCreateStringAsync(id.ToString(), () =>
    {
        return dbContext.People
                        .Include(p => p.PersonStacks)
                        .ThenInclude(p => p.Stack)
                        .Where(p => p.Id == id)
                        .ProjectTo<PersonResponse>(mapper.ConfigurationProvider)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();
    });

    return string.IsNullOrEmpty(result) ? Results.NotFound() : Results.Text(result, contentType: "application/json");
});

app.MapGet("/pessoas", async ([FromQuery(Name = "t")] string search, PeopleDbContext dbContext, IMapper mapper) =>
{
    var query = PredicateBuilder.New<Person>();
    query = query.Or(p => EF.Functions.Like(p.Nome, $"%{search}%"));
    query = query.Or(p => EF.Functions.Like(p.Apelido, $"%{search}%"));
    query = query.Or(p => p.PersonStacks.Any(s => EF.Functions.Like(s.Stack.Nome, $"%{search}%")));
    var result = await dbContext.People
                                .Include(p => p.PersonStacks)
                                .ThenInclude(p => p.Stack)
                                .Where(query)
                                .AsNoTracking()
                                .ProjectTo<PersonResponse>(mapper.ConfigurationProvider)
                                .ToListAsync();
    return Results.Ok(result);
});

app.MapGet("/contagem-pessoas", async (PeopleDbContext dbContext) =>
{
    return Results.Ok(await dbContext.People.AsNoTracking().CountAsync());
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
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


using (IServiceScope scope = app.Services.CreateScope())
    try
    {
        scope.ServiceProvider.GetService<PeopleDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
    }

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
});
app.Run();


public partial class Program { }