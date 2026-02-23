// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ActiveBodyNode.cs - Reference the active part's existing body
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Captures a transient copy of the active part's solid body.
    /// Use this to bring your existing Inventor geometry INTO the workflow
    /// so you can perform boolean, fillet, and other operations on it.
    ///
    /// When the workflow result is applied back to the part, iNode recognizes
    /// that the geometry originated from the part and handles integration
    /// accordingly (e.g., replacing the body rather than adding a duplicate).
    /// </summary>
    public class ActiveBodyNode : Node
    {
        public override string TypeName => "ActiveBody";
        public override string Title => "Active Body";
        public override string Category => "Geometry";

        public ActiveBodyNode()
        {
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            if (Context?.IsAvailable != true || !Context.HasActivePart)
            {
                HasError = true;
                ErrorMessage = "No active part document";
                return;
            }

            try
            {
                var compDef = Context.ActiveCompDef!;

                // Get the first solid body (main body)
                if (compDef.SurfaceBodies.Count == 0)
                {
                    HasError = true;
                    ErrorMessage = "Part has no solid bodies";
                    return;
                }

                var partBody = compDef.SurfaceBodies[1]; // 1-based indexing
                var tb = Context.TB;

                // Create a transient copy so the original isn't modified
                var copy = tb.Copy(partBody);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = "Active Body",
                    SourceNodeId = Id,
                    IsFromActivePart = true
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to read active body: {ex.Message}";
            }
        }
    }
}
