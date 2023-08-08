#region Using

using System.Collections;
using System.Runtime.CompilerServices;

#endregion
namespace CSharpScripting;

internal static class Helpers
{
    #region ToStringNull

    public static string ToStringNull(object? Object) => (Object?.ToString() ?? "<NULL>");

    #endregion
    #region CreateValueTuple

    public static dynamic CreateValueTuple(params dynamic[] Elements)
    {
        const int MAX_TUPLE_ELEMENTS = 8;
        List<List<dynamic>> elements = new() { new(MAX_TUPLE_ELEMENTS) };
        for (int i = 0, a = 1; i < Elements.Length; i++, a++)
        {
            if (a >= MAX_TUPLE_ELEMENTS)
            {
                elements.Add(new(MAX_TUPLE_ELEMENTS));
                a = 1;
            }
            elements[^1].Add(Elements[i]);
        }
        for (int i = (elements.Count - 1); i >= 1; i--)
            elements[i - 1].Add(CreateValueTuple(elements[i]));
        return CreateValueTuple(elements[0]);
    }
    #region CreateValueTuple

    private static dynamic CreateValueTuple(List<dynamic> Elements) =>
        ValueTupleBaseTypes[Elements.Count]
            .MakeGenericType(Elements.Select(e => (Type)e.GetType()).ToArray())
            .GetConstructors()[0]
            .Invoke(Elements.ToArray());

    #endregion
    #region ValueTupleBaseTypes

    private static readonly Dictionary<int, Type> ValueTupleBaseTypes =
        new()
        {
            [1] = typeof(ValueTuple<>),
            [2] = typeof(ValueTuple<,>),
            [3] = typeof(ValueTuple<,,>),
            [4] = typeof(ValueTuple<,,,>),
            [5] = typeof(ValueTuple<,,,,>),
            [6] = typeof(ValueTuple<,,,,,>),
            [7] = typeof(ValueTuple<,,,,,,>),
            [8] = typeof(ValueTuple<,,,,,,,>)
        };

    #endregion

    #endregion
    #region ExtractValues

    public static bool ExtractValues(dynamic Value, [MaybeNullWhen(false)]out IEnumerable<dynamic?> Values)
    {
        Values = null;
        switch (Value)
        {
            case ITuple tuple:
                List<dynamic?> val = new(tuple.Length);
                for (int i = 0; i < tuple.Length; i++)
                    val.Add(tuple[i]);
                Values = val;
                break;
            case IEnumerable<dynamic> values:
                Values = values;
                break;
            case IEnumerable values:
                Values = values.Cast<dynamic>();
                break;
        }
        return (Values is not null);
    }

    #endregion
    #region ValidateConstant

    public static void ValidateConstant([NotNull]ref string? Was, string Current,
                                        [CallerArgumentExpression("Current")]string? Name = null)
    {
        ArgumentNullException.ThrowIfNull(Current, Name);
        if (string.IsNullOrWhiteSpace(Current))
            throw new ArgumentException($"Invalid {Name} \"{Current}\".");
        if (Was is null)
            Was = Current;
        else if (Current != Was)
            throw new ArgumentException($"{Name} is inconsistent: before - \"{Was}\", now - \"{Current}\".");
    }

    #endregion
}