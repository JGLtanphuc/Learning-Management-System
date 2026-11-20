using Backend.DTOs.Student.Score;
using Backend.Data;
using Backend.Entities;
using Riok.Mapperly.Abstractions;

namespace Backend.Mappers
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class StudentScoreMapper
    {
        [MapperIgnoreTarget(nameof(StudentScoreResponseDto.Scores))]
        public static partial StudentScoreResponseDto MapBaseDto(StudentScore entity);

        public static StudentScoreResponseDto ToResponseDto(this StudentScore entity)
        {
            var dto = MapBaseDto(entity);
            if (entity.StudentScoreColumns != null)
            {
                var scoresDict = new Dictionary<string, decimal?>();
                foreach (var col in entity.StudentScoreColumns)
                {
                    scoresDict[col.ScoreKey] = col.ScoreValue;
                }
                dto.Scores = scoresDict;
            }
            return dto;
        }

        public static StudentScoreResponseDto ToDto(this StudentScore entity) => entity.ToResponseDto();

        [MapperIgnoreTarget(nameof(StudentScore.Id))]
        [MapperIgnoreTarget(nameof(StudentScore.CreatedAt))]
        [MapperIgnoreTarget(nameof(StudentScore.UpdatedAt))]
        [MapperIgnoreTarget(nameof(StudentScore.CreatedBy))]
        [MapperIgnoreTarget(nameof(StudentScore.UpdatedBy))]
        [MapperIgnoreTarget(nameof(StudentScore.StudentScoreColumns))]
        public static partial StudentScore MapBaseEntity(CreateStudentScoreRequest request);

        public static StudentScore ToEntity(this CreateStudentScoreRequest request)
        {
            var entity = MapBaseEntity(request);
            if (request.Scores != null)
            {
                foreach (var kvp in request.Scores)
                {
                    entity.StudentScoreColumns.Add(new StudentScoreColumn
                    {
                        ScoreKey = kvp.Key,
                        ScoreValue = kvp.Value
                    });
                }
            }
            return entity;
        }

        [MapperIgnoreTarget(nameof(StudentScore.Id))]
        [MapperIgnoreTarget(nameof(StudentScore.CreatedAt))]
        [MapperIgnoreTarget(nameof(StudentScore.UpdatedAt))]
        [MapperIgnoreTarget(nameof(StudentScore.CreatedBy))]
        [MapperIgnoreTarget(nameof(StudentScore.UpdatedBy))]
        [MapperIgnoreTarget(nameof(StudentScore.StudentScoreColumns))]
        public static partial void MapBaseUpdate([MappingTarget] StudentScore entity, UpdateStudentScoreRequest request);

        public static void UpdateEntity(this StudentScore entity, UpdateStudentScoreRequest request)
        {
            MapBaseUpdate(entity, request);
            entity.StudentScoreColumns.Clear();
            if (request.Scores != null)
            {
                foreach (var kvp in request.Scores)
                {
                    entity.StudentScoreColumns.Add(new StudentScoreColumn
                    {
                        StudentScoreId = entity.Id,
                        ScoreKey = kvp.Key,
                        ScoreValue = kvp.Value
                    });
                }
            }
        }
    }
}

