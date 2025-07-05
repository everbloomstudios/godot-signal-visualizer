using Godot;
using Godot.Collections;

namespace LogicalNodes.Signals;

[Tool]
[GlobalClass]
[Icon("res://addons/signal_graphs/icons/signal_station.png")]
public partial class SignalStation : Node
{
    private Array<SignalStation> _connectedStations = new();
    private Array<SignalPort> _ports = new();
    
    public void HandleEmit(SignalPortOutbound port, Array args)
    {
        foreach (var station in _connectedStations)
        {
            station.HandleReceive(port, args);
        }
    }

    private void HandleReceive(SignalPortOutbound sourcePort, Array args)
    {
        foreach (var ownPort in _ports)
        {
            if (ownPort is SignalPortInbound inbound && ownPort.Name == sourcePort.Name)
            {
                inbound.HandleReceive(sourcePort, args);
            }
        }
    }

    public void ConnectStation(SignalStation station)
    {
        if (station == null) return;
        if (!IsInstanceValid(station)) return;
        if (_connectedStations.Contains(station)) return;
        _connectedStations.Add(station);
    }

    public void ConnectStationTwoWay(SignalStation station)
    {
        if (station == null) return;
        ConnectStation(station);
        station.ConnectStation(this);
    }

    public void DisconnectStation(SignalStation station)
    {
        if (station == null) return;
        _connectedStations.Remove(station);
    }

    public void DisconnectStationTwoWay(SignalStation station)
    {
        if (station == null) return;
        DisconnectStation(station);
        station.DisconnectStation(this);
    }
    

    public override void _EnterTree()
    {
        this.Connect(Node.SignalName.ChildEnteredTree, new Callable(this, MethodName.OnChildEnteredTree));
        this.Connect(Node.SignalName.ChildExitingTree, new Callable(this, MethodName.OnChildExitingTree));
    }

    public override void _ExitTree()
    {
        this.Disconnect(Node.SignalName.ChildEnteredTree, new Callable(this, MethodName.OnChildEnteredTree));
        this.Disconnect(Node.SignalName.ChildExitingTree, new Callable(this, MethodName.OnChildExitingTree));
    }

    private void OnChildEnteredTree(Node node)
    {
        if (node is SignalPort port) _ports.Add(port);
    }

    private void OnChildExitingTree(Node node)
    {
        if (node is SignalPort port) _ports.Remove(port);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            foreach (var station in _connectedStations)
            {
                if(station != this)
                    station.DisconnectStation(this);
            }
        }
    }
}