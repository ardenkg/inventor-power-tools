// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NegateNode.cs - Negate a number
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Negates a number: Result = -Value
    /// </summary>
    public class NegateNode : Node
    {
        public override string TypeName => "Negate";
        public override string Title => "Negate";
        public override string Category => "Math";

        public NegateNode()
        {
            AddInput("Value", "Value", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            GetOutput("Result")!.Value = -GetInput("Value")!.GetDouble();
        }
    }
}
