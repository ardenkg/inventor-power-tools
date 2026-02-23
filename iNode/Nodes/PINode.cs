// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PINode.cs - PI constant and degree/radian conversion
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Outputs the PI constant, or converts between degrees and radians.
    /// Mode: PI (outputs π), Deg→Rad, Rad→Deg.
    /// </summary>
    public class PINode : Node
    {
        public override string TypeName => "PI";
        public override string Title => "PI / Deg↔Rad";
        public override string Category => "Math";

        public string Mode { get; set; } = "PI";

        public PINode()
        {
            AddInput("Value", "Value", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetDouble();

            double result = Mode switch
            {
                "Deg\u2192Rad" => val * Math.PI / 180.0,
                "Rad\u2192Deg" => val * 180.0 / Math.PI,
                _ => Math.PI
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
                    Label = "Mode",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "PI", "Deg\u2192Rad", "Rad\u2192Deg" },
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
                if (s == "PI" || s == "Deg\u2192Rad" || s == "Rad\u2192Deg") Mode = s;
        }
    }
}
