namespace AI_Server.Application.DTOs.IntentModel
{
    public class IntentModelOutDTO
    {
        public string PredictedLabel { get; set; }
        public float[] LabelScores { get; set; }
        public float Confidence{ get; set; }
    }
}
