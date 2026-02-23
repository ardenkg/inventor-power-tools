// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeFactory.cs - Creates node instances by type name
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using iNode.Nodes;

namespace iNode.Core
{
    /// <summary>
    /// Factory for creating node instances. Maintains a registry of available node types.
    /// </summary>
    public static class NodeFactory
    {
        /// <summary>
        /// Registry entry for a node type.
        /// </summary>
        public class NodeRegistration
        {
            public string TypeName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Category { get; set; } = "";
            public Func<Node> Creator { get; set; } = null!;
        }

        private static readonly List<NodeRegistration> _registry = new List<NodeRegistration>();

        /// <summary>
        /// Gets all registered node types.
        /// </summary>
        public static IReadOnlyList<NodeRegistration> Registry => _registry;

        /// <summary>
        /// Initializes the factory with all built-in node types.
        /// </summary>
        static NodeFactory()
        {
            // Input Parameters
            Register("NumberSlider", "Number Slider", "Input", () => new NumberSliderNode());
            Register("Number", "Number", "Input", () => new NumberNode());
            Register("Point3D", "Point (X,Y,Z)", "Input", () => new Point3DNode());
            Register("SketchReference", "Sketch Reference", "Input", () => new SketchReferenceNode());
            Register("WorkPlane", "Work Plane", "Input", () => new WorkPlaneNode());
            Register("OriginAxis", "Origin Axis", "Input", () => new OriginAxisNode());

            // Sketch — create profile geometry on planes
            Register("SketchCircle", "Sketch Circle", "Sketch", () => new SketchCircleNode());
            Register("SketchRectangle", "Sketch Rectangle", "Sketch", () => new SketchRectangleNode());
            Register("SketchLine", "Sketch Line", "Sketch", () => new SketchLineNode());
            Register("SketchPolygon", "Sketch Polygon", "Sketch", () => new SketchPolygonNode());
            Register("SketchSlot", "Sketch Slot", "Sketch", () => new SketchSlotNode());
            Register("SketchEllipse", "Sketch Ellipse", "Sketch", () => new SketchEllipseNode());

            // Math Operations
            Register("Add", "Add", "Math", () => new AddNode());
            Register("Subtract", "Subtract", "Math", () => new SubtractNode());
            Register("Multiply", "Multiply", "Math", () => new MultiplyNode());
            Register("Divide", "Divide", "Math", () => new DivideNode());
            Register("Negate", "Negate", "Math", () => new NegateNode());
            Register("Abs", "Abs", "Math", () => new AbsNode());
            Register("MinMax", "Min / Max", "Math", () => new MinMaxNode());
            Register("Modulo", "Modulo", "Math", () => new ModuloNode());
            Register("Power", "Power", "Math", () => new PowerNode());
            Register("Trig", "Trig", "Math", () => new TrigNode());
            Register("Round", "Round", "Math", () => new RoundNode());
            Register("PI", "PI / Deg\u2194Rad", "Math", () => new PINode());
            Register("Range", "Range", "Math", () => new RangeNode());
            Register("Random", "Random", "Math", () => new RandomNode());
            Register("Vector", "Vector", "Math", () => new VectorNode());

            // Logic
            Register("Compare", "Compare", "Logic", () => new CompareNode());
            Register("Conditional", "Conditional", "Logic", () => new ConditionalNode());

            // Geometry Creation
            Register("Box", "Box", "Geometry", () => new BoxNode());
            Register("Cylinder", "Cylinder", "Geometry", () => new CylinderNode());
            Register("Sphere", "Sphere", "Geometry", () => new SphereNode());
            Register("Cone", "Cone", "Geometry", () => new ConeNode());
            Register("Torus", "Torus", "Geometry", () => new TorusNode());
            Register("Pipe", "Pipe", "Geometry", () => new PipeNode());
            Register("Coil", "Coil", "Geometry", () => new CoilNode());
            Register("Boolean", "Boolean", "Geometry", () => new BooleanNode());
            Register("MergeBodies", "Merge Bodies", "Geometry", () => new MergeBodiesNode());
            Register("ActiveBody", "Active Body", "Geometry", () => new ActiveBodyNode());
            Register("ReferencePart", "Reference Part", "Geometry", () => new ReferencePartNode());
            Register("Extrude", "Extrude", "Geometry", () => new ExtrudeNode());
            Register("Revolve", "Revolve", "Geometry", () => new RevolveNode());

            // Topology — decompose bodies into faces/edges, filter, pick items
            Register("DeconstructBody", "Deconstruct Body", "Topology", () => new DeconstructBodyNode());
            Register("BodyCentroid", "Body Centroid", "Topology", () => new BodyCentroidNode());
            Register("BodyCount", "Body Count", "Topology", () => new BodyCountNode());
            Register("ListItem", "List Item", "Topology", () => new ListItemNode());
            Register("FilterEdges", "Filter Edges", "Topology", () => new FilterEdgesNode());
            Register("FilterFaces", "Filter Faces", "Topology", () => new FilterFacesNode());
            Register("SelectEdge", "Select Edge", "Topology", () => new SelectEdgeNode());
            Register("SelectFace", "Select Face", "Topology", () => new SelectFaceNode());

            // Operations — feature-level operations (fillet, chamfer, color, shell, hole, etc.)
            Register("Fillet", "Fillet", "Operations", () => new FilletNode());
            Register("Chamfer", "Chamfer", "Operations", () => new ChamferNode());
            Register("ColorFaces", "Color Faces", "Operations", () => new ColorFacesNode());
            Register("Shell", "Shell", "Operations", () => new ShellNode());
            Register("Hole", "Hole", "Operations", () => new HoleNode());
            Register("Draft", "Draft", "Operations", () => new DraftNode());
            Register("Thicken", "Thicken", "Operations", () => new ThickenNode());
            Register("SplitBody", "Split Body", "Operations", () => new SplitBodyNode());

            // Transformations
            Register("Move", "Move", "Transform", () => new MoveNode());
            Register("Rotate", "Rotate", "Transform", () => new RotateNode());
            Register("Mirror", "Mirror", "Transform", () => new MirrorNode());
            Register("Scale", "Scale", "Transform", () => new ScaleNode());
            Register("PatternLinear", "Linear Pattern", "Transform", () => new PatternLinearNode());
            Register("PatternCircular", "Circular Pattern", "Transform", () => new PatternCircularNode());

            // Measurements & Analysis
            Register("Distance", "Distance", "Measure", () => new DistanceNode());
            Register("AngleBetween", "Angle Between", "Measure", () => new AngleBetweenNode());
            Register("Area", "Area", "Measure", () => new AreaNode());
            Register("Volume", "Volume", "Measure", () => new VolumeNode());
            Register("BoundingBox", "Bounding Box", "Measure", () => new BoundingBoxNode());

            // Points & Vectors
            Register("CrossProduct", "Cross Product", "Vector", () => new CrossProductNode());
            Register("DotProduct", "Dot Product", "Vector", () => new DotProductNode());
            Register("Normalize", "Normalize", "Vector", () => new NormalizeNode());
            Register("DecomposePoint", "Decompose Point", "Vector", () => new DecomposePointNode());
            Register("VectorAdd", "Vector Add / Subtract", "Vector", () => new VectorAddNode());
            Register("VectorScale", "Vector Scale", "Vector", () => new VectorScaleNode());

            // Utility
            Register("Display", "Display", "Utility", () => new DisplayNode());
            Register("ListViewer", "List Viewer", "Utility", () => new ListViewerNode());
            Register("Note", "Note", "Utility", () => new NoteNode());
            Register("Relay", "Relay", "Utility", () => new RelayNode());

            // Output
            Register("Bake", "Bake", "Output", () => new BakeNode());

            // List / Tree operations
            Register("CreateList", "Create List", "Utility", () => new CreateListNode());
            Register("ListGet", "List Get", "Utility", () => new ListGetNode());
            Register("ListLength", "List Length", "Utility", () => new ListLengthNode());
            Register("ListReverse", "List Reverse", "Utility", () => new ListReverseNode());
            Register("Graft", "Graft", "Utility", () => new GraftNode());
            Register("Flatten", "Flatten", "Utility", () => new FlattenNode());
            Register("Repeat", "Repeat", "Utility", () => new RepeatNode());
        }

        /// <summary>
        /// Registers a node type.
        /// </summary>
        public static void Register(string typeName, string displayName, string category, Func<Node> creator)
        {
            _registry.Add(new NodeRegistration
            {
                TypeName = typeName,
                DisplayName = displayName,
                Category = category,
                Creator = creator
            });
        }

        /// <summary>
        /// Creates a node instance by type name.
        /// </summary>
        public static Node? Create(string typeName)
        {
            var reg = _registry.FirstOrDefault(r => r.TypeName == typeName);
            return reg?.Creator();
        }

        /// <summary>
        /// Gets all registered types grouped by category.
        /// </summary>
        public static Dictionary<string, List<NodeRegistration>> GetByCategory()
        {
            return _registry
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Searches for nodes matching a query string.
        /// </summary>
        public static List<NodeRegistration> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _registry.ToList();

            var lower = query.ToLowerInvariant();
            return _registry
                .Where(r => r.DisplayName.ToLowerInvariant().Contains(lower)
                         || r.TypeName.ToLowerInvariant().Contains(lower)
                         || r.Category.ToLowerInvariant().Contains(lower))
                .ToList();
        }
    }
}
