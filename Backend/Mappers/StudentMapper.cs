using Backend.DTOs.Student;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class StudentMapper
    {
        [MapProperty(nameof(Student.Class) + "." + nameof(TrainingClass.ClassName), nameof(StudentResponseDto.ClassName))]
        [MapProperty(nameof(Student.MockProject) + "." + nameof(MockProject.ProjectName), nameof(StudentResponseDto.MockProjectName))]
        [MapperIgnoreSource(nameof(Student.User))]
        public static partial StudentResponseDto ToResponseDto(this Student entity);

        public static StudentResponseDto ToDto(this Student entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Student.Id))]
        [MapperIgnoreTarget(nameof(Student.CreatedAt))]
        [MapperIgnoreTarget(nameof(Student.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Student.CreatedBy))]
        [MapperIgnoreTarget(nameof(Student.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Student.Class))]
        [MapperIgnoreTarget(nameof(Student.MockProject))]
        [MapperIgnoreTarget(nameof(Student.Attendances))]
        [MapperIgnoreTarget(nameof(Student.Allocations))]
        [MapperIgnoreTarget(nameof(Student.User))]
        public static partial Student ToEntity(this CreateStudentRequest request);

        [MapperIgnoreTarget(nameof(Student.Id))]
        [MapperIgnoreTarget(nameof(Student.CreatedAt))]
        [MapperIgnoreTarget(nameof(Student.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Student.CreatedBy))]
        [MapperIgnoreTarget(nameof(Student.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Student.JoinedAt))]
        [MapperIgnoreTarget(nameof(Student.Class))]
        [MapperIgnoreTarget(nameof(Student.MockProject))]
        [MapperIgnoreTarget(nameof(Student.Attendances))]
        [MapperIgnoreTarget(nameof(Student.Allocations))]
        [MapperIgnoreTarget(nameof(Student.User))]
        public static partial void UpdateEntity([MappingTarget] this Student entity, UpdateStudentRequest request);
    }
}
