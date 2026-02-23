// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NumberSliderNode.cs - Slider input node with min/max range
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// A number slider node with configurable min, max, and current value.
    /// Outputs a single number value controlled by a slider UI.
    /// </summary>
    public class NumberSliderNode : Node
    {
        public override string TypeName => "NumberSlider";
        public override string Title => "Number Slider";
        public override string Category => "Input";

        /// <summary>Minimum slider value.</summary>
        public double Min { get; set; } = 0;

        /// <summary>Maximum slider value.</summary>
        public double Max { get; set; } = 100;

        /// <summary>Current slider value.</summary>
        public double CurrentValue { get; set; } = 50;

        /// <summary>Step size for snapping the slider value.</summary>
        public double Step { get; set; } = 1;

        public NumberSliderNode()
        {
            AddOutput("Value", "Value", PortDataType.Number);
        }

        protected override void Compute()
        {
            // Clamp the value to the range
            CurrentValue = Math.Max(Min, Math.Min(Max, CurrentValue));
            var output = GetOutput("Value");
            if (output != null)
                output.Value = CurrentValue;
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Min"] = Min,
                ["Max"] = Max,
                ["CurrentValue"] = CurrentValue,
                ["Step"] = Step
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Min", out var min))
                Min = Convert.ToDouble(min ?? 0);
            if (parameters.TryGetValue("Max", out var max))
                Max = Convert.ToDouble(max ?? 100);
            if (parameters.TryGetValue("CurrentValue", out var val))
                CurrentValue = Convert.ToDouble(val ?? 50);
            if (parameters.TryGetValue("Step", out var step))
                Step = Convert.ToDouble(step ?? 1);
        }
    }
}
