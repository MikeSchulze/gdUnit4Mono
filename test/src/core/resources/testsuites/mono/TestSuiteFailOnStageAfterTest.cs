namespace GdUnit4.Tests.Resources;

using static Assertions;

// will be ignored because of missing `[TestSuite]` annotation
// used by executor integration test
public class TestSuiteFailOnStageAfterTest
{

    [Before]
    public void Before()
        => AssertString("Suite Before()").IsEqual("Suite Before()");

    [After]
    public void After()
        => AssertString("Suite After()").IsEqual("Suite After()");

    [BeforeTest]
    public void BeforeTest()
        => AssertString("Suite BeforeTest()").IsEqual("Suite BeforeTest()");

    [AfterTest]
    public void AfterTest()
        => AssertString("Suite AfterTest()").OverrideFailureMessage("failed on AfterTest()").IsEmpty();

    [TestCase]
    public void TestCase1()
        => AssertString("TestCase1").IsEqual("TestCase1");

    [TestCase]
    public void TestCase2()
        => AssertString("TestCase2").IsEqual("TestCase2");
}
