using Backend.DTOs.MockProject;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class MockProjectMapper
    {
        [MapperIgnoreTarget(nameof(MockProjectResponseDto.StudentIds))]
        [MapperIgnoreTarget(nameof(MockProjectResponseDto.StudentNames))]
        [MapProperty(nameof(MockProject.Students), nameof(MockProjectResponseDto.Members))]
        public static partial MockProjectResponseDto MapBaseResponseDto(MockProject entity);

        public static partial StudentSimpleDto MapStudentToSimpleDto(Student student);

        public static MockProjectResponseDto ToResponseDto(this MockProject entity)
        {
            var dto = MapBaseResponseDto(entity);
            if (entity.Students != null)
            {
                dto.StudentIds = entity.Students.Select(s => s.Id).ToList();
                dto.StudentNames = entity.Students.Select(s => $"{s.FullName} ({s.StudentCode})").ToList();
            }
            return dto;
        }

        public static MockProjectResponseDto ToDto(this MockProject entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(MockProject.Id))]
        [MapperIgnoreTarget(nameof(MockProject.CreatedAt))]
        [MapperIgnoreTarget(nameof(MockProject.UpdatedAt))]
        [MapperIgnoreTarget(nameof(MockProject.CreatedBy))]
        [MapperIgnoreTarget(nameof(MockProject.UpdatedBy))]
        [MapperIgnoreTarget(nameof(MockProject.Students))]
        public static partial MockProject ToEntity(this CreateMockProjectRequest request);

        [MapperIgnoreTarget(nameof(MockProject.Id))]
        [MapperIgnoreTarget(nameof(MockProject.CreatedAt))]
        [MapperIgnoreTarget(nameof(MockProject.UpdatedAt))]
        [MapperIgnoreTarget(nameof(MockProject.CreatedBy))]
        [MapperIgnoreTarget(nameof(MockProject.UpdatedBy))]
        [MapperIgnoreTarget(nameof(MockProject.Students))]
        public static partial void UpdateEntity([MappingTarget] this MockProject entity, UpdateMockProjectRequest request);
    }
}

