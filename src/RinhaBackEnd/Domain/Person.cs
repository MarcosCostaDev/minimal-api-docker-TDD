using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.Domain;

public class Person 
{
    protected Person() { }
    public Person(string apelido, string nome, DateTime? nascimento, IEnumerable<string>? stacks)
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
    public DateTime? Nascimento { get; private set; }
    public string Stacks
    {
        get { return Stack.ToJson(); }
        set { Stack = value.DeserializeTo<IEnumerable<string>>(); }
    }
    public IEnumerable<string> Stack { get; set; }

    public bool IsValid()
    {
        return !(string.IsNullOrEmpty(Apelido)
                || Apelido.Length > 32
                || string.IsNullOrEmpty(Nome)
                || Nome.Length > 32
                || !Nascimento.HasValue
                || Stack.Any(p => string.IsNullOrEmpty(p) || p.Length > 32));
    }

    public PersonResponse ToPersonResponse()
    {
        return new PersonResponse
        {
            Id = Id,
            Apelido = Apelido,
            Nome = Nome,
            Stacks = Stack,
            Nascimento = Nascimento.GetValueOrDefault()
        };
    } 
}
