using Backend.DTOs.Allocation;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class AllocationMapper
    {
        [MapProperty(nameof(Allocation.Student) + "." + nameof(Student.StudentCode), nameof(AllocationResponseDto.StudentCode))]
        [MapProperty(nameof(Allocation.Student) + "." + nameof(Student.FullName), nameof(AllocationResponseDto.StudentName))]
        public static partial AllocationResponseDto ToResponseDto(this Allocation entity);

        public static AllocationResponseDto ToDto(this Allocation entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Allocation.Id))]
        [MapperIgnoreTarget(nameof(Allocation.CreatedAt))]
        [MapperIgnoreTarget(nameof(Allocation.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Allocation.CreatedBy))]
        [MapperIgnoreTarget(nameof(Allocation.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Allocation.Student))]
        public static partial Allocation ToEntity(this CreateAllocationRequest request);

        [MapperIgnoreTarget(nameof(Allocation.Id))]
        [MapperIgnoreTarget(nameof(Allocation.CreatedAt))]
        [MapperIgnoreTarget(nameof(Allocation.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Allocation.CreatedBy))]
        [MapperIgnoreTarget(nameof(Allocation.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Allocation.Student))]
        public static partial void UpdateEntity([MappingTarget] this Allocation entity, UpdateAllocationRequest request);
    }
}

