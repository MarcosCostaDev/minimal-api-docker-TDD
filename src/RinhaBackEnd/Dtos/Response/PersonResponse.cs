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
    private string? _stack { get; set; }

    public string GetStack() => string.IsNullOrWhiteSpace(_stack) ? "[]" : _stack;

    [JsonPropertyName("stack")]
    public IEnumerable<string>? Stacks
    {
        get
        {
            return !string.IsNullOrEmpty(_stack) ? _stack.DeserializeTo<IEnumerable<string>>() : Enumerable.Empty<string>();
        }
        set
        {
            _stack = value?.ToJson();
        }
    }
}
