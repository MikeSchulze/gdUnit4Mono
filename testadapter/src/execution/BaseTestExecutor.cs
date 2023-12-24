using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;

using GdUnit4.Api;
using Godot;

namespace GdUnit4.TestAdapter.Execution;

internal abstract class BaseTestExecutor
{

    protected string GodotBin { get; set; } = System.Environment.GetEnvironmentVariable("GODOT_BIN")
        ?? throw new Exception("Godot runtime is not set! Set evn 'GODOT_BIN' is missing!");

    protected static EventHandler ExitHandler(IFrameworkHandle frameworkHandle) => new((sender, e)
        => frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Exited: {e}"));

    protected static DataReceivedEventHandler StdErrorProcessor(IFrameworkHandle frameworkHandle) => new((sender, args) =>
    {
        var message = args.Data?.Trim();
        if (string.IsNullOrEmpty(message))
            return;
        frameworkHandle.SendMessage(TestMessageLevel.Error, $"stderr: {message}");
    });

    protected static string WriteTestRunnerConfig(Dictionary<string, List<TestCase>> groupedTestSuites)
    {
        var fileName = $"GdUnitRunner_{Guid.NewGuid()}.cfg";
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        var testConfig = new TestRunnerConfig();
        foreach (var testSuite in groupedTestSuites)
        {
            testConfig.Included.Add(testSuite.Key, testSuite.Value.Select(t => new TestCaseConfig() { Name = t.DisplayName }));
        }
        var jsonContent = JsonConvert.SerializeObject(testConfig, Formatting.Indented);
        File.WriteAllText(filePath, jsonContent);
        return filePath;
    }


    protected static void AttachDebuggerIfNeed(IRunContext runContext, IFrameworkHandle frameworkHandle, Process process)
    {
        if (runContext.IsBeingDebugged && frameworkHandle is IFrameworkHandle2 fh2)
            fh2.AttachDebuggerToProcess(pid: process.Id);
    }

    protected static DataReceivedEventHandler TestEventProcessor(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> tests) => new((sender, args) =>
    {
        var json = args.Data?.Trim();
        if (string.IsNullOrEmpty(json))
            return;

        if (json.StartsWith("GdUnitTestEvent:"))
        {
            json = json.TrimPrefix("GdUnitTestEvent:");
            TestEvent e = JsonConvert.DeserializeObject<TestEvent>(json)!;

            switch (e.Type)
            {
                case TestEvent.TYPE.TESTSUITE_BEFORE:
                    //frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Execute Test Suite '{e.SuiteName}'");
                    break;
                case TestEvent.TYPE.TESTCASE_BEFORE:
                    {
                        var testCase = tests.FirstOrDefault(t => t.FullyQualifiedName.EndsWith(e.FullyQualifiedName));
                        if (testCase == null)
                        {
                            frameworkHandle.SendMessage(TestMessageLevel.Error, $"TESTCASE_BEFORE: cant find test case {e.FullyQualifiedName}");
                            return;
                        }
                        frameworkHandle.RecordStart(testCase);
                    }
                    break;
                case TestEvent.TYPE.TESTCASE_AFTER:
                    {
                        var testCase = tests.FirstOrDefault(t => t.FullyQualifiedName.EndsWith(e.FullyQualifiedName));
                        if (testCase == null)
                        {
                            frameworkHandle.SendMessage(TestMessageLevel.Error, $"TESTCASE_AFTER: cant find test case {e.FullyQualifiedName}");
                            return;
                        }
                        var testResult = new TestResult(testCase)
                        {
                            DisplayName = testCase.DisplayName,
                            Outcome = e.AsTestOutcome(),
                            EndTime = DateTimeOffset.Now,
                            Duration = e.ElapsedInMs
                        };
                        foreach (var report in e.Reports)
                        {
                            testResult.ErrorMessage = report.Message.RichTextNormalize();
                            testResult.ErrorStackTrace = $"StackTrace    at {testCase.FullyQualifiedName}() in {testCase.CodeFilePath}:line {report.LineNumber}";
                        }
                        frameworkHandle.RecordResult(testResult);
                        frameworkHandle.RecordEnd(testCase, testResult.Outcome);
                    }
                    break;
                case TestEvent.TYPE.TESTSUITE_AFTER:
                    //frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run Test Suite {e.SuiteName} {e.AsTestOutcome()}");
                    break;
            }
            return;
        }
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"stdout: {json}");
    });

    protected static string? LookupGodotProjectPath(string classPath)
    {
        DirectoryInfo? currentDir = new DirectoryInfo(classPath).Parent;
        while (currentDir != null)
        {
            if (currentDir.EnumerateFiles("project.godot").Any())
                return currentDir.FullName;
            currentDir = currentDir.Parent;
        }
        return null;
    }
}
