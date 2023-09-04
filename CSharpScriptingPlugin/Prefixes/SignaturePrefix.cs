namespace CSharpScripting.Configuration.Prefixes;

public abstract class SignaturePrefix : CodePrefix
{
    private protected const string ADDITIONAL_PREFIX = "=";
    private protected SignaturePrefix() { }

    private protected static void Show(Globals Globals, Type Type)
    {
        List<string> lines = new();
        MemberInfo[] members = Type.GetMembers();
        AddMembers("Types", members.OfType<TypeInfo>()
                                   .Where(i => !i.IsSpecialName)
                                   .Select(i => i.Name)
                                   .ToArray());
        AddMembers("Fields", members.OfType<FieldInfo>()
                                    .Where(i => !i.IsSpecialName)
                                    .Select(ToStringField)
                                    .ToArray());
        AddMembers("Properties", members.OfType<PropertyInfo>()
                                        .Where(i => !i.IsSpecialName)
                                        .Select(ToStringProp)
                                        .ToArray());
        AddMembers("Constructors", members.OfType<ConstructorInfo>()
                                          .Select(ToStringMethod)
                                          .ToArray());
        AddMembers("Methods", members.OfType<MethodInfo>()
                                     .Where(i => !i.IsSpecialName)
                                     .Select(ToStringMethod)
                                     .ToArray());
        AddMembers("Other", members.Where(i => (i is not TypeInfo or FieldInfo or PropertyInfo
                                                     or ConstructorInfo or MethodInfo))
                                   .Select(ToStringOther)
                                   .ToArray());
        if (lines.Any())
            Globals.cw($"{Type} {{\n{string.Join('\n', lines)}\n}}");

        #region ToString[Field/Prop/Method/Other]

        static string ToStringField(FieldInfo Field) => $"[{Field.FieldType} {Field.Name}]";
        static string ToStringProp(PropertyInfo Prop) => $"[{Prop.PropertyType} {Prop.Name}]";
        static string ToStringMethod(MethodBase Method)
        {
            string ret = ((Method is MethodInfo method)
                              ? $"{method.ReturnType} "
                              : string.Empty);
            return $"[{ret}{Method.Name}({ToStringParams()})]";

            string ToStringParam(ParameterInfo Param) => $"{Param.ParameterType} {Param.Name}";
            string ToStringParams() => string.Join(", ", Method.GetParameters().Select(ToStringParam));
        }
        static string ToStringOther(MemberInfo Other) => $"[{Other.MemberType} {Other.Name}]";

        #endregion
        #region AddMembers

        void AddMembers(string Type, string[] Members)
        {
            if (!Members.Any())
                return;

            List<string> l = PaginationTools.BuildLinesFromTerms(Members);
            if (l.Count > 1)
            {
                lines.Add($"  {Type}:");
                lines.AddRange(l.Select(ll => $"    {ll}"));
            }
            else
                lines.Add($"  {Type}: {l[0]}");
        }

        #endregion
    }
}