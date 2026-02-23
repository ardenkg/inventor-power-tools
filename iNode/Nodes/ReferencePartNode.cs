// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ReferencePartNode.cs - Import geometry from another open part document
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// References a part body from another open document in Inventor.
    /// Lists all open part documents (except the active one) and lets
    /// the user pick one by index. Outputs a transient copy of that
    /// part's primary solid body for use in boolean/transform operations.
    /// </summary>
    public class ReferencePartNode : Node
    {
        public override string TypeName => "ReferencePart";
        public override string Title => "Reference Part";
        public override string Category => "Geometry";

        /// <summary>
        /// 1-based index into the list of other open part documents.
        /// </summary>
        public int PartIndex { get; set; } = 1;

        /// <summary>
        /// 1-based index of the solid body within the selected part (default 1 = first body).
        /// </summary>
        public int BodyIndex { get; set; } = 1;

        public ReferencePartNode()
        {
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            if (Context?.IsAvailable != true)
            {
                HasError = true;
                ErrorMessage = "Inventor context not available";
                return;
            }

            try
            {
                // Collect all open part documents other than the active one
                var otherParts = new List<PartDocument>();
                var activeDoc = Context.ActivePartDoc;

                foreach (Document doc in Context.App.Documents)
                {
                    if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
                    {
                        var partDoc = (PartDocument)doc;
                        // Skip the active document
                        if (activeDoc != null && partDoc.FullFileName == activeDoc.FullFileName)
                            continue;
                        otherParts.Add(partDoc);
                    }
                }

                if (otherParts.Count == 0)
                {
                    HasError = true;
                    ErrorMessage = "No other part documents are open";
                    return;
                }

                int idx = Math.Max(1, Math.Min(PartIndex, otherParts.Count));
                var selectedPart = otherParts[idx - 1]; // 0-based

                var compDef = selectedPart.ComponentDefinition;
                if (compDef.SurfaceBodies.Count == 0)
                {
                    HasError = true;
                    ErrorMessage = $"'{System.IO.Path.GetFileNameWithoutExtension(selectedPart.DisplayName)}' has no solid bodies";
                    return;
                }

                int bIdx = Math.Max(1, Math.Min(BodyIndex, compDef.SurfaceBodies.Count));
                var srcBody = compDef.SurfaceBodies[bIdx]; // 1-based

                // Create a transient copy so we don't modify the source
                var copy = Context.TB.Copy(srcBody);

                string partName = System.IO.Path.GetFileNameWithoutExtension(selectedPart.DisplayName);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = $"Ref: {partName}",
                    SourceNodeId = Id,
                    IsFromActivePart = false
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Reference part failed: {ex.Message}";
            }
        }

        public override string? GetDisplaySummary()
        {
            return $"Part #{PartIndex}, Body #{BodyIndex}";
        }

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            // Build the list of open parts for the description
            string partNames = "";
            if (Context?.IsAvailable == true)
            {
                var names = new List<string>();
                var activeDoc = Context.ActivePartDoc;
                int i = 1;
                try
                {
                    foreach (Document doc in Context.App.Documents)
                    {
                        if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
                        {
                            var partDoc = (PartDocument)doc;
                            if (activeDoc != null && partDoc.FullFileName == activeDoc.FullFileName)
                                continue;
                            names.Add($"{i}: {System.IO.Path.GetFileNameWithoutExtension(partDoc.DisplayName)}");
                            i++;
                        }
                    }
                }
                catch { }
                if (names.Count > 0)
                    partNames = string.Join(", ", names);
            }

            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = partNames.Length > 0 ? $"Part Index ({partNames})" : "Part Index",
                    Key = "PartIndex",
                    Value = PartIndex.ToString(),
                    DisplayOnNode = $"Part #{PartIndex}"
                },
                new ParameterDescriptor
                {
                    Label = "Body Index",
                    Key = "BodyIndex",
                    Value = BodyIndex.ToString(),
                    DisplayOnNode = $"Body #{BodyIndex}"
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["PartIndex"] = PartIndex,
                ["BodyIndex"] = BodyIndex
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("PartIndex", out var pi))
            {
                if (pi is int pii) PartIndex = Math.Max(1, pii);
                else if (pi is string ps && int.TryParse(ps, out int piv)) PartIndex = Math.Max(1, piv);
                else PartIndex = Math.Max(1, Convert.ToInt32(pi ?? 1));
            }
            if (parameters.TryGetValue("BodyIndex", out var bi))
            {
                if (bi is int bii) BodyIndex = Math.Max(1, bii);
                else if (bi is string bs && int.TryParse(bs, out int biv)) BodyIndex = Math.Max(1, biv);
                else BodyIndex = Math.Max(1, Convert.ToInt32(bi ?? 1));
            }
        }
    }
}
