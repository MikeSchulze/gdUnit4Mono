namespace GdUnit4.Tests.Asserts;

using static Assertions;
using Executions;
using Exceptions;

[TestSuite]
public class BoolAssertTest
{
    [TestCase]
    public void IsTrue()
    {
        AssertBool(true).IsTrue();
        AssertThrown(() => AssertBool(false).IsTrue())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(14)
            .HasMessage("Expecting: 'True' but is 'False'");
    }

    [TestCase]
    public void IsFalse()
    {
        AssertBool(false).IsFalse();
        AssertThrown(() => AssertBool(true).IsFalse())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(24)
            .HasMessage("Expecting: 'False' but is 'True'");
    }

    [TestCase]
    public void IsNull()
    {
        AssertThrown(() => AssertBool(true).IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(33)
            .StartsWithMessage("""
                Expecting be <Null>:
                 but is
                    'True'
                """);
        AssertThrown(() => AssertBool(false).IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(41)
            .StartsWithMessage("""
                Expecting be <Null>:
                 but is
                    'False'
                """);
    }

    [TestCase]
    public void IsNotNull()
    {
        AssertBool(true).IsNotNull();
        AssertBool(false).IsNotNull();
    }

    [TestCase]
    public void IsEqual()
    {
        AssertBool(true).IsEqual(true);
        AssertBool(false).IsEqual(false);
        AssertThrown(() => AssertBool(true).IsEqual(false))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(63)
            .HasMessage("""
                Expecting be equal:
                    'False' but is 'True'
                """);
    }

    [TestCase]
    public void IsNotEqual()
    {
        AssertBool(true).IsNotEqual(false);
        AssertBool(false).IsNotEqual(true);
        AssertThrown(() => AssertBool(true).IsNotEqual(true))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(77)
            .HasMessage("""
                Expecting be NOT equal:
                    'True' but is 'True'
                """);
    }

    [TestCase]
    public void Fluent()
        => AssertBool(true).IsTrue()
            .IsEqual(true)
            .IsNotEqual(false)
            .IsNotNull();

    [TestCase]
    public void OverrideFailureMessage()
        => AssertThrown(() => AssertBool(true).OverrideFailureMessage("Custom failure message").IsFalse())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(95)
            .HasMessage("Custom failure message");

    [TestCase]
    public void InterruptIsFailure()
    {
        // we disable failure reporting until we simulate an failure
        if (ExecutionContext.Current != null)
            ExecutionContext.Current.FailureReporting = false;
        // try to fail
        AssertBool(true).IsFalse();

        // expect this line will never called because of the test is interrupted by a failing assert
        AssertBool(true).OverrideFailureMessage("This line should never be called").IsFalse();
    }
}
