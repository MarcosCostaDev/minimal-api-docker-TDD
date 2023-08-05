namespace RinhaBackEnd.Domain;

public class Person : Notifiable<Notification>
{
    protected Person() { }
    public Person(string apelido, string nome, DateTime nascimento)
    {
        Id = Guid.NewGuid();
        Apelido = apelido;
        Nome = nome;
        Nascimento = nascimento.Date;

        var contract = new Contract<Notification>();
        contract.IsNotNullOrEmpty(Apelido, nameof(Apelido))
                .IsNotNullOrEmpty(Nome, nameof(Nome))
                .IsBetween(Nascimento, new DateTime(1900, 01, 01), DateTime.Now.Date, nameof(Nascimento));
        AddNotifications(contract);
    }

    public Guid Id { get; private set; }
    public string Apelido { get; private set; }
    public string Nome { get; private set; }
    public DateTime Nascimento { get; private set; }
    public IReadOnlyCollection<PersonStack> PersonStacks { get; private set; }
}
