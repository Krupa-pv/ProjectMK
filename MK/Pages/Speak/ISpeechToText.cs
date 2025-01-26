using System.Globalization;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using MK.Models;


namespace MK;

    public interface ISpeechToText
    {
        Task<bool> RequestPermissions();

        
   


    Task<bool> ListenAndAssessAsync(
        string expectedWord,
        CultureInfo culture,
        Action<string> updateFeedback,
        Action<bool> updateButtonState,
        CancellationToken cancellationToken,
        AttemptTracker attemptTracker);



    Task<PronunciationAssessmentResult> DiscreteAssessPronunciation(
        CultureInfo culture,
        string referenceText,
        AudioConfig audioConfig,
        IProgress<string> recognitionResult,
        CancellationToken cancellationToken);



        Task<string> Listen(CultureInfo culture, 
        IProgress<string> recognitionResult, 
        CancellationToken cancellationToken);
        
        Task<PronunciationAssessmentResult> AssessPronunciation(
            CultureInfo culture,
            string referenceText,
            IProgress<string> recognitionResult,
            CancellationToken cancellationToken);
      
    }


