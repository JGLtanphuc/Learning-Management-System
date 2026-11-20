using Backend.DTOs.Menu;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class ModuleGroupMapper
    {
        public static partial ModuleGroupResponseDto ToResponseDto(this ModuleGroup entity);

        public static ModuleGroupResponseDto ToDto(this ModuleGroup entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(ModuleGroup.Id))]
        [MapperIgnoreTarget(nameof(ModuleGroup.CreatedAt))]
        [MapperIgnoreTarget(nameof(ModuleGroup.UpdatedAt))]
        [MapperIgnoreTarget(nameof(ModuleGroup.CreatedBy))]
        [MapperIgnoreTarget(nameof(ModuleGroup.UpdatedBy))]
        [MapperIgnoreTarget(nameof(ModuleGroup.Modules))]
        public static partial ModuleGroup ToEntity(this CreateModuleGroupRequest request);

        [MapperIgnoreTarget(nameof(ModuleGroup.Id))]
        [MapperIgnoreTarget(nameof(ModuleGroup.CreatedAt))]
        [MapperIgnoreTarget(nameof(ModuleGroup.UpdatedAt))]
        [MapperIgnoreTarget(nameof(ModuleGroup.CreatedBy))]
        [MapperIgnoreTarget(nameof(ModuleGroup.UpdatedBy))]
        [MapperIgnoreTarget(nameof(ModuleGroup.Modules))]
        public static partial void UpdateEntity([MappingTarget] this ModuleGroup entity, UpdateModuleGroupRequest request);
    }
}
