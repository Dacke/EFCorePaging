using EFCorePagingPrototype.Data.Models;
using Mapster;

namespace EFCorePagingPrototype.Mappings;

public class MappingConfig : IRegister {
    public void Register(TypeAdapterConfig config) {
        config.NewConfig<Person, PersonDto>()
            .Map(dest => dest.Id, src => src.PersonId);

#if DEBUG
        config.Compile(true); // Force compilation of ALL configurations
#endif
    }
}
