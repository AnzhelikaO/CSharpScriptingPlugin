namespace CSharpScripting.Configuration.Delegates;

public delegate CodeManager PreSetCodeManagerD(CodeManager Manager);
public delegate void PostSetCodeManagerD(CodeManager Manager, bool IsInitialized);

public delegate bool PreToggleD(CodeManager Manager, bool Enable, bool FromInitializeOrDispose);
public delegate void PostToggleD(CodeManager Manager, bool Success,
                                 bool Enable, bool FromInitializeOrDispose);



public delegate CodePrefix? PreRegisterCodePrefixD(CodePrefix? Prefix);
public delegate void PostRegisterCodePrefixD(bool Success, CodePrefix Prefix);

public delegate bool PreDeregisterCodePrefixD(CodePrefix? Prefix);
public delegate void PostDeregisterCodePrefixD(bool Success, CodePrefix Prefix);



public delegate dynamic? TransformDictionaryD(Dictionary<dynamic, dynamic?> Dictionary);

public delegate void GetValuesD(TransformDictionaryD? Transform, dynamic?[]? Keys,
                                Dictionary<dynamic, dynamic?> NoTransformValues,
                                ref dynamic? TransformedValues);
public delegate bool SetValuesD(dynamic?[]? Keys, dynamic?[] Values);

public delegate bool PreClearDictionaryD(bool Force);
public delegate void PostClearDictionaryD(bool Success, bool Force);