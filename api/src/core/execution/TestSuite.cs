using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using GdUnit4.Core;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GdUnit4.Executions
{
    internal sealed class TestSuite : IDisposable
    {
        private Lazy<IEnumerable<Executions.TestCase>> _testCases = new Lazy<IEnumerable<Executions.TestCase>>();

        public int TestCaseCount => TestCases.Count<Executions.TestCase>();

        public IEnumerable<Executions.TestCase> TestCases => _testCases.Value;

        public string ResourcePath { get; set; }

        public string Name { get; set; }

        public string? FullName => GetType().FullName;

        public object Instance { get; set; }

        public bool FilterDisabled { get; set; } = false;

        public TestSuite(string classPath, IEnumerable<string>? includedTests = null, bool checkIfTestSuite = true)
        {
            Type? type = GdUnitTestSuiteBuilder.ParseType(classPath, checkIfTestSuite)
                ?? throw new ArgumentException($"Can't parse testsuite {classPath}");
            Instance = Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Cannot create an instance of '{type.FullName}' because it does not have a public parameterless constructor.");

            Name = type.Name;
            ResourcePath = classPath;
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(classPath)).WithFilePath(classPath).GetCompilationUnitRoot();
            // we do lazy loading to only load test case one times
            _testCases = new Lazy<IEnumerable<TestCase>>(() => LoadTestCases(type, syntaxTree, includedTests));
        }

        private IEnumerable<Executions.TestCase> LoadTestCases(Type type, CompilationUnitSyntax? syntaxTree, IEnumerable<string>? includedTests = null)
        {
            return type.GetMethods()
                .Where(m => m.IsDefined(typeof(TestCaseAttribute)))
                .Where(FilterByTestCaseName(includedTests))
                .Select(mi =>
                {
                    var lineNumber = syntaxTree != null ? GdUnitTestSuiteBuilder.TestCaseLineNumber(syntaxTree, mi.Name) : -1;
                    return new Executions.TestCase(mi, lineNumber);
                })
                .ToList();
        }

        private Func<MethodInfo, bool> FilterByTestCaseName(IEnumerable<string>? includedTests) =>
            (mi) => includedTests?.Any((testName) =>
            {
                var testCases = mi.GetCustomAttributes(typeof(TestCaseAttribute))
                    .Cast<TestCaseAttribute>()
                    .Where(attr => attr != null && (attr.Arguments?.Any() ?? false))
                    .Select(attr => $"{attr.TestName ?? mi.Name}({attr.Arguments.Formated()})")
                    .DefaultIfEmpty($"{mi.Name}");
                return testCases.Contains(testName);
            }) ?? true;

        public void Dispose()
        {
            if (Instance is IDisposable)
                ((IDisposable)Instance).Dispose();
        }
    }
}
