using Backend.DTOs.Rbac;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class RoleMapper
    {
        [MapProperty(nameof(Role.Permissions), nameof(RoleResponseDto.PermissionIds))]
        public static partial RoleResponseDto ToResponseDto(this Role entity);

        public static RoleResponseDto ToDto(this Role entity) => entity.ToResponseDto();

        private static long MapPermissionToId(Permission permission) => permission.Id;

        [MapperIgnoreTarget(nameof(Role.Id))]
        [MapperIgnoreTarget(nameof(Role.CreatedAt))]
        [MapperIgnoreTarget(nameof(Role.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Role.CreatedBy))]
        [MapperIgnoreTarget(nameof(Role.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Role.Permissions))]
        [MapperIgnoreTarget(nameof(Role.Users))]
        public static partial Role ToEntity(this CreateRoleRequest request);

        [MapperIgnoreTarget(nameof(Role.Id))]
        [MapperIgnoreTarget(nameof(Role.CreatedAt))]
        [MapperIgnoreTarget(nameof(Role.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Role.CreatedBy))]
        [MapperIgnoreTarget(nameof(Role.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Role.Permissions))]
        [MapperIgnoreTarget(nameof(Role.Users))]
        public static partial void UpdateEntity([MappingTarget] this Role entity, UpdateRoleRequest request);
    }
}
