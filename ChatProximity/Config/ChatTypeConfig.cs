﻿using System.Numerics;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

public class ChatTypeConfig(XivChatType type, bool enabled, float range, Vector4 closestColor = new(), Vector4 farthestColor = new())
{
    public XivChatType Type { get; set; } = type;
    public bool Enabled { get; set; } = enabled;
    public float Range { get; set; } = range;
    public Vector4 ClosestColor { get; set; } = closestColor;
    public Vector4 FarthestColor { get; set; } = farthestColor;
}
