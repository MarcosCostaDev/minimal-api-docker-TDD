using RinhaBackEnd.Extensions;
using System.Text.Json.Serialization;

namespace RinhaBackEnd.Dtos.Response;

public class PersonResponse
{
    public Guid Id { get; set; }
    public string Apelido { get; set; }
    public string Nome { get; set; }
    public DateTime Nascimento { get; set; }
    public string[]? Stack { get; set; }
}
