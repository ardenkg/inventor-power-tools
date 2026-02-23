// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeTestRunner.cs - Automated test harness for all nodes
// Runs without Inventor to verify node instantiation, ports, and basic compute.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using iNode.Core;
using iNode.Nodes;

namespace iNode
{
    /// <summary>
    /// Automated test runner for all registered nodes.
    /// Tests run without Inventor — verifying node structure, port definitions,
    /// serialization, and compute behavior with default/null inputs.
    /// 
    /// Usage: Call NodeTestRunner.RunAllTests() to get a TestReport.
    /// Can be invoked from the iNode UI via a "Run Tests" developer button,
    /// or from a console test harness.
    /// </summary>
    public static class NodeTestRunner
    {
        /// <summary>
        /// Result of a single test case.
        /// </summary>
        public class TestResult
        {
            public string NodeType { get; set; } = "";
            public string TestName { get; set; } = "";
            public bool Passed { get; set; }
            public string Message { get; set; } = "";
            public TimeSpan Duration { get; set; }
        }

        /// <summary>
        /// Summary report of all tests.
        /// </summary>
        public class TestReport
        {
            public List<TestResult> Results { get; set; } = new();
            public int TotalTests => Results.Count;
            public int Passed => Results.Count(r => r.Passed);
            public int Failed => Results.Count(r => !r.Passed);
            public TimeSpan TotalDuration { get; set; }

            public string GetSummary()
            {
                var lines = new List<string>
                {
                    "═══════════════════════════════════════════════════",
                    "  iNode Automated Test Report",
                    "═══════════════════════════════════════════════════",
                    $"  Total: {TotalTests}  |  Passed: {Passed}  |  Failed: {Failed}",
                    $"  Duration: {TotalDuration.TotalMilliseconds:F0}ms",
                    "═══════════════════════════════════════════════════"
                };

                if (Failed > 0)
                {
                    lines.Add("");
                    lines.Add("  FAILURES:");
                    foreach (var fail in Results.Where(r => !r.Passed))
                    {
                        lines.Add($"  ✗ [{fail.NodeType}] {fail.TestName}");
                        lines.Add($"    → {fail.Message}");
                    }
                }

                lines.Add("");
                lines.Add("  ALL NODES:");

                // Group by category
                var nodeTypes = NodeFactory.GetByCategory();
                foreach (var cat in nodeTypes.OrderBy(kv => kv.Key))
                {
                    lines.Add($"  ── {cat.Key} ──");
                    foreach (var reg in cat.Value)
                    {
                        var nodeResults = Results.Where(r => r.NodeType == reg.TypeName).ToList();
                        int nodePassed = nodeResults.Count(r => r.Passed);
                        int nodeTotal = nodeResults.Count;
                        string status = nodeResults.All(r => r.Passed) ? "✓" : "✗";
                        lines.Add($"    {status} {reg.DisplayName} ({nodePassed}/{nodeTotal} tests)");
                    }
                }

                lines.Add("");
                lines.Add("═══════════════════════════════════════════════════");

                return string.Join(Environment.NewLine, lines);
            }
        }

        /// <summary>
        /// Runs all tests for all registered node types.
        /// </summary>
        public static TestReport RunAllTests()
        {
            var report = new TestReport();
            var sw = Stopwatch.StartNew();

            // Access Registry to trigger static constructor
            var _ = NodeFactory.Registry;

            var allTypes = NodeFactory.GetByCategory()
                .SelectMany(kv => kv.Value)
                .ToList();

            foreach (var reg in allTypes)
            {
                try
                {
                    var node = NodeFactory.Create(reg.TypeName);
                    if (node == null)
                    {
                        report.Results.Add(new TestResult
                        {
                            NodeType = reg.TypeName,
                            TestName = "Factory.Create",
                            Passed = false,
                            Message = "NodeFactory.Create returned null"
                        });
                        continue;
                    }

                    // Run test suite for this node
                    report.Results.AddRange(TestInstantiation(node));
                    report.Results.AddRange(TestPortDefinitions(node));
                    report.Results.AddRange(TestDefaultCompute(node));
                    report.Results.AddRange(TestParameterSerialization(node));
                    report.Results.AddRange(TestWidthComputation(node));
                    report.Results.AddRange(TestMathCompute(node));
                    report.Results.AddRange(TestListCompute(node));
                }
                catch (Exception ex)
                {
                    report.Results.Add(new TestResult
                    {
                        NodeType = reg.TypeName,
                        TestName = "Setup",
                        Passed = false,
                        Message = $"Exception during setup: {ex.Message}"
                    });
                }
            }

            sw.Stop();
            report.TotalDuration = sw.Elapsed;
            return report;
        }

        // ============================================================
        // Test: Node instantiation
        // ============================================================
        private static List<TestResult> TestInstantiation(Node node)
        {
            var results = new List<TestResult>();
            var sw = Stopwatch.StartNew();

            // TypeName is not empty
            results.Add(Test(node.TypeName, "TypeName not empty",
                !string.IsNullOrEmpty(node.TypeName),
                $"TypeName is '{node.TypeName}'"));

            // Title is not empty
            results.Add(Test(node.TypeName, "Title not empty",
                !string.IsNullOrEmpty(node.Title),
                $"Title is '{node.Title}'"));

            // Category is valid
            var validCategories = new HashSet<string>
            {
                "Input", "Math", "Logic", "Geometry", "Sketch",
                "Topology", "Operations", "Transform", "Measure",
                "Vector", "Utility"
            };
            results.Add(Test(node.TypeName, "Category valid",
                validCategories.Contains(node.Category),
                $"Category '{node.Category}' not in valid set"));

            // Has a valid GUID
            results.Add(Test(node.TypeName, "Has valid ID",
                node.Id != Guid.Empty, "ID is empty GUID"));

            // HeaderColor is not default black
            results.Add(Test(node.TypeName, "Has header color",
                node.HeaderColor.R > 0 || node.HeaderColor.G > 0 || node.HeaderColor.B > 0,
                $"HeaderColor is {node.HeaderColor}"));

            return results;
        }

        // ============================================================
        // Test: Port definitions
        // ============================================================
        private static List<TestResult> TestPortDefinitions(Node node)
        {
            var results = new List<TestResult>();

            // Check all input ports have names
            foreach (var input in node.Inputs)
            {
                results.Add(Test(node.TypeName, $"Input '{input.Name}' has name",
                    !string.IsNullOrEmpty(input.Name),
                    "Port name is empty"));

                results.Add(Test(node.TypeName, $"Input '{input.Name}' has display name",
                    !string.IsNullOrEmpty(input.DisplayName),
                    "Port display name is empty"));
            }

            // Check all output ports have names
            foreach (var output in node.Outputs)
            {
                results.Add(Test(node.TypeName, $"Output '{output.Name}' has name",
                    !string.IsNullOrEmpty(output.Name),
                    "Port name is empty"));

                results.Add(Test(node.TypeName, $"Output '{output.Name}' has display name",
                    !string.IsNullOrEmpty(output.DisplayName),
                    "Port display name is empty"));
            }

            // No duplicate port names within inputs
            var inputNames = node.Inputs.Select(p => p.Name).ToList();
            results.Add(Test(node.TypeName, "No duplicate input names",
                inputNames.Distinct().Count() == inputNames.Count,
                $"Duplicate input names: {string.Join(", ", inputNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key))}"));

            // No duplicate port names within outputs
            var outputNames = node.Outputs.Select(p => p.Name).ToList();
            results.Add(Test(node.TypeName, "No duplicate output names",
                outputNames.Distinct().Count() == outputNames.Count,
                $"Duplicate output names: {string.Join(", ", outputNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key))}"));

            return results;
        }

        // ============================================================
        // Test: Default compute (with no Inventor context)
        // ============================================================
        private static List<TestResult> TestDefaultCompute(Node node)
        {
            var results = new List<TestResult>();

            // Create a dummy graph for execution context
            var graph = new NodeGraph();
            graph.AddNode(node);

            try
            {
                // Execute with default values (no Inventor context)
                node.Execute(graph);

                // For most geometry/operation nodes, they should either:
                // - Set HasError=true with a message (expected for nodes needing Inventor)
                // - Complete without crash
                // Either outcome is acceptable — the test is that it doesn't throw

                results.Add(Test(node.TypeName, "Compute doesn't crash",
                    true, ""));

                // If it set an error, the error message should be non-empty
                if (node.HasError)
                {
                    results.Add(Test(node.TypeName, "Error has message",
                        !string.IsNullOrEmpty(node.ErrorMessage),
                        "HasError=true but ErrorMessage is empty"));
                }
            }
            catch (Exception ex)
            {
                results.Add(Test(node.TypeName, "Compute doesn't crash",
                    false, $"Exception: {ex.Message}"));
            }

            return results;
        }

        // ============================================================
        // Test: Parameter serialization round-trip
        // ============================================================
        private static List<TestResult> TestParameterSerialization(Node node)
        {
            var results = new List<TestResult>();

            try
            {
                var parameters = node.GetParameters();
                if (parameters != null && parameters.Count > 0)
                {
                    // Should be able to set parameters back without crash
                    node.SetParameters(parameters);

                    results.Add(Test(node.TypeName, "Parameter round-trip",
                        true, ""));

                    // Parameters should survive round-trip
                    var after = node.GetParameters();
                    results.Add(Test(node.TypeName, "Parameter count preserved",
                        after.Count == parameters.Count,
                        $"Before: {parameters.Count}, After: {after.Count}"));
                }
                else
                {
                    results.Add(Test(node.TypeName, "No parameters (OK)",
                        true, ""));
                }
            }
            catch (Exception ex)
            {
                results.Add(Test(node.TypeName, "Parameter serialization",
                    false, $"Exception: {ex.Message}"));
            }

            return results;
        }

        // ============================================================
        // Test: Width computation
        // ============================================================
        private static List<TestResult> TestWidthComputation(Node node)
        {
            var results = new List<TestResult>();

            try
            {
                int width = node.NodeWidth;
                results.Add(Test(node.TypeName, "Width > 0",
                    width > 0, $"Width is {width}"));
                results.Add(Test(node.TypeName, "Width reasonable",
                    width >= 100 && width <= 2000,
                    $"Width {width} seems unreasonable"));
            }
            catch (Exception ex)
            {
                results.Add(Test(node.TypeName, "Width computation",
                    false, $"Exception: {ex.Message}"));
            }

            return results;
        }

        // ============================================================
        // Test: Math node value correctness
        // ============================================================
        private static List<TestResult> TestMathCompute(Node node)
        {
            var results = new List<TestResult>();
            if (node.Category != "Math") return results;

            // Create fresh instance for clean state
            var fresh = NodeFactory.Create(node.TypeName);
            if (fresh == null) return results;

            var graph = new NodeGraph();
            graph.AddNode(fresh);

            // Set known input values via DefaultValue so Execute() doesn't reset them
            var inputA = fresh.GetInput("A");
            var inputB = fresh.GetInput("B");

            if (inputA != null) { inputA.Value = 10.0; inputA.DefaultValue = 10.0; }
            if (inputB != null) { inputB.Value = 3.0; inputB.DefaultValue = 3.0; }

            // Special handling for nodes with different input names
            var inputVal = fresh.GetInput("Value");
            if (inputVal != null) { inputVal.Value = 16.0; inputVal.DefaultValue = 16.0; }

            var inputBase = fresh.GetInput("Base");
            if (inputBase != null) { inputBase.Value = 2.0; inputBase.DefaultValue = 2.0; }

            var inputExp = fresh.GetInput("Exponent");
            if (inputExp != null) { inputExp.Value = 8.0; inputExp.DefaultValue = 8.0; }

            try
            {
                fresh.Execute(graph);

                var result = fresh.GetOutput("Result");
                if (result?.Value != null && result.Value is double d)
                {
                    double expected = double.NaN;
                    switch (fresh.TypeName)
                    {
                        case "Add": expected = 13.0; break;
                        case "Subtract": expected = 7.0; break;
                        case "Multiply": expected = 30.0; break;
                        case "Divide": expected = 10.0 / 3.0; break;
                        case "Modulo": expected = 1.0; break;
                        case "Abs": expected = 16.0; break;
                        case "Negate": expected = -16.0; break;
                        case "Sqrt": expected = 4.0; break;
                        case "Power": expected = 256.0; break;
                        case "Min": expected = 3.0; break;
                        case "Max": expected = 10.0; break;
                        case "Floor": expected = 16.0; break;
                        case "Ceil": expected = 16.0; break;
                        case "Round": expected = 16.0; break;
                        case "Remap": break; // skip - needs range inputs
                    }

                    if (!double.IsNaN(expected))
                    {
                        bool match = Math.Abs(d - expected) < 1e-9;
                        results.Add(Test(fresh.TypeName, $"Value correctness ({d:F4} == {expected:F4})",
                            match, $"Expected {expected}, got {d}"));
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(Test(fresh.TypeName, "Math compute",
                    false, $"Exception: {ex.Message}"));
            }

            return results;
        }

        // ============================================================
        // Test: List/tree node behavior
        // ============================================================
        private static List<TestResult> TestListCompute(Node node)
        {
            var results = new List<TestResult>();
            string[] listTypes = { "CreateList", "ListGet", "ListLength", "ListReverse", "Graft", "Flatten", "Repeat" };
            if (!listTypes.Contains(node.TypeName)) return results;

            var fresh = NodeFactory.Create(node.TypeName);
            if (fresh == null) return results;

            var graph = new NodeGraph();
            graph.AddNode(fresh);

            try
            {
                switch (fresh.TypeName)
                {
                    case "CreateList":
                    {
                        // Set some inputs via DefaultValue so Execute() doesn't reset
                        var i0 = fresh.GetInput("Item0"); if (i0 != null) { i0.Value = 1.0; i0.DefaultValue = 1.0; }
                        var i1 = fresh.GetInput("Item1"); if (i1 != null) { i1.Value = 2.0; i1.DefaultValue = 2.0; }
                        var i2 = fresh.GetInput("Item2"); if (i2 != null) { i2.Value = 3.0; i2.DefaultValue = 3.0; }
                        fresh.Execute(graph);
                        var output = fresh.GetOutput("List");
                        results.Add(Test("CreateList", "Creates DataList",
                            output?.Value is DataList, $"Output type is {output?.Value?.GetType()?.Name ?? "null"}"));
                        if (output?.Value is DataList dl)
                        {
                            results.Add(Test("CreateList", "Has items",
                                dl.Items.Count >= 3, $"Count is {dl.Items.Count}"));
                        }
                        break;
                    }
                    case "ListLength":
                    {
                        var input = fresh.GetInput("List");
                        var dl4 = DataList.FromItems(1.0, 2.0, 3.0, 4.0);
                        if (input != null) { input.Value = dl4; input.DefaultValue = dl4; }
                        fresh.Execute(graph);
                        var count = fresh.GetOutput("Count");
                        results.Add(Test("ListLength", "Correct count",
                            count?.Value is double d && Math.Abs(d - 4.0) < 1e-9,
                            $"Expected 4, got {count?.Value}"));
                        break;
                    }
                    case "ListReverse":
                    {
                        var input = fresh.GetInput("List");
                        var dl3 = DataList.FromItems(1.0, 2.0, 3.0);
                        if (input != null) { input.Value = dl3; input.DefaultValue = dl3; }
                        fresh.Execute(graph);
                        var output = fresh.GetOutput("List");
                        if (output?.Value is DataList rev)
                        {
                            results.Add(Test("ListReverse", "First is last",
                                rev.Items.Count == 3 && rev.Items[0] is double d && Math.Abs(d - 3.0) < 1e-9,
                                $"First item is {rev.Items.FirstOrDefault()}"));
                        }
                        else
                        {
                            results.Add(Test("ListReverse", "Returns DataList",
                                false, $"Output is {output?.Value?.GetType()?.Name ?? "null"}"));
                        }
                        break;
                    }
                    case "ListGet":
                    {
                        var listIn = fresh.GetInput("List");
                        var idxIn = fresh.GetInput("Index");
                        var dl3g = DataList.FromItems(10.0, 20.0, 30.0);
                        if (listIn != null) { listIn.Value = dl3g; listIn.DefaultValue = dl3g; }
                        if (idxIn != null) { idxIn.Value = 1.0; idxIn.DefaultValue = 1.0; }
                        fresh.Execute(graph);
                        var item = fresh.GetOutput("Item");
                        results.Add(Test("ListGet", "Correct index",
                            item?.Value is double d && Math.Abs(d - 20.0) < 1e-9,
                            $"Expected 20, got {item?.Value}"));
                        break;
                    }
                    case "Repeat":
                    {
                        var valIn = fresh.GetInput("Value");
                        var countIn = fresh.GetInput("Count");
                        if (valIn != null) { valIn.Value = 42.0; valIn.DefaultValue = 42.0; }
                        if (countIn != null) { countIn.Value = 5.0; countIn.DefaultValue = 5.0; }
                        fresh.Execute(graph);
                        var output = fresh.GetOutput("List");
                        if (output?.Value is DataList dl)
                        {
                            results.Add(Test("Repeat", "Correct count",
                                dl.Items.Count == 5, $"Count is {dl.Items.Count}"));
                            results.Add(Test("Repeat", "All same value",
                                dl.Items.All(i => i is double d && Math.Abs(d - 42.0) < 1e-9),
                                "Not all items are 42"));
                        }
                        else
                        {
                            results.Add(Test("Repeat", "Returns DataList",
                                false, $"Output is {output?.Value?.GetType()?.Name ?? "null"}"));
                        }
                        break;
                    }
                    case "Graft":
                    {
                        var input = fresh.GetInput("Data");
                        if (input != null) { input.Value = 7.0; input.DefaultValue = 7.0; }
                        fresh.Execute(graph);
                        var output = fresh.GetOutput("List");
                        results.Add(Test("Graft", "Wraps single value",
                            output?.Value is DataList dl && dl.Items.Count == 1,
                            $"Output is {output?.Value?.GetType()?.Name ?? "null"}"));
                        break;
                    }
                    case "Flatten":
                    {
                        var input = fresh.GetInput("Data");
                        var nested = new DataList();
                        nested.Items.Add(DataList.FromItems(1.0, 2.0));
                        nested.Items.Add(DataList.FromItems(3.0, 4.0));
                        if (input != null) { input.Value = nested; input.DefaultValue = nested; }
                        fresh.Execute(graph);
                        var output = fresh.GetOutput("List");
                        if (output?.Value is DataList flat)
                        {
                            results.Add(Test("Flatten", "Correct count",
                                flat.Items.Count == 4, $"Count is {flat.Items.Count}"));
                        }
                        else
                        {
                            results.Add(Test("Flatten", "Returns DataList",
                                false, $"Output is {output?.Value?.GetType()?.Name ?? "null"}"));
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(Test(fresh.TypeName, "List compute",
                    false, $"Exception: {ex.Message}"));
            }

            return results;
        }

        // ============================================================
        // Helper
        // ============================================================
        private static TestResult Test(string nodeType, string testName, bool passed, string message)
        {
            return new TestResult
            {
                NodeType = nodeType,
                TestName = testName,
                Passed = passed,
                Message = passed ? "OK" : message
            };
        }
    }
}
