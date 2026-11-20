using Backend.DTOs.Rbac;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class PermissionMapper
    {
        public static partial PermissionResponseDto ToResponseDto(this Permission entity);

        public static PermissionResponseDto ToDto(this Permission entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Permission.Id))]
        [MapperIgnoreTarget(nameof(Permission.CreatedAt))]
        [MapperIgnoreTarget(nameof(Permission.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Permission.CreatedBy))]
        [MapperIgnoreTarget(nameof(Permission.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Permission.Roles))]
        public static partial Permission ToEntity(this CreatePermissionRequest request);

        [MapperIgnoreTarget(nameof(Permission.Id))]
        [MapperIgnoreTarget(nameof(Permission.CreatedAt))]
        [MapperIgnoreTarget(nameof(Permission.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Permission.CreatedBy))]
        [MapperIgnoreTarget(nameof(Permission.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Permission.Roles))]
        public static partial void UpdateEntity([MappingTarget] this Permission entity, UpdatePermissionRequest request);
    }
}
