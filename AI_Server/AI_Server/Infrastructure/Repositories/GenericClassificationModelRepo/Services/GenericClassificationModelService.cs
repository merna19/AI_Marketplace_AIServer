using AI_Server.Infrastructure.Interfaces.IntentModel;
using Microsoft.ML;
using Microsoft.Extensions.ML;
using AI_Server.Infrastructure.Repositories.GenericClassificationModelRepo.Interfaces;
namespace AI_Server.Infrastructure.Repositories.GenericClassificationModelRepo.Services
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
            //Lower Case conversion
            prompt = prompt?.ToLower();
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
