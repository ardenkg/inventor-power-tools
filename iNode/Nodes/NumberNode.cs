// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NumberNode.cs - Simple numeric input node
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// A simple number input node. User types a numeric value.
    /// </summary>
    public class NumberNode : Node
    {
        public override string TypeName => "Number";
        public override string Title => "Number";
        public override string Category => "Input";

        public double CurrentValue { get; set; } = 0;

        public NumberNode()
        {
            AddOutput("Value", "Value", PortDataType.Number);
        }

        protected override void Compute()
        {
            var output = GetOutput("Value");
            if (output != null)
                output.Value = CurrentValue;
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["CurrentValue"] = CurrentValue
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("CurrentValue", out var val))
                CurrentValue = Convert.ToDouble(val ?? 0);
        }
    }
}
