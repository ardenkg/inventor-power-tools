// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// AbsNode.cs - Absolute value math node
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Returns the absolute value of a number.
    /// </summary>
    public class AbsNode : Node
    {
        public override string TypeName => "Abs";
        public override string Title => "Abs";
        public override string Category => "Math";

        public AbsNode()
        {
            AddInput("Value", "Value", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("Value")!.GetDouble();
            GetOutput("Result")!.Value = Math.Abs(val);
        }
    }
}
