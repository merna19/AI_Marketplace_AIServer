using Microsoft.ML.Data;
namespace AI_Server.Infrastructure.Interfaces.IntentModel
{
    /// <summary>
    /// model input class for IntentModel.
    /// </summary>
    public class IntentInput
    {
        [LoadColumn(0)]
        [ColumnName(@"prompt")]
        public string Prompt { get; set; }

        [LoadColumn(1)]
        [ColumnName(@"label")]
        public string Label { get; set; }

    }
}
