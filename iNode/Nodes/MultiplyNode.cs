// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// MultiplyNode.cs - Multiplication math node
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Multiplies two numbers: Result = A × B
    /// </summary>
    public class MultiplyNode : Node
    {
        public override string TypeName => "Multiply";
        public override string Title => "Multiply";
        public override string Category => "Math";

        public MultiplyNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble();
            GetOutput("Result")!.Value = a * b;
        }
    }
}
