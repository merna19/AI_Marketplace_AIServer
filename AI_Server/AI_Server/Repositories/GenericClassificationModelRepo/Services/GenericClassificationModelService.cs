using AI_Server.Infrastructure.Models.IntentModel;
using AI_Server.Repositories.GenericClassificationModelRepo.Interfaces;
using Microsoft.ML;
using Microsoft.Extensions.ML;
namespace AI_Server.Repositories.GenericClassificationModelRepo.Services
{
    
    public class GenericClassificationModelService<TOutModel, TInModel> : IGenericClassificationModel< TOutModel, TInModel> where TInModel : class where TOutModel:class, new()
    {
        private PredictionEnginePool<TInModel, TOutModel> _Engine;
        public GenericClassificationModelService(PredictionEnginePool<TInModel, TOutModel> Engine) 
        {
            this._Engine=Engine;
        }
        public void Normalize(string prompt) 
        {
            //remove all start-end white spaces
            prompt=prompt?.Trim();
            //remove comma
            prompt=prompt?.Replace(",","");
        }
        public TOutModel Predict(TInModel NormalizedPrompt)
        {
            return _Engine.Predict(NormalizedPrompt);
        }
        public float MaxScore(float[] LabelScores)
        {
            return LabelScores.Max();
        }

    }
    
}
