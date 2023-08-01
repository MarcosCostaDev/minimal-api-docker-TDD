using RinhaBackEnd.Test.Extensions;

namespace RinhaBackEnd.Test.Controllers;

[Collection("API")]
public class PeopleControllerTest
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

        var sut = await response.Content.ReadAsStringAsync();

        sut.Should().Be("pong");
    }

}
