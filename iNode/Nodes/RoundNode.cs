// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RoundNode.cs - Rounding math node (Round, Floor, Ceiling)
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Rounds a number. Mode: Round, Floor, Ceiling.
    /// </summary>
    public class RoundNode : Node
    {
        public override string TypeName => "Round";
        public override string Title => "Round";
        public override string Category => "Math";

        public string Mode { get; set; } = "Round";

        public RoundNode()
        {
            AddInput("Value", "Value", PortDataType.Number, 0.0);
            AddInput("Decimals", "Decimals", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetDouble();
            var decimals = (int)GetInput("Decimals")!.GetDouble(0);
            if (decimals < 0) decimals = 0;
            if (decimals > 15) decimals = 15;

            double result = Mode switch
            {
                "Floor" => Math.Floor(val * Math.Pow(10, decimals)) / Math.Pow(10, decimals),
                "Ceiling" => Math.Ceiling(val * Math.Pow(10, decimals)) / Math.Pow(10, decimals),
                _ => Math.Round(val, decimals, MidpointRounding.AwayFromZero)
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
                    Choices = new[] { "Round", "Floor", "Ceiling" },
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
                if (s == "Round" || s == "Floor" || s == "Ceiling") Mode = s;
        }
    }
}
