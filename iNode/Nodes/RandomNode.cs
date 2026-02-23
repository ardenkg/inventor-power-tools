// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RandomNode.cs - Random number within range
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Generates a random number between Min and Max.
    /// The Seed input allows reproducible results.
    /// </summary>
    public class RandomNode : Node
    {
        public override string TypeName => "Random";
        public override string Title => "Random";
        public override string Category => "Math";

        public RandomNode()
        {
            AddInput("Min", "Min", PortDataType.Number, 0.0);
            AddInput("Max", "Max", PortDataType.Number, 1.0);
            AddInput("Seed", "Seed", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var min = GetInput("Min")!.GetDouble(0);
            var max = GetInput("Max")!.GetDouble(1);
            var seed = (int)GetInput("Seed")!.GetDouble(0);

            Random rng = seed != 0 ? new Random(seed) : new Random();
            var result = min + rng.NextDouble() * (max - min);

            GetOutput("Result")!.Value = result;
        }
    }
}
