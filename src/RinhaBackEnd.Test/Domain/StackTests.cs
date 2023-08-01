using RinhaBackEnd.Domain;

namespace RinhaBackEnd.Test.Domain;

public class StackTests
{
    [Fact]
    public void ShouldCreateStack()
    {
        // Arrange
        var nome = "MyStack";

        // Act
        var sut = new Stack(nome);

        // Assert
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotCreateStackWithEmptyNome()
    {
        // Arrange
        var nome = "";

        // Act
        var sut = new Stack(nome);

        // Assert
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.IsValid.Should().BeFalse();
    }
}
