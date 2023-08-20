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
            CustomUpTest = output => output.Any(l => l.Contains("custom up test satisfied")),
            StartupTimeoutSecs = 240,
            DockerComposeUpArgs = "--build"
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

        var sut = responseText.DeserializeTo<PersonResponse>(true);

        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.ToString("yyyy-MM-dd").Should().Be(request.Nascimento);
        sut.Stack.Should().Contain(request.Stack.ToArray());
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

        await Task.Delay(TimeSpan.FromSeconds(5));

        var response2 = await _fixture.Client.PostAsync("/pessoas", request.ToJsonHttpContent());

        var responseText = await response2.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonRequest>(true);

        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Should().Be(request.Nascimento);
        sut.Stack.Should().Contain(request.Stack.ToArray());
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

        var sut = responseText.DeserializeTo<PersonRequest>(true);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.Should().Be(request.Nascimento);
        sut.Stack.Should().Contain(request.Stack.ToArray());
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

        await Task.Delay(TimeSpan.FromSeconds(3));

        var response = await _fixture.Client.GetAsync(createReponse.Headers.Location.ToString());
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<PersonResponse>(true);

        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(request.Nome);
        sut.Apelido.Should().Be(request.Apelido);
        sut.Nascimento.ToString("yyyy-MM-dd").Should().Be(request.Nascimento);
        sut.Stack.Should().Contain(request.Stack.ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task GetPersonShouldBeFail404()
    {
        var response = await _fixture.Client.GetAsync($"/pessoas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 10_000)]
    public async Task QueryPersonShouldFind2()
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
        await Task.Delay(TimeSpan.FromSeconds(3));

        var response = await _fixture.Client.GetAsync("/pessoas?t=Java");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>(true);

        sut.Should().HaveCount(2);
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

        await Task.Delay(TimeSpan.FromSeconds(2));

        var response = await _fixture.Client.GetAsync("/pessoas?t=Cobol");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();

        var sut = responseText.DeserializeTo<IEnumerable<PersonResponse>>(true);

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

        await Task.Delay(TimeSpan.FromSeconds(2));

        var response = await _fixture.Client.GetAsync("/contagem-pessoas");
        response.EnsureSuccessStatusCode();

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("3");
    }

    private IEnumerable<IEnumerable<string>> GetLanguages()
    {
        yield return new string[] { "Java", "PHP", "Go" };
        yield return new string[] { "CSharp", "Elixir", };
        yield return new string[] { "Dart", "Ruby", "Javascript" };
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
