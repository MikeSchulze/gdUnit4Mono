using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Newtonsoft.Json;
using Godot;

namespace GdUnit4.TestAdapter;

[ExtensionUri(ExecutorUri)]
public class GdUnit4TestExecutor : ITestExecutor
{

    private Process? pProcess = null;

    ///<summary>
    /// The Uri used to identify the NUnitExecutor
    ///</summary>
    public const string ExecutorUri = "executor://GdUnit4.TestAdapter/v1";


    /// <summary>
    /// Runs only the tests specified by parameter 'tests'. 
    /// </summary>
    /// <param name="tests">Tests to be run.</param>
    /// <param name="runContext">Context to use when executing the tests.</param>
    /// <param param name="frameworkHandle">Handle to the framework to record results and to do framework operations.</param>
    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests == null || frameworkHandle == null || runContext == null) return;
        //CheckIfDebug();

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runContext.RunSettings?.SettingsXml);
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"RunConfiguration: {runConfiguration.TestSessionTimeout}");
        var settings = XmlRunSettingsUtilities.GetTestRunParameters(runContext.RunSettings?.SettingsXml);
        foreach (var key in settings.Keys)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"{key} = '{settings[key]}'");
        }

        var classPath = tests.First().CodeFilePath;
        Console.WriteLine($"classPath: {classPath}");
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Processing: {classPath}");
        using (pProcess = new())
        {
            pProcess.StartInfo.WorkingDirectory = @"D:\develop\workspace\gdUnit4Mono\test";
            pProcess.StartInfo.FileName = @"d:\develop\Godot_v4.1.2-stable_mono_win64\Godot_v4.1.2-stable_mono_win64.exe";
            pProcess.StartInfo.Arguments = $@"-d --path D:\develop\workspace\gdUnit4Mono\test --testadapter --testsuites='{classPath} --verbose'";
            pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.RedirectStandardError = true;
            pProcess.StartInfo.RedirectStandardInput = true;
            pProcess.EnableRaisingEvents = true;
            pProcess.OutputDataReceived += TestEventProcessor(frameworkHandle, tests);
            pProcess.ErrorDataReceived += StdErrorProcessor(frameworkHandle);
            pProcess.Exited += ExitHandler(frameworkHandle);
            pProcess.Start();
            pProcess.BeginErrorReadLine();
            pProcess.BeginOutputReadLine();
            //pProcess.WaitForExit((int)runConfiguration.TestSessionTimeout);
            pProcess.WaitForExit();
        };
    }

    /// <summary>
    /// Runs 'all' the tests present in the specified 'containers'. 
    /// </summary>
    /// <param name="containers">Path to test container files to look for tests in.</param>
    /// <param name="runContext">Context to use when executing the tests.</param>
    /// <param param name="frameworkHandle">Handle to the framework to record results and to do framework operations.</param>
    public void RunTests(IEnumerable<string>? containers, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        frameworkHandle?.SendMessage(TestMessageLevel.Warning, $"RunTests:containers ${containers}");
    }

    /// <summary>
    /// Cancel the execution of the tests.
    /// </summary>
    public void Cancel()
    {
        if (pProcess != null)
        {
            pProcess.Refresh();
            if (pProcess.HasExited)
                return;
            pProcess.Kill(true);
        }
    }


    private static EventHandler ExitHandler(IFrameworkHandle frameworkHandle) => new((sender, e)
        => frameworkHandle.SendMessage(TestMessageLevel.Informational, "Exited: " + e.GetType()));

    private static DataReceivedEventHandler StdErrorProcessor(IFrameworkHandle frameworkHandle) => new((sender, args) =>
    {
        var message = args.Data?.Trim();
        if (string.IsNullOrEmpty(message))
            return;
        frameworkHandle.SendMessage(TestMessageLevel.Error, $"stderr: {message}");
    });

    private static DataReceivedEventHandler TestEventProcessor(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> tests) => new((sender, args) =>
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
                    {
                        //frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run Test Suite {e.SuiteName}");
                    }
                    break;
                case TestEvent.TYPE.TESTCASE_BEFORE:
                    {
                        var testCase = tests.First(t => t.DisplayName == e.TestName);
                        frameworkHandle.RecordStart(testCase);
                    }
                    break;
                case TestEvent.TYPE.TESTCASE_AFTER:
                    {
                        var testCase = tests.First(t => t.DisplayName == e.TestName);
                        var testResult = new TestResult(testCase)
                        {
                            DisplayName = testCase.DisplayName,
                            Outcome = e.AsTestOutcome(),
                            EndTime = DateTimeOffset.Now,
                            Duration = e.ElapsedInMs
                        };
                        foreach (var report in e.Reports)
                        {
                            testResult.Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, report.Message.RichTextNormalize().Indentation(2)));
                        }
                        frameworkHandle.RecordResult(testResult);
                        frameworkHandle.RecordEnd(testCase, testResult.Outcome);
                    }
                    break;
                case TestEvent.TYPE.TESTSUITE_AFTER:
                    {
                        //frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run Test Suite {e.SuiteName} {e.AsTestOutcome()}");
                    }
                    break;
            }
            return;
        }
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"stdout: {json}");
    });

    private void CheckIfDebug()
    {
        if (!Debugger.IsAttached)
            Debugger.Launch();
    }
}