using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace LogicalNodes.Signals;

[Tool]
[GlobalClass]
[Icon("res://addons/signal_graphs/icons/signal_port_in.png")]
public partial class SignalPortInbound : SignalPort
{
    [Signal]
    public delegate void ReceivedEventHandler();

    private Array _receivingArgs;

    [Export] public Array<ObjectCallable> Callables = new();

    public void Receive(Array args)
    {
        _receivingArgs = args;
        try
        {
            EmitSignalReceived();
            InvokeCallables();
        }
        finally
        {
            _receivingArgs = null;
        }
    }

    public Variant GetArgument(int index)
    {
        if (_receivingArgs == null || index < 0 || index >= _receivingArgs.Count) return default;
        return _receivingArgs[index];
    }

    private void InvokeCallables()
    {
        foreach (var callable in Callables)
        {
            if (callable == null) continue;
            try
            {
                callable.Call(this);
            }
            catch (Exception ex)
            {
                GD.PushError(ex.Message);
            }
        }
    }
}