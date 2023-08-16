using RinhaBackEnd.Test.Interfaces;

namespace RinhaBackEnd.Test.Controllers;

[Trait("Integration", "Api")]
[Collection("docker")]
public class PeopleControllerTest : IClassFixture<DockerFixture>, IDisposable
{
    protected IIntegrationTest _fixture { get; set; }
    protected ITestOutputHelper _output { get; set; }

    public PeopleControllerTest(ContainerFixture fixture, DockerFixture dockerFixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;

        dockerFixture.InitOnce(() => new DockerFixtureOptions
        {
            DockerComposeFiles = new[] { "docker-compose.yml", "docker-compose.testing.yml" },
            CustomUpTest = output => output.Any(l => l.Contains("ready for start up") || l.Contains("Attaching to api01, api02, cache, database, proxy")),
            StartupTimeoutSecs = 240
        });


        Thread.Sleep(2_400);

    }

    [Fact(Timeout = 10_000)]
    public async Task HealthShouldBeSuccess()
    {
        var response = await _fixture.Client.GetAsync("/ping");

        response.EnsureSuccessStatusCode();

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("pong");
    }

    [Fact(Timeout = 10_000)]
    public async Task CreatePersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
            Nascimento = DateTime.Now.AddYears(-10).Date.ToString("yyyy-MM-dd"),
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
        sut.Nascimento.ToString("yyyy-MM-dd").Should().Be(request.Nascimento);
        sut.Stacks.Should().Contain(request.Stack);
    }

    [Fact(Timeout = 10_000)]
    public async Task CreateRepeatedPersonShouldFail422()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
            Nascimento = DateTime.Now.AddYears(-10).Date.ToString("yyyy-MM-dd"),
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
        sut.Nascimento.ToString("yyyy-MM-dd").Should().Be(request.Nascimento);
        sut.Stacks.Should().Contain(request.Stack);
    }

    [Fact(Timeout = 10_000)]
    public async Task CreatePersonShouldFailStatus422()
    {
        var request = new PersonRequest
        {
            Apelido = "",
            Nascimento = DateTime.Now.AddYears(-10).Date.ToString("yyyy-MM-dd"),
            Nome = "",
            Stack = new List<string> { "Java", "C#", "Html" }
        };

        var response = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonRequest>();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Should().Be(request.Nascimento);
        sut.Stack.Should().Contain(request.Stack);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetPersonShouldBeSuccess()
    {
        var request = new PersonRequest
        {
            Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
            Nascimento = DateTime.Now.AddYears(-10).Date.ToString("yyyy-MM-dd"),
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
        sut.Nascimento.ToString("yyyy-MM-dd").Should().Be(request.Nascimento);
        sut.Stacks.Should().Contain(request.Stack);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetPersonShouldBeFail404()
    {
        var response = await _fixture.Client.GetAsync($"/pessoas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 10_000)]
    public async Task QueryPersonShouldFind1()
    {
        var index = 0;
        foreach (var languages in GetLanguages().Take(2))
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
                Nascimento = DateTime.Now.AddYears(-(index * 3)).Date.ToString("yyyy-MM-dd"),
                Nome = $"Nome",
                Stack = languages
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();

            index++;
        }
        Thread.Sleep(2_000);
        var response = await _fixture.Client.GetAsync("/pessoas?t=Java");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>();

        sut.Should().HaveCount(1);
    }

    [Fact(Timeout = 10_000)]
    public async Task QueryPersonShouldFind0()
    {
        var index = 0;
        foreach (var languages in GetLanguages().Take(2))
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
                Nascimento = DateTime.Now.AddYears(-(index * 3)).Date.ToString("yyyy-MM-dd"),
                Nome = $"Nome",
                Stack = languages
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();

            index++;
        }

        Thread.Sleep(2_000);

        var response = await _fixture.Client.GetAsync("/pessoas?t=Cobol");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>();

        sut.Should().HaveCount(0);
    }

    [Fact(Timeout = 15_000)]
    public async Task CountPersonShouldBe3()
    {
        var index = 0;
        foreach (var languages in GetLanguages().Take(3))
        {
            var request = new PersonRequest
            {
                Apelido = $"Apelido{Guid.NewGuid().ToString()[..4]}",
                Nascimento = DateTime.Now.AddYears(-(index * 3)).Date.ToString("yyyy-MM-dd"),
                Nome = $"Nome",
                Stack = languages
            };

            var createReponse = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());
            createReponse.EnsureSuccessStatusCode();

            index++;
        }

        Thread.Sleep(2_000);

        var response = await _fixture.Client.GetAsync("/contagem-pessoas");
        response.EnsureSuccessStatusCode();

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("3");
    }

    private IEnumerable<IEnumerable<string>> GetLanguages()
    {
        yield return new string[] { "Java", "PHP", "Go" };
        yield return new string[] { "CSharp", "Elixir", };
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
