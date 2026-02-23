// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RangeNode.cs - Generate a sequence of numbers
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Generates a sequence of numbers from Start to End with a Step.
    /// Outputs individual values via index selection.
    /// </summary>
    public class RangeNode : Node
    {
        public override string TypeName => "Range";
        public override string Title => "Range";
        public override string Category => "Math";

        public RangeNode()
        {
            AddInput("Start", "Start", PortDataType.Number, 0.0);
            AddInput("End", "End", PortDataType.Number, 10.0);
            AddInput("Step", "Step", PortDataType.Number, 1.0);
            AddInput("Index", "Index", PortDataType.Number, 0.0);
            AddOutput("Value", "Value", PortDataType.Number);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var start = GetInput("Start")!.GetDouble(0);
            var end = GetInput("End")!.GetDouble(10);
            var step = GetInput("Step")!.GetDouble(1);
            var index = (int)GetInput("Index")!.GetDouble(0);

            if (step == 0)
            {
                HasError = true;
                ErrorMessage = "Step cannot be zero";
                return;
            }

            // Build sequence
            var values = new List<double>();
            if (step > 0)
            {
                for (double v = start; v <= end + 1e-10; v += step)
                    values.Add(v);
            }
            else
            {
                for (double v = start; v >= end - 1e-10; v += step)
                    values.Add(v);
            }

            if (values.Count == 0)
            {
                HasError = true;
                ErrorMessage = "Range produces no values";
                return;
            }

            if (index < 0) index = 0;
            if (index >= values.Count) index = values.Count - 1;

            GetOutput("Value")!.Value = values[index];
            GetOutput("Count")!.Value = (double)values.Count;
        }
    }
}
