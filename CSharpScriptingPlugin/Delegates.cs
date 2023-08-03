namespace CSharpScriptingPlugin.Configuration;

public delegate CodeManager? SetManagerD(CodeManager? Manager);
public delegate dynamic? GetSetValueD(dynamic? Key, dynamic? Value);
public delegate bool ClearD(bool Force);