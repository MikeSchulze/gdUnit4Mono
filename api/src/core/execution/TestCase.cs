namespace GdUnit4.Executions;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

internal sealed class TestCase
{
    public TestCase(MethodInfo methodInfo, int lineNumber)
    {
        MethodInfo = methodInfo;
        Line = lineNumber;
        Parameters = InitialParameters();
    }

    public string Name => MethodInfo.Name;

    public int Line
    { get; private set; }

    public IEnumerable<TestCaseAttribute> TestCaseAttributes
        => MethodInfo.GetCustomAttributes<TestCaseAttribute>().Where(TestParametersFilter);

    public Func<TestCaseAttribute, bool> TestParametersFilter { get; set; } = _ => true;

    public TestCaseAttribute TestCaseAttribute => MethodInfo.GetCustomAttribute<TestCaseAttribute>()!;

    public bool IsSkipped => Attribute.IsDefined(MethodInfo, typeof(IgnoreUntilAttribute));

    private IEnumerable<object> Parameters
    { get; set; }

    public MethodInfo MethodInfo
    { get; set; }

    private IEnumerable<object> ResolveParam(object input)
    {
        if (input is IValueProvider provider)
        {
            return provider.GetValues();
        }
        return new object[] { input };
    }

    private List<object> InitialParameters()
        => MethodInfo.GetParameters()
            .SelectMany(pi => pi.GetCustomAttributesData()
                .Where(attr => attr.AttributeType == typeof(FuzzerAttribute))
                .Select(attr =>
                {
                    var arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray();
                    return attr.Constructor.Invoke(arguments);
                }
            )
         ).ToList();

    public object[] Arguments => Parameters.SelectMany(ResolveParam).ToArray();

    public static string BuildTestCaseName(string testName, TestCaseAttribute attribute)
        => BuildTestCaseName(testName, attribute.TestName ?? testName, attribute.Arguments);

    public static string BuildTestCaseName(string testName, string parameterizedName, params object?[] arguments)
    {
        if (arguments.Length > 0)
        {
            var parameters = string.Join(", ", arguments.ToArray().Select(GdUnitExtensions.Formatted));
            testName = $"{testName}.{parameterizedName}({parameters})";
        }
        return testName;
    }
}
