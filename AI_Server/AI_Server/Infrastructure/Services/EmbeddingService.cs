using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.Numerics.Tensors;

namespace Embeddings.Services
{
    public class EmbeddingService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly Tokenizer _tokenizer;

        public EmbeddingService()
        {
            // 1. FIX: Explicitly specify namespace for SessionOptions to avoid conflict with ASP.NET
            var options = new Microsoft.ML.OnnxRuntime.SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            _session = new InferenceSession("Infrastructure/Models/Embeddings/all-MiniLM-L6-v2.onnx", options);

            // 2. FIX: Use the factory method 'Create' instead of 'new'
            // This reads your standard BERT vocab.txt file.
            _tokenizer = WordPieceTokenizer.Create("Infrastructure/Models/Embeddings/vocab.txt");

            Console.WriteLine("✓ Loaded Optimized Embedding Service");
        }

        public float[] GenerateEmbedding(string text)
        {
            // 1. Tokenize
            var tokenIds = _tokenizer.EncodeToIds(text.ToLowerInvariant());
            var count = Math.Min(tokenIds.Count, 510);

            long[] inputIds = new long[count + 2];
            long[] attentionMask = new long[count + 2];
            long[] tokenTypeIds = new long[count + 2];

            // Add [CLS]
            inputIds[0] = 101;
            attentionMask[0] = 1;

            for (int i = 0; i < count; i++)
            {
                inputIds[i + 1] = tokenIds[i];
                attentionMask[i + 1] = 1;
            }

            // Add [SEP]
            inputIds[count + 1] = 102;
            attentionMask[count + 1] = 1;

            int sequenceLength = inputIds.Length;
            var dimensions = new[] { 1L, sequenceLength };

            // 2. Create Inputs
            using var inputIdsTensor = OrtValue.CreateTensorValueFromMemory(inputIds, dimensions);
            using var attMaskTensor = OrtValue.CreateTensorValueFromMemory(attentionMask, dimensions);
            using var typeIdsTensor = OrtValue.CreateTensorValueFromMemory(tokenTypeIds, dimensions);

            var inputs = new Dictionary<string, OrtValue>
            {
                { "input_ids", inputIdsTensor },
                { "attention_mask", attMaskTensor },
                { "token_type_ids", typeIdsTensor }
            };

            // 3. RUN INFERENCE (FIXED LINE)
            using var runOptions = new RunOptions(); // <--- Create this object
            using var outputs = _session.Run(runOptions, inputs, _session.OutputNames); // <--- Pass it here

            using var outputTensor = outputs.First();
            var outputData = outputTensor.GetTensorDataAsSpan<float>();

            // 4. Mean Pooling
            int hiddenSize = 384;
            var pooledEmbedding = new float[hiddenSize];

            for (int i = 0; i < sequenceLength; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    pooledEmbedding[j] += outputData[i * hiddenSize + j];
                }
            }

            for (int i = 0; i < hiddenSize; i++)
            {
                pooledEmbedding[i] /= sequenceLength;
            }

            Normalize(pooledEmbedding);
            return pooledEmbedding;
        }

        private void Normalize(float[] vector)
        {
            float sumSquares = 0;
            foreach (var val in vector) sumSquares += val * val;
            float norm = (float)Math.Sqrt(sumSquares);
            if (norm < 1e-9) return;
            for (int i = 0; i < vector.Length; i++) vector[i] /= norm;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}