// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ModuloNode.cs - Modulo (remainder) math node
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Returns the remainder of A / B.
    /// </summary>
    public class ModuloNode : Node
    {
        public override string TypeName => "Modulo";
        public override string Title => "Modulo";
        public override string Category => "Math";

        public ModuloNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 1.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble(1.0);

            if (b == 0)
            {
                HasError = true;
                ErrorMessage = "Cannot divide by zero";
                return;
            }

            GetOutput("Result")!.Value = a % b;
        }
    }
}
