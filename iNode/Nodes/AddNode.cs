// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// AddNode.cs - Addition math node
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Adds two numbers: Result = A + B
    /// </summary>
    public class AddNode : Node
    {
        public override string TypeName => "Add";
        public override string Title => "Add";
        public override string Category => "Math";

        public AddNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble();
            GetOutput("Result")!.Value = a + b;
        }
    }
}
