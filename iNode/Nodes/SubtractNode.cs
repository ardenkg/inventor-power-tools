// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SubtractNode.cs - Subtraction math node
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Subtracts two numbers: Result = A - B
    /// </summary>
    public class SubtractNode : Node
    {
        public override string TypeName => "Subtract";
        public override string Title => "Subtract";
        public override string Category => "Math";

        public SubtractNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble();
            GetOutput("Result")!.Value = a - b;
        }
    }
}
