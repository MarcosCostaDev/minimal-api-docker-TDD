using RinhaBackEnd.Test.Interfaces;

namespace RinhaBackEnd.Test.Controllers;

[Trait("Integration", "Api")]
[Collection("API")]
public class PeopleControllerTest : IDisposable
{
    protected IIntegrationTest _fixture { get; set; }
    protected ITestOutputHelper _output { get; set; }

    protected PeopleControllerTest()
    {
    }
    public PeopleControllerTest(ProgramFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Benchmark(Description = nameof(HealthShouldBeSuccess))]
    [Fact]
    public async Task HealthShouldBeSuccess()
    {
        var response = await _fixture.Client.GetAsync("/ping");

        response.EnsureSuccessStatusCode();

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("pong");
    }

    [Benchmark(Description = nameof(CreatePersonShouldBeSuccess))]
    [Fact]
    public async Task CreatePersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString().Take(4)}",
            Nascimento = DateTime.Now.AddYears(-10).Date,
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
        //  sut.Nascimento.Date.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Benchmark(Description = nameof(CreateRepeatedPersonShouldFail422))]
    [Fact]
    public async Task CreateRepeatedPersonShouldFail422()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString().Take(4)}",
            Nascimento = DateTime.Now.AddYears(-10).Date,
            Nome = "Nome1",
            Stack = new List<string> { "Java", "C#", "Html" }
        };

        var response1 = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
        response1.EnsureSuccessStatusCode();

        var response2 = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());

        var responseText = await response2.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonResponse>();

        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Date.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Benchmark(Description = nameof(CreatePersonShouldFailStatus422))]
    [Fact]
    public async Task CreatePersonShouldFailStatus422()
    {
        var request = new PersonRequest
        {
            Apelido = "",
            Nascimento = DateTime.Now.AddYears(-10).Date,
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

    [Benchmark(Description = nameof(GetPersonShouldBeSuccess))]
    [Fact]
    public async Task GetPersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString().Take(4)}",
            Nascimento = DateTime.Now.AddYears(-10).Date,
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
        sut.Nascimento.Date.Should().Be(request.Nascimento.Date);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Benchmark(Description = nameof(GetPersonShouldBeFail404))]
    [Fact]
    public async Task GetPersonShouldBeFail404()
    {
        var response = await _fixture.Client.GetAsync($"/pessoas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Benchmark(Description = nameof(QueryPersonShouldFind4))]
    [Fact]
    public async Task QueryPersonShouldFind4()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}",
                Nascimento = DateTime.Now.AddYears(-3 * (i + 1)).Date,
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

    [Benchmark(Description = nameof(QueryPersonShouldFind0))]
    [Fact]
    public async Task QueryPersonShouldFind0()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}{Guid.NewGuid().ToString().Take(4)}",
                Nascimento = DateTime.Now.AddYears(-3 * (i + 1)).Date,
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

    [Benchmark(Description = nameof(CountPersonShouldBe10))]
    [Fact]
    public async Task CountPersonShouldBe10()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{i}{Guid.NewGuid().ToString().Take(4)}",
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

    private IEnumerable<IEnumerable<string>> GetLanguages()
    {
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
        await CleanDatabaseAsync();
    }

    protected async Task CleanDatabaseAsync()
    {
        await _fixture.ClearDatabaseAsync();
    }
    public void Dispose()
    {
        DisposeAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
