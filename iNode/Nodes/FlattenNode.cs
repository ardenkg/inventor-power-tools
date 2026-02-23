// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// FlattenNode.cs - Flattens nested lists into a single flat list
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Flattens a nested list (tree) into a single flat list.
    /// If input contains DataLists within DataLists, all items are
    /// recursively extracted into a single-level list.
    /// This is the inverse of Graft.
    /// </summary>
    public class FlattenNode : Node
    {
        public override string TypeName => "Flatten";
        public override string Title => "Flatten";
        public override string Category => "Utility";

        public FlattenNode()
        {
            AddInput("Data", "Data", PortDataType.Any, null);
            AddOutput("List", "List", PortDataType.List);
            AddOutput("Count", "Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var val = GetInput("Data")!.GetEffectiveValue();

            DataList result;
            if (val is DataList dataList)
            {
                result = dataList.Flatten();
            }
            else if (val != null)
            {
                result = DataList.FromItem(val);
            }
            else
            {
                result = new DataList();
            }

            GetOutput("List")!.Value = result;
            GetOutput("Count")!.Value = (double)result.Count;
        }
    }
}
