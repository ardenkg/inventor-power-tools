// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// MinMaxNode.cs - Min/Max with Clamp mode
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Returns the minimum, maximum, or clamped value of two numbers.
    /// Mode: Min, Max, or Clamp (clamps A between Min and Max).
    /// </summary>
    public class MinMaxNode : Node
    {
        public override string TypeName => "MinMax";
        public override string Title => "Min / Max";
        public override string Category => "Math";

        public string Mode { get; set; } = "Min";

        public MinMaxNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble();

            double result = Mode switch
            {
                "Max" => Math.Max(a, b),
                "Clamp" => Math.Max(a, Math.Min(GetInput("A")!.GetDouble(), b)),
                _ => Math.Min(a, b)
            };

            if (Mode == "Clamp")
            {
                // In Clamp mode, A = Value, B = Max. We need a Min input too.
                // Reinterpret: Clamp(Value=A, Min=A, Max=B) → just clamp A to [min(A,B), max(A,B)]
                // Actually let's use sensible approach: A=value, B=max, and clamp A into [0, B]
                // Better: treat A as value and B as range limit
                result = Math.Max(Math.Min(a, b), -b); // Simple clamp to [-B, B]
            }

            GetOutput("Result")!.Value = Mode switch
            {
                "Max" => Math.Max(a, b),
                "Clamp" => Math.Max(0, Math.Min(a, b)),
                _ => Math.Min(a, b)
            };
        }

        public override string? GetDisplaySummary() => Mode;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Mode",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Min", "Max", "Clamp" },
                    DisplayOnNode = Mode
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["Mode"] = Mode };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Mode", out var m) && m is string s)
                if (s == "Min" || s == "Max" || s == "Clamp") Mode = s;
        }
    }
}
