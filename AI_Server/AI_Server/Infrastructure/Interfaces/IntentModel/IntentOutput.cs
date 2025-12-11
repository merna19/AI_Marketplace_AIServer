using Microsoft.ML.Data;

namespace AI_Server.Infrastructure.Interfaces.IntentModel
{
    public class IntentOutput
    {
        //[ColumnName(@"text")]
        //public float[] Text { get; set; }

        //[ColumnName(@"intent")]
        //public uint Intent { get; set; }

        //[ColumnName(@"Features")]
        //public float[] Features { get; set; }

        [ColumnName(@"PredictedLabel")]
        public string PredictedLabel { get; set; }

        [ColumnName(@"Score")]
        public float[] Scores { get; set; }



    }

}
