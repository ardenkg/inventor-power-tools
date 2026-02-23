// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PowerNode.cs - Power / Square root math node
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes power or square root.
    /// Mode: Power (Base^Exponent), Sqrt (√Value).
    /// </summary>
    public class PowerNode : Node
    {
        public override string TypeName => "Power";
        public override string Title => "Power";
        public override string Category => "Math";

        public string Mode { get; set; } = "Power";

        public PowerNode()
        {
            AddInput("Base", "Base", PortDataType.Number, 2.0);
            AddInput("Exponent", "Exponent", PortDataType.Number, 2.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            if (Mode == "Sqrt")
            {
                var val = GetInput("Base")!.GetDouble();
                if (val < 0)
                {
                    HasError = true;
                    ErrorMessage = "Cannot take square root of negative number";
                    return;
                }
                GetOutput("Result")!.Value = Math.Sqrt(val);
            }
            else
            {
                var b = GetInput("Base")!.GetDouble(2.0);
                var e = GetInput("Exponent")!.GetDouble(2.0);
                GetOutput("Result")!.Value = Math.Pow(b, e);
            }
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
                    Choices = new[] { "Power", "Sqrt" },
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
                if (s == "Power" || s == "Sqrt") Mode = s;
        }
    }
}
