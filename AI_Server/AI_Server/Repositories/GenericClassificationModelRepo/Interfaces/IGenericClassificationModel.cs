using Microsoft.ML;

namespace AI_Server.Repositories.GenericClassificationModelRepo.Interfaces
{
    public interface IGenericClassificationModel<TOutModel,TInModel>
    {
        public void Normalize(string prompt);
        public TOutModel Predict(TInModel NormalizedPrompt);
        public float MaxScore(float[] LabelScores);
    }
}
