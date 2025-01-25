using System.Globalization;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

namespace MK
{
    public interface ISpeechToText
    {
        Task<bool> RequestPermissions();
        Task<string> Listen(CultureInfo culture, IProgress<string> recognitionResult, CancellationToken cancellationToken);

        Task<PronunciationAssessmentResult> AssessPronunciation(
            CultureInfo culture,
            string referenceText,
            IProgress<string> recognitionResult,
            CancellationToken cancellationToken);
        Task<PronunciationAssessmentResult> DiscreteAssessPronunciation(
            CultureInfo culture,
            string referenceText,
            IProgress<string> recognitionResult,
            CancellationToken cancellationToken);

        
    }
}
