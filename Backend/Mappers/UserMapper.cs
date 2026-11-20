using Backend.DTOs.Rbac;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class UserMapper
    {
        [MapProperty(nameof(User.Roles), nameof(UserResponseDto.RoleIds))]
        public static partial UserResponseDto ToResponseDto(this User entity);

        public static UserResponseDto ToDto(this User entity) => entity.ToResponseDto();

        private static long MapRoleToId(Role role) => role.Id;

        [MapperIgnoreTarget(nameof(User.Id))]
        [MapperIgnoreTarget(nameof(User.CreatedAt))]
        [MapperIgnoreTarget(nameof(User.UpdatedAt))]
        [MapperIgnoreTarget(nameof(User.CreatedBy))]
        [MapperIgnoreTarget(nameof(User.UpdatedBy))]
        [MapperIgnoreTarget(nameof(User.OneTimeTokens))]
        [MapperIgnoreTarget(nameof(User.RefreshTokens))]
        [MapperIgnoreTarget(nameof(User.Roles))]
        [MapperIgnoreTarget(nameof(User.PasswordHash))]
        public static partial User ToEntity(this CreateUserRequest request);

        public static User ToEntity(this CreateUserRequest request, string passwordHash)
        {
            var user = request.ToEntity();
            user.PasswordHash = passwordHash;
            return user;
        }

        [MapperIgnoreTarget(nameof(User.Id))]
        [MapperIgnoreTarget(nameof(User.CreatedAt))]
        [MapperIgnoreTarget(nameof(User.UpdatedAt))]
        [MapperIgnoreTarget(nameof(User.CreatedBy))]
        [MapperIgnoreTarget(nameof(User.UpdatedBy))]
        [MapperIgnoreTarget(nameof(User.OneTimeTokens))]
        [MapperIgnoreTarget(nameof(User.RefreshTokens))]
        [MapperIgnoreTarget(nameof(User.Roles))]
        [MapperIgnoreTarget(nameof(User.PasswordHash))]
        public static partial void UpdateEntity([MappingTarget] this User entity, UpdateUserRequest request);
    }
}
