namespace GdUnit4.Asserts;

public interface IValueExtractor
{
    /// <summary> Extracts a value by given implementation</summary>
    public object? ExtractValue(object? value);
}
