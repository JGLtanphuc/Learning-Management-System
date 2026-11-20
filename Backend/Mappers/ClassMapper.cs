using Backend.DTOs.Class;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class ClassMapper
    {
        [MapProperty(nameof(TrainingClass.Students) + ".Count", nameof(ClassResponseDto.TotalStudents))]
        [MapperIgnoreSource(nameof(TrainingClass.Teacher))]
        [MapperIgnoreTarget(nameof(ClassResponseDto.TeacherName))]
        public static partial ClassResponseDto ToResponseDto(this TrainingClass entity);

        public static ClassResponseDto ToDto(this TrainingClass entity)
        {
            var dto = entity.ToResponseDto();
            if (entity.Teacher != null)
            {
                dto.TeacherName = $"{entity.Teacher.FirstName} {entity.Teacher.LastName}".Trim();
            }
            return dto;
        }

        [MapperIgnoreTarget(nameof(TrainingClass.Id))]
        [MapperIgnoreTarget(nameof(TrainingClass.CreatedAt))]
        [MapperIgnoreTarget(nameof(TrainingClass.UpdatedAt))]
        [MapperIgnoreTarget(nameof(TrainingClass.CreatedBy))]
        [MapperIgnoreTarget(nameof(TrainingClass.UpdatedBy))]
        [MapperIgnoreTarget(nameof(TrainingClass.Students))]
        [MapperIgnoreTarget(nameof(TrainingClass.Teacher))]
        public static partial TrainingClass ToEntity(this CreateClassRequest request);

        [MapperIgnoreTarget(nameof(TrainingClass.Id))]
        [MapperIgnoreTarget(nameof(TrainingClass.CreatedAt))]
        [MapperIgnoreTarget(nameof(TrainingClass.UpdatedAt))]
        [MapperIgnoreTarget(nameof(TrainingClass.CreatedBy))]
        [MapperIgnoreTarget(nameof(TrainingClass.UpdatedBy))]
        [MapperIgnoreTarget(nameof(TrainingClass.Students))]
        [MapperIgnoreTarget(nameof(TrainingClass.Teacher))]
        public static partial void UpdateEntity([MappingTarget] this TrainingClass entity, UpdateClassRequest request);
    }
}
