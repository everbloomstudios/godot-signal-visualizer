@tool
extends Node

signal connection_output_ready;

# This script is necessary to use necessary Godot API features that are not available in C#, but are in GDScript.

var output_signal;
var output_flags: int;
var output_callable_method_name: StringName;
var output_callable_argument_count: int = 0;
var output_callable_bound_argument_count: int = 0;
var output_callable_bound_arguments;

func parse_incoming_connections(node: Node):
	var incoming_connections: Array[Dictionary] = node.get_incoming_connections();
	for connection : Dictionary in incoming_connections:
		if(connection.flags & CONNECT_PERSIST) == 0:
			continue;
		parse_connection(connection);
		connection_output_ready.emit();

func parse_connection(connection: Dictionary):
	output_signal = connection.signal;
	parse_callable(connection.callable);
	output_flags = connection.flags;

func parse_callable(callable: Callable, get_arguments: bool = false):
	output_callable_method_name = callable.get_method();
	output_callable_argument_count = callable.get_argument_count();
	output_callable_bound_argument_count = callable.get_bound_arguments_count();
	if get_arguments:
		output_callable_bound_arguments = callable.get_bound_arguments();
	else:
		output_callable_bound_arguments = null;
