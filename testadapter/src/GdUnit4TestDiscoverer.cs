using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GdUnit4.TestAdapter.Discovery;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using static GdUnit4.TestAdapter.Discovery.CodeNavigationDataProvider;

namespace GdUnit4.TestAdapter;

[DefaultExecutorUri(GdUnit4TestExecutor.ExecutorUri)]
[ExtensionUri(GdUnit4TestExecutor.ExecutorUri)]
[FileExtension(".dll")]
[FileExtension(".exe")]
public sealed class GdUnit4TestDiscoverer : ITestDiscoverer
{

    internal static readonly TestProperty TestClassNameProperty = TestProperty.Register(
            "GdUnit4.Test",
            "SuiteName",
            typeof(string),
            TestPropertyAttributes.Hidden,
            typeof(TestCase));

    public void DiscoverTests(
        IEnumerable<string> assemblyPaths,
        IDiscoveryContext discoveryContext,
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
        //logger.SendMessage(TestMessageLevel.Informational, $"RunSettings: {discoveryContext.RunSettings}:{discoveryContext.RunSettings?.SettingsXml}");
        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(discoveryContext.RunSettings?.SettingsXml);
        //logger.SendMessage(TestMessageLevel.Informational, $"RunConfiguration: {runConfiguration.TestSessionTimeout}");

        var filteredAssemblys = FilterWithoutTestAdapter(assemblyPaths);
        foreach (string assemblyPath in filteredAssemblys)
        {
            logger.SendMessage(TestMessageLevel.Informational, $"Discover tests for assembly: {assemblyPath}");

            using var codeNavigationProvider = new CodeNavigationDataProvider(assemblyPath, logger);
            if (codeNavigationProvider.GetAssembly() == null) continue;
            Assembly assembly = codeNavigationProvider.GetAssembly()!;

            // discover GdUnit4 testsuites
            foreach (var type in assembly.GetTypes().Where(IsTestSuite))
            {
                // discover test cases
                var className = type.FullName!;
                type.GetMethods()
                    .Where(m => m.IsDefined(typeof(TestCaseAttribute)))
                    .AsParallel()
                    .ForAll(mi =>
                    {
                        var navData = codeNavigationProvider.GetNavigationData(className, mi.Name);
                        if (!navData.IsValid)
                            logger.SendMessage(TestMessageLevel.Informational, $"Can't collect code navigation data for {className}:{mi.Name}    GetNavigationData -> {navData.Source}:{navData.Line}");

                        // Collect parameterized tests or build a single test
                        mi.GetCustomAttributes(typeof(TestCaseAttribute))
                            .Cast<TestCaseAttribute>()
                            .Where(attr => attr != null && (attr.Arguments?.Any() ?? false))
                            .Select(attr =>
                            {
                                var paramaterizedTestName = $"{attr.TestName ?? mi.Name}({attr.Arguments.Formated()})";
                                return new
                                {
                                    TestName = paramaterizedTestName,
                                    FullyQualifiedName = $"{mi.DeclaringType}.{mi.Name}.{paramaterizedTestName}"
                                };
                            })
                            .DefaultIfEmpty(new
                            {
                                TestName = $"{mi.Name}",
                                FullyQualifiedName = $"{mi.DeclaringType}.{mi.Name}"
                            })
                            .Select(test => BuildTestCase(test.FullyQualifiedName, test.TestName, assemblyPath, navData))
                            .ToList()
                            .ForEach(discoverySink.SendTestCase);
                    });
            };
        }
    }

    private TestCase BuildTestCase(string fullyQualifiedName, string testName, string assemblyPath, CodeNavigation navData)
    {
        TestCase testCase = new(fullyQualifiedName, new Uri(GdUnit4TestExecutor.ExecutorUri), assemblyPath)
        {
            DisplayName = testName,
            FullyQualifiedName = fullyQualifiedName,
            CodeFilePath = navData.Source,
            LineNumber = navData.Line
        };
        testCase.SetPropertyValue(TestClassNameProperty, testCase.DisplayName);
        return testCase;
    }

    private static IEnumerable<string> FilterWithoutTestAdapter(IEnumerable<string> assemblyPaths) =>
        assemblyPaths.Where(assembly => !assembly.Contains(".TestAdapter."));

    private static bool IsTestSuite(Type type) =>
        type.IsClass && !type.IsAbstract && Attribute.IsDefined(type, typeof(TestSuiteAttribute));

}
