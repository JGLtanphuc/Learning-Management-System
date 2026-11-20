using Backend.DTOs.Book;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class BookMapper
    {
        public static partial BookResponseDto ToResponseDto(this Book entity);
        public static BookResponseDto ToDto(this Book entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(Book.Id))]
        [MapperIgnoreTarget(nameof(Book.CreatedAt))]
        [MapperIgnoreTarget(nameof(Book.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Book.CreatedBy))]
        [MapperIgnoreTarget(nameof(Book.UpdatedBy))]
        public static partial Book ToEntity(this CreateBookRequest request);

        [MapperIgnoreTarget(nameof(Book.Id))]
        [MapperIgnoreTarget(nameof(Book.CreatedAt))]
        [MapperIgnoreTarget(nameof(Book.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Book.CreatedBy))]
        [MapperIgnoreTarget(nameof(Book.UpdatedBy))]
        public static partial void UpdateEntity([MappingTarget] this Book entity, UpdateBookRequest request);
    }
}

