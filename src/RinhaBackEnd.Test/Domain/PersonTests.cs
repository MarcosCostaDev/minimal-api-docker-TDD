using RinhaBackEnd.Domain;

namespace RinhaBackEnd.Test.Domain;
[Trait("Unit", "Domain")]
public class PersonTests
{
    [Fact]
    public void IsValid_WhenCalled_ReturnsTrue()
    {
        // Arrange
        var person = new Person("Doe", "John", DateTime.Now.ToString("yyyy-MM-dd"), new List<string> { "C#", "Java" });

        // Act
        var result = person.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenCalledWithInvalidData_ReturnsFalse()
    {
        // Arrange
        var person = new Person("", "John", DateTime.Now.ToString("yyyy-MM-dd"), new List<string> { "C#", "Java" });

        // Act
        var result = person.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToPersonResponse_WhenCalled_ReturnsPersonResponse()
    {
        // Arrange
        var person = new Person("Doe", "John", DateTime.Now.ToString("yyyy-MM-dd"), new List<string> { "C#", "Java" });

        // Act
        var result = person.ToPersonResponse();

        // Assert
        result.Id.Should().Be(person.Id);
        result.Apelido.Should().Be(person.Apelido);
        result.Nome.Should().Be(person.Nome);
        result.Stack.Should().BeEquivalentTo(person.Stack);
        result.Nascimento.Should().Be(person.Nascimento.GetValueOrDefault());
    }
}
