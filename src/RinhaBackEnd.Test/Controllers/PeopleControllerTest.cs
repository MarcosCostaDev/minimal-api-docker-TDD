namespace RinhaBackEnd.Test.Controllers;

[Collection("API")]
public class PeopleControllerTest : IDisposable
{
    private ProgramFixture _fixture;
    private ITestOutputHelper _output;

    public PeopleControllerTest(ProgramFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task CreatePersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = "Apelido1",
            Nascimento = DateTime.Now.AddYears(-10),
            Nome = "Nome1",
            Stack = new List<string> { "Java", "C#", "Html" }
        };

        var response = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonResponse>();

        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Fact]
    public async Task CreatePersonShouldFailStatus422()
    {
        var request = new PersonRequest
        {
            Apelido = "",
            Nascimento = DateTime.Now.AddYears(-10),
            Nome = "",
            Stack = new List<string> { "Java", "C#", "Html" }
        };

        var response = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonRequest>();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Date.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Fact]
    public async Task GetPersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = "Apelido1",
            Nascimento = DateTime.Now.AddYears(-10),
            Nome = "Nome1",
            Stack = new List<string> { "Java", "C#", "Html" }
        };

        var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
        createReponse.EnsureSuccessStatusCode();

        var response = await _fixture.Client.GetAsync(createReponse.Headers.Location.ToString());
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonResponse>();

        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Fact]
    public async Task GetPersonShouldBeFail404()
    {
        var response = await _fixture.Client.GetAsync($"/pessoas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task QueryPersonShouldFind4()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}",
                Nascimento = DateTime.Now.AddYears(-3 * (i + 1)),
                Nome = $"Nome{i}",
                Stack = GetLanguages().ElementAt(i)
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();
        }

        var response = await _fixture.Client.GetAsync("/pessoas?t=Java");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>();

        sut.Should().HaveCount(4);
    }

    [Fact]
    public async Task QueryPersonShouldFind0()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}",
                Nascimento = DateTime.Now.AddYears(-3 * (i + 1)),
                Nome = $"Nome{i}",
                Stack = GetLanguages().ElementAt(i)
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();
        }

        var response = await _fixture.Client.GetAsync("/pessoas?t=Cobol");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>();

        sut.Should().HaveCount(0);
    }

    [Fact]
    public async Task CountPersonShouldBe10()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}",
                Nascimento = DateTime.Now.AddYears(-3 * (i + 1)),
                Nome = $"Nome{i}",
                Stack = GetLanguages().ElementAt(i) 
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();
        }

        var response = await _fixture.Client.GetAsync("/contagem-pessoas");
        response.EnsureSuccessStatusCode();

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("10");
    }

    private IEnumerable<IEnumerable<string>> GetLanguages() {
        yield return new string[] { "Java", "PHP", "Go" };
        yield return new string[] { "CSharp", "Elixir", "Javascript" };
        yield return new string[] { "Dart", "Ruby", "Elixir" };
        yield return new string[] { "Ruby", "PHP" };
        yield return new string[] { "CSharp" };
        yield return new string[] { "Java" };
        yield return new string[] { "PHP", "Delphi" };
        yield return new string[] { "Rush", "C", "C++" };
        yield return new string[] { "Python", "Java", "C++" };
        yield return new string[] { "Python", "CSharp", "Elixir" };
    }

    public async Task DisposeAsync(bool dispose)
    {
        if (!dispose) return;
        using var scope = _fixture.Server.Services.CreateScope();
        using var appDbContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();

        var people = await appDbContext.People.ToListAsync();
        var stack = await appDbContext.Stacks.ToListAsync();

        appDbContext.People.RemoveRange(people);

        await appDbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DisposeAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
