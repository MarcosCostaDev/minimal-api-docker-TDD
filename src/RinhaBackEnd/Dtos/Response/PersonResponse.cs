using RinhaBackEnd.Extensions;
using System.Text.Json.Serialization;

namespace RinhaBackEnd.Dtos.Response;

public class PersonResponse
{
    public Guid Id { get; set; }
    public string Apelido { get; set; }
    public string Nome { get; set; }
    public DateTime Nascimento { get; set; }
    [JsonIgnore]
    public string Stack { get; set; }

    [JsonPropertyName("stack")]
    public IEnumerable<string>? Stacks { get { return !string.IsNullOrEmpty(Stack) ? Stack.DeserializeTo<IEnumerable<string>>() : Enumerable.Empty<string>();  } set { ; } }
}
