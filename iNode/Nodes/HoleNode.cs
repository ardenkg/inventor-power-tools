// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// HoleNode.cs - Full Inventor Hole feature: Drilled, Counterbore, Countersink,
//               Tapped, CBore+Tapped, CSink+Tapped with Through All / To Depth
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates holes using Inventor's full HoleFeatures API.
    /// Supports: Drilled, Counterbore, Countersink, Tapped,
    ///           Counterbore+Tapped, Countersink+Tapped.
    /// Extent: Through All or To Depth.
    /// </summary>
    public class HoleNode : Node
    {
        public override string TypeName => "Hole";
        public override string Title => "Hole";
        public override string Category => "Operations";

        public string HoleType { get; set; } = "Drilled";
        public string Extent { get; set; } = "ThroughAll";
        public string ThreadStandard { get; set; } = "ISO Metric profile";
        public string ThreadDesignation { get; set; } = "M8x1.25";
        public bool RightHand { get; set; } = true;

        public HoleNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Faces", "Placement Face", PortDataType.Face, null);
            AddInput("Diameter", "Diameter (mm)", PortDataType.Number, 5.0);
            AddInput("Depth", "Depth (mm)", PortDataType.Number, 10.0);
            AddInput("CBoreDia", "C'Bore Dia (mm)", PortDataType.Number, 10.0);
            AddInput("CBoreDepth", "C'Bore Depth (mm)", PortDataType.Number, 3.0);
            AddInput("CSinkDia", "C'Sink Dia (mm)", PortDataType.Number, 10.0);
            AddInput("CSinkAngle", "C'Sink Angle (°)", PortDataType.Number, 82.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var facesVal = GetInput("Faces")?.GetEffectiveValue() as FaceListData;
            var diameter = GetInput("Diameter")!.GetDouble(5.0);
            var depth = GetInput("Depth")!.GetDouble(10.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }
            if (diameter <= 0)
            {
                HasError = true;
                ErrorMessage = "Diameter must be positive";
                return;
            }

            var faceSnapshots = new List<FaceSnapshot>();
            var liveFaces = new List<object>();
            if (facesVal != null)
            {
                foreach (var f in facesVal.Faces)
                {
                    try
                    {
                        faceSnapshots.Add(FaceSnapshot.FromInventorFace(f));
                        liveFaces.Add(f);
                    }
                    catch { }
                }
            }

            var result = srcBody.CloneMetadata();
            result.Description = $"Hole ({HoleType} \u00D8{diameter:F1}mm)";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingHole
            {
                HoleType = HoleType,
                Extent = Extent,
                Diameter = diameter,
                Depth = depth,
                CounterboreDiameter = GetInput("CBoreDia")!.GetDouble(10.0),
                CounterboreDepth = GetInput("CBoreDepth")!.GetDouble(3.0),
                CountersinkDiameter = GetInput("CSinkDia")!.GetDouble(10.0),
                CountersinkAngle = GetInput("CSinkAngle")!.GetDouble(82.0),
                ThreadStandard = ThreadStandard,
                ThreadDesignation = ThreadDesignation,
                IsRightHand = RightHand,
                FaceSnapshots = faceSnapshots,
                LiveFaces = liveFaces,
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }

        public override string? GetDisplaySummary() => $"{HoleType} ({Extent})";

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            var parms = new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Hole Type", Key = "HoleType", Value = HoleType,
                    Choices = new[] { "Drilled", "Counterbore", "Countersink",
                                     "Tapped", "CBore+Tapped", "CSink+Tapped" }
                },
                new ParameterDescriptor
                {
                    Label = "Extent", Key = "Extent", Value = Extent,
                    Choices = new[] { "ThroughAll", "ToDepth" }
                }
            };

            if (HoleType.Contains("Tapped"))
            {
                parms.Add(new ParameterDescriptor
                {
                    Label = "Thread Standard", Key = "ThreadStandard", Value = ThreadStandard,
                    Choices = new[] { "ISO Metric profile", "ANSI Unified Screw Threads",
                                     "ISO Metric Trapezoidal Threads", "ACME General Purpose" }
                });
                parms.Add(new ParameterDescriptor
                {
                    Label = "Thread Size", Key = "ThreadDesignation", Value = ThreadDesignation,
                    Choices = GetThreadDesignations()
                });
                parms.Add(new ParameterDescriptor
                {
                    Label = "Right Hand", Key = "RightHand",
                    Value = RightHand ? "Yes" : "No",
                    Choices = new[] { "Yes", "No" }
                });
            }

            return parms;
        }

        private string[] GetThreadDesignations()
        {
            if (ThreadStandard.Contains("Metric profile"))
            {
                return new[]
                {
                    "M3x0.5", "M4x0.7", "M5x0.8", "M6x1", "M8x1.25", "M8x1",
                    "M10x1.5", "M10x1.25", "M12x1.75", "M12x1.5",
                    "M14x2", "M16x2", "M20x2.5", "M24x3", "M30x3.5"
                };
            }
            else if (ThreadStandard.Contains("Unified"))
            {
                return new[]
                {
                    "1/4-20 UNC", "5/16-18 UNC", "3/8-16 UNC",
                    "1/2-13 UNC", "5/8-11 UNC", "3/4-10 UNC", "1-8 UNC"
                };
            }
            return new[] { ThreadDesignation };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["HoleType"] = HoleType,
                ["Extent"] = Extent,
                ["ThreadStandard"] = ThreadStandard,
                ["ThreadDesignation"] = ThreadDesignation,
                ["RightHand"] = RightHand
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("HoleType", out var h) && h is string hs) HoleType = hs;
            if (parameters.TryGetValue("Extent", out var e) && e is string es) Extent = es;
            if (parameters.TryGetValue("ThreadStandard", out var ts) && ts is string tss) ThreadStandard = tss;
            if (parameters.TryGetValue("ThreadDesignation", out var td) && td is string tds) ThreadDesignation = tds;
            if (parameters.TryGetValue("RightHand", out var r))
            {
                if (r is bool rb) RightHand = rb;
                else if (r is string rs) RightHand = rs == "Yes";
            }
        }
    }
}
