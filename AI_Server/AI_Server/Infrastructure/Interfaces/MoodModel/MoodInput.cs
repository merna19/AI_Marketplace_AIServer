
using Microsoft.ML.Data;

namespace AI_Server.Infrastructure.Models.MoodModel
{
    public class MoodInput
    {
        [LoadColumn(0)]
        [ColumnName(@"text")]
        public string Text { get; set; }

        [LoadColumn(1)]
        [ColumnName(@"label")]
        public string Label { get; set; }
    }
}
