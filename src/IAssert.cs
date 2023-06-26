namespace GdUnit4.Asserts
{
    /// <summary> Main interface of all GdUnit asserts </summary>
    public interface IAssert
    {
        /// <summary> Overrides the default failure message by given custom message.</summary>
        IAssert OverrideFailureMessage(string message);
    }

    /// <summary> Base interface of all GdUnit asserts </summary>
    public interface IAssertBase<V> : IAssert
    {
        /// <summary>Verifies that the current value is null.</summary>
        IAssertBase<V> IsNull();

        /// <summary> Verifies that the current value is not null.</summary>
        IAssertBase<V> IsNotNull();

        /// <summary> Verifies that the current value is equal to expected one.
        IAssertBase<V> IsEqual(V expected);

        /// <summary> Verifies that the current value is not equal to expected one.</summary>
        IAssertBase<V> IsNotEqual(V expected);
    }
}
