using System.Numerics;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

public class ChatTypeConfig(XivChatType type, bool enabled, float range, Vector4 nearColor = new(), Vector4 farColor = new())
{
    public XivChatType Type { get; set; } = type;
    public bool Enabled { get; set; } = enabled;
    public float Range { get; set; } = range;
    public Vector4 NearColor { get; set; } = nearColor;
    public Vector4 FarColor { get; set; } = farColor;
}
