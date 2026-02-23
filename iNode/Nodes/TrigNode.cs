// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// TrigNode.cs - Trigonometric functions (Sin, Cos, Tan, Atan2)
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes trigonometric functions. Input angle is in degrees.
    /// Mode: Sin, Cos, Tan, Asin, Acos, Atan, Atan2.
    /// For inverse trig, output is in degrees.
    /// </summary>
    public class TrigNode : Node
    {
        public override string TypeName => "Trig";
        public override string Title => "Trig";
        public override string Category => "Math";

        public string Mode { get; set; } = "Sin";

        public TrigNode()
        {
            AddInput("Value", "Value (\u00B0)", PortDataType.Number, 0.0);
            AddInput("Y", "Y", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetDouble();

            double result = Mode switch
            {
                "Sin" => Math.Sin(val * Math.PI / 180.0),
                "Cos" => Math.Cos(val * Math.PI / 180.0),
                "Tan" => Math.Tan(val * Math.PI / 180.0),
                "Asin" => Math.Asin(Math.Max(-1, Math.Min(1, val))) * 180.0 / Math.PI,
                "Acos" => Math.Acos(Math.Max(-1, Math.Min(1, val))) * 180.0 / Math.PI,
                "Atan" => Math.Atan(val) * 180.0 / Math.PI,
                "Atan2" => Math.Atan2(GetInput("Y")!.GetDouble(), val) * 180.0 / Math.PI,
                _ => 0
            };

            GetOutput("Result")!.Value = result;
        }

        public override string? GetDisplaySummary() => Mode;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Function",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Sin", "Cos", "Tan", "Asin", "Acos", "Atan", "Atan2" },
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
                if (s == "Sin" || s == "Cos" || s == "Tan" ||
                    s == "Asin" || s == "Acos" || s == "Atan" || s == "Atan2")
                    Mode = s;
            }
        }
    }
}
