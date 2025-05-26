﻿using System;
using CloneSwap.Systems.Multiplayer;
using Godot;

namespace LogicalNodes;

public enum AuthorityMode
{
    Authority,
    AnyPeer,
    NonAuthority
}

public static class AuthorityModeExt
{
    public static bool CanExecute(this AuthorityMode mode, Node node)
    {
        return mode switch
        {
            AuthorityMode.Authority => node.IsMultiplayerAuthority(),
            AuthorityMode.AnyPeer => true,
            AuthorityMode.NonAuthority => !node.IsMultiplayerAuthority(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}