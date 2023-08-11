using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.Domain;

public class Person 
{
    protected Person() { }
    public Person(string apelido, string nome, DateTime nascimento, IEnumerable<string>? stacks)
    {
        Id = Guid.NewGuid();
        Apelido = apelido;
        Nome = nome;
        Nascimento = nascimento;
        Stack = stacks;
    }

    public Guid Id { get; private set; }
    public string Apelido { get; private set; }
    public string Nome { get; private set; }
    public DateTime Nascimento { get; private set; }
    public string Stacks
    {
        get { return Stack.ToJson(); }
        set { Stack = value.DeserializeTo<IEnumerable<string>>(); }
    }
    public IEnumerable<string> Stack { get; set; }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Apelido)) return false;
        if (Apelido.Length > 32) return false;
        if (string.IsNullOrEmpty(Nome)) return false;
        if (Nome.Length > 32) return false;
        if (Nascimento.Date > new DateTime(1900, 01, 01) && Nascimento.Date <= DateTime.Now.Date) return false;
        if (Stack.Any(p => string.IsNullOrEmpty(p))) return false;
        return true;
    }

    public PersonResponse ToPersonResponse()
    {
        return new PersonResponse
        {
            Id = Id,
            Apelido = Apelido,
            Nome = Nome,
            Stacks = Stack,
            Nascimento = Nascimento
        };
    } 
}
