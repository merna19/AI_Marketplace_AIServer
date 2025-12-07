using Microsoft.ML.Data;

namespace AI_Server.Infrastructure.Models.MoodModel
{
    public class MoodOutput
    {
        //[ColumnName(@"text")]
        //public float[] Text { get; set; }

        //[ColumnName(@"label")]
        //public uint Label { get; set; }

        //[ColumnName(@"Features")]
        //public float[] Features { get; set; }

        [ColumnName(@"PredictedLabel")]
        public string PredictedLabel { get; set; }

        [ColumnName(@"Score")]
        public float[] Scores { get; set; }
    }
}
