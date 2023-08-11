using RinhaBackEnd.Dtos.Response;
using RinhaBackEnd.Extensions;

namespace RinhaBackEnd.Domain;

public class Person : Notifiable<Notification>
{
    protected Person() { }
    public Person(string apelido, string nome, DateTime nascimento, IEnumerable<string>? stacks)
    {
        Id = Guid.NewGuid();
        Apelido = apelido;
        Nome = nome;
        Nascimento = nascimento;

        var contract = new Contract<Notification>();
        contract.IsNotNullOrEmpty(Apelido, nameof(Apelido))
                .IsLowerOrEqualsThan(Apelido, 32, nameof(Apelido))
                .IsNotNullOrEmpty(Nome, nameof(Nome))
                .IsLowerOrEqualsThan(Nome, 100, nameof(Nome))
                .IsBetween(Nascimento, new DateTime(1900, 01, 01), DateTime.Now.Date, nameof(Nascimento));

        if (stacks != null)
        {
            foreach (var stack in stacks)
            {
                contract.IsNotNullOrEmpty(stack, nameof(stack));
            }

            Stack = stacks;
        }

        AddNotifications(contract);
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
