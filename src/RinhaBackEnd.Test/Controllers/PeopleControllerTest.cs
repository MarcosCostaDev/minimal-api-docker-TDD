using Flurl.Http;


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
        var cli = new FlurlClient(_fixture.Client);
        var response = await cli.Request("people").PostJsonAsync(new PersonRequest {
            Apelido = "Apelido1",
            Nascimento = DateTime.Now.AddYears(-10),
            Nome = "Nome1" ,
            Stack = new List<string> { "Java", "C#", "Html"}
        });

        response.ResponseMessage.EnsureSuccessStatusCode();

        var sut = await response.ResponseMessage.Content.ReadAsStringAsync();

        sut.Should().Be("pong");
    }

}
