using RinhaBackEnd.Domain;
using RinhaBackEnd.Dtos.Response;

namespace RinhaBackEnd.Mapping;

public class MapProfile : Profile
{
    public MapProfile()
    {
        CreateMap<Person, PersonResponse>()
            .ForMember(p => p.Stack, opts => opts.MapFrom(p => p.PersonStacks.Select(ps => ps.Stack.Nome)));
        CreateMap<Stack, string>()
            .ConvertUsing(p => p.Nome);
    }
}
