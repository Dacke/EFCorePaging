using EFCorePaging.Sample.Web.Data.Models;
using EFCorePaging.Sample.Web.Dtos;
using Mapster;

namespace EFCorePaging.Sample.Web.Mappings;

public class MappingConfig : IRegister {
    public void Register(TypeAdapterConfig config) {
        config.NewConfig<Person, PersonDto>()
            .Map(dest => dest.Id, src => src.PersonId);

#if DEBUG
        config.Compile(true); // Force compilation of ALL configurations
#endif
    }
}
