using Backend.DTOs.Menu;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class ModuleMapper
    {
        public static partial ModuleResponseDto ToResponseDto(this Module entity);

        public static ModuleResponseDto ToDto(this Module entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Module.Id))]
        [MapperIgnoreTarget(nameof(Module.CreatedAt))]
        [MapperIgnoreTarget(nameof(Module.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Module.CreatedBy))]
        [MapperIgnoreTarget(nameof(Module.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Module.ModuleGroup))]
        public static partial Module ToEntity(this CreateModuleRequest request);

        [MapperIgnoreTarget(nameof(Module.Id))]
        [MapperIgnoreTarget(nameof(Module.CreatedAt))]
        [MapperIgnoreTarget(nameof(Module.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Module.CreatedBy))]
        [MapperIgnoreTarget(nameof(Module.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Module.ModuleGroup))]
        public static partial void UpdateEntity([MappingTarget] this Module entity, UpdateModuleRequest request);
    }
}
