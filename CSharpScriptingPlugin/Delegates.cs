#region Using

using CSharpScripting.Configuration.Prefixes;

#endregion
namespace CSharpScripting.Configuration.Delegates;

public delegate CodeManager? SetManagerD(CodeManager? Manager);
public delegate CodePrefix? RegisterCodePrefixD(CodePrefix? Manager);
public delegate bool DeregisterCodePrefixD(CodePrefix? Manager);
public delegate dynamic? TransformDictionaryD(Dictionary<dynamic, dynamic> Dictionary);
public delegate dynamic? GetSetValueD(dynamic? Key, dynamic? Value);
public delegate bool ClearD(bool Force);