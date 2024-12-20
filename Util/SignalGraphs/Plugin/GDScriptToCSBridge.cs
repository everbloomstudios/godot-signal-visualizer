using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace Util.SignalGraphs.Plugin;

[Tool]
public partial class GDScriptToCSBridge : Node
{
    private static readonly HashSet<StringName> __tempConnectedMethodNames = new();
    private static readonly List<ParsedConnection> __tempParsedConnections = new();
    
    [Export] public Node GdsExclusives;
    
    private List<ParsedConnection> _outputConnections;

    public override void _Ready()
    {
        GdsExclusives.Connect(GDSSignalName.ConnectionOutputReady,
            new Callable(this, MethodName.OnConnectionOutputReady));
    }

    /// <summary>
    /// Retrieves a HashSet of all the method names for the given node, that signals are connected to.
    /// Node: The returned HashSet instance is always the same; If you need to store the result of this,
    /// or call it recursively, create a copy of the return value before calling this method again.
    /// </summary>
    public HashSet<StringName> GetConnectedMethodNames(Node node)
    {
        __tempParsedConnections.Clear();
        DumpIncomingConnections(node, __tempParsedConnections);
        
        __tempConnectedMethodNames.Clear();
        foreach (var connection in __tempParsedConnections)
        {
            __tempConnectedMethodNames.Add(connection.Callable.Method);
        }

        return __tempConnectedMethodNames;
    }
    
    /// <summary>
    /// Given a Callable, returns a struct containing data about it,
    /// including data that is accessible in GDScript but not C#.
    /// </summary>
    public void DumpIncomingConnections(Node node, List<ParsedConnection> output)
    {
        _outputConnections = output;
        GdsExclusives.Call(GDSMethodName.ParseIncomingConnections, node);
        _outputConnections = null;
    }

    private void OnConnectionOutputReady()
    {
        if (_outputConnections == null) return;
        _outputConnections.Add(new ParsedConnection()
        {
            Signal = GdsExclusives.Get(GDSPropertyName.OutputSignal).AsSignal(),
            Flags = GdsExclusives.Get(GDSPropertyName.OutputFlags).As<ConnectFlags>(),
            Callable = new ParsedCallable()
                {
                    Method = GdsExclusives.Get(GDSPropertyName.OutputCallableMethodName).AsStringName(),
                    ArgumentCount = GdsExclusives.Get(GDSPropertyName.OutputCallableArgumentCount).AsInt32(),
                    BoundArgumentCount = GdsExclusives.Get(GDSPropertyName.OutputCallableBoundArgumentCount).AsInt32(),
                    // BoundArguments = GdsExclusives.Get(GDSPropertyName.OutputCallableBoundArguments).AsGodotArray()
                }
        });
    }
    
    private static class GDSMethodName
    {
        public static readonly StringName ParseIncomingConnections = "parse_incoming_connections";
        public static readonly StringName ParseConnection = "parse_connection";
        public static readonly StringName ParseCallable = "parse_callable";
    }
    
    private static class GDSPropertyName
    {
        public static readonly StringName OutputSignal = "output_signal";
        public static readonly StringName OutputFlags = "output_flags";
        public static readonly StringName OutputCallableMethodName = "output_callable_method_name";
        public static readonly StringName OutputCallableArgumentCount = "output_callable_argument_count";
        public static readonly StringName OutputCallableBoundArgumentCount = "output_callable_bound_argument_count";
        public static readonly StringName OutputCallableBoundArguments = "output_callable_bound_arguments";
    }
    
    private static class GDSSignalName
    {
        public static readonly StringName ConnectionOutputReady = "connection_output_ready";
    }
}

public struct ParsedConnection
{
    public Signal Signal;
    public ParsedCallable Callable;
    public GodotObject.ConnectFlags Flags;
}

public struct ParsedCallable
{
    public StringName Method;
    public int ArgumentCount;
    public int BoundArgumentCount;
    public Array BoundArguments;
}