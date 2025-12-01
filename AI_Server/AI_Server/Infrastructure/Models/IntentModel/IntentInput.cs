using Microsoft.ML.Data;
namespace AI_Server.Infrastructure.Models.IntentModel
{
    /// <summary>
    /// model input class for IntentModel.
    /// </summary>
    public class IntentInput
    {
        [LoadColumn(0)]
        [ColumnName(@"text")]
        public string Instruction { get; set; }

        [LoadColumn(1)]
        [ColumnName(@"intent")]
        public string Intent { get; set; }

    }
}
