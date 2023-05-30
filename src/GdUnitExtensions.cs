using System.Collections;
using System.Collections.Generic;

namespace GdUnit4
{
    /// <summary>
    /// A extension to convert c# types to Godot types
    /// </summary>
    public static class GdUnitExtensions
    {
        public static Godot.Collections.Array<T> ToGodotArray<T>(this IEnumerable<T> elements) => new Godot.Collections.Array<T>(elements);

        public static Godot.Collections.Array<T> ToGodotArray<T>(this T[] args) => ToGodotArray((IEnumerable<T>)args);

        public static Godot.Collections.Array ToGodotArray(this object[] args) => ToGodotArray((IEnumerable)args);

        public static Godot.Collections.Array ToGodotArray(this IEnumerable elements)
        {
            var converted = new Godot.Collections.Array();
            foreach (var item in elements)
            {
                converted.Add((Godot.Variant)item);
            }
            return converted;
        }

        public static Godot.Collections.Dictionary ToGodotDictionary(this Dictionary<string, object> dict)
        {
            var converted = new Godot.Collections.Dictionary();
            foreach (var item in dict)
            {
                converted.Add(item.Key, (Godot.Variant)item.Value);
            }
            return converted;
        }

        public static string Formated(this IEnumerable args) => ToGodotArray(args).ToString();

        public static string Formated(this object[] args) => $"{string.Join(", ", args)}";
    }
}
