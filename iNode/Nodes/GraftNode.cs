// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// GraftNode.cs - Wraps each item in its own sub-list (tree branch)
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Grafts a list: wraps each item in its own sub-list.
    /// If input is a single value, wraps it into a list of one.
    /// If input is a DataList, wraps each element in its own DataList.
    /// This is the inverse of Flatten.
    /// </summary>
    public class GraftNode : Node
    {
        public override string TypeName => "Graft";
        public override string Title => "Graft";
        public override string Category => "Utility";

        public GraftNode()
        {
            AddInput("Data", "Data", PortDataType.Any, null);
            AddOutput("List", "List", PortDataType.List);
        }

        protected override void Compute()
        {
            var val = GetInput("Data")!.GetEffectiveValue();

            if (val is DataList dataList)
            {
                // Graft: wrap each item in its own sub-list
                GetOutput("List")!.Value = dataList.Graft();
            }
            else if (val != null)
            {
                // Wrap single value into a list of one item
                GetOutput("List")!.Value = DataList.FromItem(val);
            }
            else
            {
                GetOutput("List")!.Value = new DataList();
            }
        }
    }
}
