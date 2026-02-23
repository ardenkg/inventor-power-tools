// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DivideNode.cs - Division math node
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Divides two numbers: Result = A ÷ B (handles divide by zero)
    /// </summary>
    public class DivideNode : Node
    {
        public override string TypeName => "Divide";
        public override string Title => "Divide";
        public override string Category => "Math";

        public DivideNode()
        {
            AddInput("A", "A", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 1.0);
            AddOutput("Result", "Result", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetDouble();
            var b = GetInput("B")!.GetDouble(1.0);

            if (System.Math.Abs(b) < 1e-10)
            {
                HasError = true;
                ErrorMessage = "Division by zero";
                GetOutput("Result")!.Value = 0.0;
                return;
            }

            GetOutput("Result")!.Value = a / b;
        }
    }
}
