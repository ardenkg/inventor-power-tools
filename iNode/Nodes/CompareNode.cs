// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// CompareNode.cs - Comparison logic node
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Compares two numbers and outputs 1.0 (true) or 0.0 (false).
    /// Mode: >, <, >=, <=, ==, !=
    /// </summary>
    public class CompareNode : Node
    {
        public override string TypeName => "Compare";
        public override string Title => "Compare";
        public override string Category => "Logic";

        public string Mode { get; set; } = ">";

        public CompareNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Result", "Result (0/1)", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble();

            bool result = Mode switch
            {
                ">" => a > b,
                "<" => a < b,
                ">=" => a >= b,
                "<=" => a <= b,
                "==" => Math.Abs(a - b) < 1e-10,
                "!=" => Math.Abs(a - b) >= 1e-10,
                _ => false
            };

            GetOutput("Result")!.Value = result ? 1.0 : 0.0;
        }

        public override string? GetDisplaySummary() => Mode;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Operator",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { ">", "<", ">=", "<=", "==", "!=" },
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
            {
                if (s == ">" || s == "<" || s == ">=" || s == "<=" || s == "==" || s == "!=")
                    Mode = s;
            }
        }
    }
}
