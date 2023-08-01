using RinhaBackEnd.Domain;

namespace RinhaBackEnd.Test.Domain;

public class PersonTests
{
    [Fact]
    public void ShouldCreatePerson()
    {
        // Arrange
        var apelido = "Doe";
        var nome = "John";
        var nascimento = DateTime.Now;

        // Act
        var person = new Person(apelido, nome, nascimento);

        // Assert
        var sut = new Person(apelido, nome, nascimento);
        sut.IsValid.Should().BeTrue();
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.Nascimento.Should().Be(nascimento.Date);
    }

    [Fact]
    public void ShouldNotCreatePersonWithEmptyApelido()
    {
        // Arrange
        var apelido = "";
        var nome = "John";
        var nascimento = DateTime.Now;

        // Act & Assert
        var sut = new Person(apelido, nome, nascimento);
        sut.IsValid.Should().BeFalse();
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.Nascimento.Should().Be(nascimento.Date);
    }

    [Fact]
    public void ShouldNotCreatePersonWithEmptyNome()
    {
        // Arrange
        var apelido = "Doe";
        var nome = "";
        var nascimento = DateTime.Now;

        // Act & Assert
        var sut = new Person(apelido, nome, nascimento);
        sut.IsValid.Should().BeFalse();
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.Nascimento.Should().Be(nascimento.Date);
    }

    [Fact]
    public void ShouldNotCreatePersonWithInvalidNascimento()
    {
        // Arrange
        var apelido = "Doe";
        var nome = "John";
        var nascimento = DateTime.MinValue;

        // Act & Assert
        var sut = new Person(apelido, nome, nascimento);
        sut.IsValid.Should().BeFalse();
        sut.Id.Should().NotBeEmpty();
        sut.Nome.Should().Be(nome);
        sut.Nascimento.Should().Be(nascimento.Date);
    }
}
