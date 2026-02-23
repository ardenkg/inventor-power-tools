// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ListReverseNode.cs - Reverses the order of items in a DataList
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Reverses the order of items in a DataList.
    /// </summary>
    public class ListReverseNode : Node
    {
        public override string TypeName => "ListReverse";
        public override string Title => "List Reverse";
        public override string Category => "Utility";

        public ListReverseNode()
        {
            AddInput("List", "List", PortDataType.List, null);
            AddOutput("List", "List", PortDataType.List);
        }

        protected override void Compute()
        {
            var listVal = GetInput("List")!.GetEffectiveValue();

            if (listVal is DataList dataList)
            {
                GetOutput("List")!.Value = dataList.Reverse();
            }
            else
            {
                HasError = true;
                ErrorMessage = "Connect a DataList";
            }
        }
    }
}
