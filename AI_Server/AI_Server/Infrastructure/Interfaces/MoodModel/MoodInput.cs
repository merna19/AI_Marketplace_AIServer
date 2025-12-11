
using Microsoft.ML.Data;

namespace AI_Server.Infrastructure.Interfaces.MoodModel
{
    public class MoodInput
    {
        [LoadColumn(0)]
        [ColumnName(@"prompt")]
        public string Prompt { get; set; }

        [LoadColumn(1)]
        [ColumnName(@"label")]
        public string Label { get; set; }
    }
}
