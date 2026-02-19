using System;
using System.IO;
using System.Linq;
using System.Reflection;

var bin = @"C:\Quantower\TradingPlatform\v1.145.16\bin";
AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
{
    var n = new AssemblyName(e.Name).Name + ".dll";
    var p = Path.Combine(bin, n);
    return File.Exists(p) ? Assembly.LoadFrom(p) : null;
};
var asm = Assembly.LoadFrom(Path.Combine(bin, "TradingPlatform.BusinessLayer.dll"));

void Dump(string typeName)
{
    var t = asm.GetType(typeName);
    Console.WriteLine("==== " + typeName);
    if (t == null) { Console.WriteLine("not found"); return; }
    foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
        Console.WriteLine($"PROP {p.PropertyType.FullName} {p.Name}");
    foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
        Console.WriteLine($"METH {m}");
}

Dump("TradingPlatform.BusinessLayer.DOMQuote");
Dump("TradingPlatform.BusinessLayer.Level2Quote");
Dump("TradingPlatform.BusinessLayer.DepthOfMarket");
