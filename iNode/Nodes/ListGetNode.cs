// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ListGetNode.cs - Gets an item from a DataList by index
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Gets a single item from a DataList by index.
    /// Outputs the item and passes through the original list.
    /// Index is clamped to valid range if out of bounds.
    /// </summary>
    public class ListGetNode : Node
    {
        public override string TypeName => "ListGet";
        public override string Title => "List Get";
        public override string Category => "Utility";

        public ListGetNode()
        {
            AddInput("List", "List", PortDataType.List, null);
            AddInput("Index", "Index", PortDataType.Number, 0.0);
            AddOutput("Item", "Item", PortDataType.Any);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var listVal = GetInput("List")!.GetEffectiveValue();
            int index = (int)GetInput("Index")!.GetDouble(0);

            if (listVal is not DataList dataList)
            {
                HasError = true;
                ErrorMessage = "Connect a DataList";
                return;
            }

            if (dataList.Count == 0)
            {
                HasError = true;
                ErrorMessage = "List is empty";
                return;
            }

            GetOutput("Item")!.Value = dataList.GetItem(index);
            GetOutput("Count")!.Value = (double)dataList.Count;
        }
    }
}
