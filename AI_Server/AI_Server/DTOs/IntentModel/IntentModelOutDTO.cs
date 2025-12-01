namespace AI_Server.DTOs.MoodModel
{
    public class IntentModelOutDTO
    {
        public string PredictedLabel { get; set; }
        public float[] LabelScores { get; set; }
        public float Confidence{ get; set; }
    }
}
