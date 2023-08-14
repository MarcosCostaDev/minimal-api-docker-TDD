using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;
using System.Globalization;

namespace RinhaBackEnd.Domain;

public class Person
{
    protected Person() { }
    public Person(string apelido, string nome, string nascimento, IEnumerable<string>? stacks)
    {
        Id = Guid.NewGuid();
        Apelido = apelido;
        Nome = nome;
        Nascimento = null;
        Stack = stacks;

        if (!string.IsNullOrWhiteSpace(nascimento) && 
            DateTime.TryParseExact(nascimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var nascimentoResult))
        {
            Nascimento = nascimentoResult;
        }
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
        if (string.IsNullOrWhiteSpace(Apelido)) return false;
        if (Apelido.Length > 32) return false;
        if (string.IsNullOrWhiteSpace(Nome)) return false;
        if (Nome.Length > 32) return false;
        if (!Nascimento.HasValue) return false;
        if (Stack != null && Stack.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 32)) return false;
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
            Nascimento = Nascimento.GetValueOrDefault()
        };
    }
}
