# Permission
`csharp.scripting.execute`

# Prefixes to run C# script in console or in game chat
`;` - execute code: <br/>
&emsp;&emsp;`;Console.WriteLine(1)` <br/>
`;;` - show object: <br/>
&emsp;&emsp;`;;TSPlayer.Server.Name` <br/>
`;=` - print public members of an **object**: <br/>
&emsp;&emsp;`;=Main.tile[0, 0]` <br/>
`;==` - print public members of a **type**: <br/>
&emsp;&emsp;`;==Chest` <br/>

# Global members always available in your context
`cw(obj, [receivers])` method: <br/>
&emsp;&emsp;`;cw("test")` <br/>
&emsp;&emsp;`;cw("Hello!", admins)` <br/>
`me` - your TSPlayer <br/>
`server` - TSPlayer.Server shortcut <br/>
`admins` - server and every player that have [permission](#permission) <br/>
`kv` - Dictionary<dynamic, dynamic>, null key allowed <br/>

# Special features
`;using Microsoft.Xna.Framework` updates your context with said using (untill you leave and rejoin) <br/>
Every `$var` gets replaced to `kv["var"]`: <br/>
&emsp;&emsp;`;$i=5` equals to `;kv["i"]=5` <br/>
&emsp;&emsp;`;;$i` equals to `;;kv["i"]` or `;cw(kv["i"])` <br/>
&emsp;&emsp;`;$a=$b+$c` equals to `;kv["a"]=kv["b"]+kv["c"]`
