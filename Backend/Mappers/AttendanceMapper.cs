using Backend.DTOs.Attendance;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class AttendanceMapper
    {
        [MapProperty(nameof(Attendance.Student) + "." + nameof(Student.StudentCode), nameof(AttendanceResponseDto.StudentCode))]
        [MapProperty(nameof(Attendance.Student) + "." + nameof(Student.FullName), nameof(AttendanceResponseDto.StudentName))]
        [MapProperty(nameof(Attendance.Class) + "." + nameof(TrainingClass.ClassName), nameof(AttendanceResponseDto.ClassName))]
        public static partial AttendanceResponseDto ToResponseDto(this Attendance entity);

        public static AttendanceResponseDto ToDto(this Attendance entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Attendance.Id))]
        [MapperIgnoreTarget(nameof(Attendance.CreatedAt))]
        [MapperIgnoreTarget(nameof(Attendance.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Attendance.CreatedBy))]
        [MapperIgnoreTarget(nameof(Attendance.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Attendance.Student))]
        [MapperIgnoreTarget(nameof(Attendance.Class))]
        public static partial Attendance ToEntity(this CreateAttendanceRequest request);

        [MapperIgnoreTarget(nameof(Attendance.Id))]
        [MapperIgnoreTarget(nameof(Attendance.CreatedAt))]
        [MapperIgnoreTarget(nameof(Attendance.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Attendance.CreatedBy))]
        [MapperIgnoreTarget(nameof(Attendance.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Attendance.Student))]
        [MapperIgnoreTarget(nameof(Attendance.Class))]
        public static partial void UpdateEntity([MappingTarget] this Attendance entity, UpdateAttendanceRequest request);
    }
}

