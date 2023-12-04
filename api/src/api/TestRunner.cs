namespace GdUnit4.Api;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using GdUnit4.Executions;
using GdUnit4.Core;

partial class TestRunner : Godot.Node
{

    public class Options
    {
        [Option(Required = false, HelpText = "If failfast=true the test run will abort on first test failure.")]
        public bool FailFast { get; set; } = true;

        [Option(Required = false, HelpText = "Runs the Runner in test adapter mode.")]
        public bool TestAdapter { get; set; }

        [Option(Required = false, HelpText = "The list of test-suites to execute.")]
        public IEnumerable<string> TestSuites { get; set; } = new List<string>();

        [Option(Required = false, HelpText = "Adds the given test suite or directory to the execution pipeline.")]
        public string Add { get; set; } = "";
    }

    private bool FailFast { get; set; } = true;

    public async Task RunTests()
    {
        var cmdArgs = Godot.OS.GetCmdlineArgs();

        await new Parser(with =>
        {
            with.EnableDashDash = true;
            with.IgnoreUnknownArguments = true;
        })
           .ParseArguments<Options>(cmdArgs)
           .WithParsedAsync(async o =>
           {
               FailFast = o.FailFast;
               var exitCode = await (o.TestAdapter
                ? RunTests(LoadTestSuites(o.TestSuites), new TestAdapterReporter())
                : RunTests(LoadTestSuites(new DirectoryInfo(o.Add)), new TestReporter()));
               Console.WriteLine($"Testrun ends with exit code: {exitCode}, FailFast:{FailFast}");
               GetTree().Quit(exitCode);
           });
    }

    private async Task<int> RunTests(IEnumerable<TestSuite> testSuites, ITestEventListener listener)
    {
        if (!testSuites.Any())
        {
            Console.Error.WriteLine("No testsuite's specified!, Abort!");
            return -1;
        }
        using Executor executor = new();
        executor.AddTestEventListener(listener);

        foreach (var testSuite in testSuites)
        {
            await executor.ExecuteInternally(testSuite!);
            if (listener.IsFailed && FailFast)
                break;
        }
        return listener.IsFailed ? 100 : 0;
    }

    private static List<TestSuite> LoadTestSuites(IEnumerable<string> testSuites)
    {
        List<TestSuite> acc = new();
        foreach (var path in testSuites)
        {
            var testSuitePath = path.TrimStart('\'').TrimEnd('\'');
            Type? type = GdUnitTestSuiteBuilder.ParseType(testSuitePath);
            if (type != null && IsTestSuite(type))
                acc.Add(new TestSuite(testSuitePath));
            else
                Console.Error.WriteLine($"Can't load testsuite {testSuitePath}!, Skipp it!");
        }
        return acc;
    }

    private static List<TestSuite> LoadTestSuites(DirectoryInfo rootDir)
    {
        List<TestSuite> acc = new();
        Stack<DirectoryInfo> stack = new();
        stack.Push(rootDir);

        while (stack.Count > 0)
        {
            DirectoryInfo currentDir = stack.Pop();
            Console.WriteLine($"Scanning for test suites in: {currentDir.FullName}");

            foreach (var file in currentDir.GetFiles("*.cs"))
            {
                Type? type = GdUnitTestSuiteBuilder.ParseType(file.FullName);
                if (type != null && IsTestSuite(type))
                    acc.Add(new TestSuite(file.FullName));
            }

            foreach (var directory in currentDir.GetDirectories())
                stack.Push(directory);
        }
        return acc;
    }

    private static bool IsTestSuite(Type type) =>
        type.IsClass && !type.IsAbstract && Attribute.IsDefined(type, typeof(TestSuiteAttribute));

}
