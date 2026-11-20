using System.Threading.Tasks;

namespace Backend.Services.Interfaces
{
    public record AiGradingOutcome(
        decimal Score,
        string Verdict,
        string Feedback,
        string GradedBy
    );

    public interface IInterviewAiEvaluator
    {
        Task<AiGradingOutcome> EvaluateAnswerAsync(
            string topicName,
            string questionText,
            string difficulty,
            string? sampleAnswer,
            string? keywords,
            string studentAnswer);
    }
}
