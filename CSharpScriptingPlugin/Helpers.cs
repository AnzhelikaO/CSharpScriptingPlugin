namespace CSharpScripting;

internal static class Helpers
{
    #region ToStringNull

    public static string ToStringNull(object? Object) => (Object?.ToString() ?? "<NULL>");

    #endregion
    #region ValidateConstant

    public static T ValidateConstant<T>(T Constant, T? Previous,
                                        [CallerArgumentExpression(nameof(Constant))]string? Name = null)
    {
        ArgumentNullException.ThrowIfNull(Constant, Name);
        if ((Constant is string constant) && (string.IsNullOrWhiteSpace(constant)))
            throw new ArgumentException($"Invalid {Name} \"{Constant}\".");
        else if ((Previous is not null) && !Previous.Equals(Constant))
            throw new ArgumentException($"{Name} is inconsistent: " +
                                        $"before - \"{Previous}\", now - \"{Constant}\".");
        return Constant;
    }

    #endregion
}