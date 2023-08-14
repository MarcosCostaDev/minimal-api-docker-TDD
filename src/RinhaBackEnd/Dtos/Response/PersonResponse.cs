using RinhaBackEnd.Extensions;
using System.Text.Json.Serialization;

namespace RinhaBackEnd.Dtos.Response;

public class PersonResponse
{
    public Guid Id { get; set; }
    public string Apelido { get; set; }
    public string Nome { get; set; }
    public DateTime Nascimento { get; set; }

    public string GetStack() => string.IsNullOrWhiteSpace(_stack) ? "[]" : _stack;

    [JsonIgnore]
    private string? _stack { get; set; }

    [JsonPropertyName("stack")]
    public IEnumerable<string>? Stacks
    {
        get
        {
            return GetStackValue();
        }
        set
        {
            _stack = value?.ToJson();
        }
    }

    protected virtual IEnumerable<string> GetStackValue()
    {
        return string.IsNullOrWhiteSpace(_stack) ? Enumerable.Empty<string>() : _stack.DeserializeTo<IEnumerable<string>>();
    }
}


public class PersonResponseQuery : PersonResponse
{
    [JsonIgnore]
    public string? Stack { get; set; }

    protected override IEnumerable<string> GetStackValue()
    {
        return !string.IsNullOrEmpty(Stack) ? Stack.DeserializeTo<IEnumerable<string>>() : Enumerable.Empty<string>();
    }
}
