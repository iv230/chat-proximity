using System.Numerics;
using Dalamud.Game.Text;

namespace ChatProximity.Config;

public class ChatTypeConfig
{
    public ChatTypeConfig(XivChatType type, bool enabled, float range, Vector4 nearColor = new(), Vector4 farColor = new(), Vector4 targetingColor = new(), Vector4 targetedColor = new(), float threshold = 0f)
    {
        Type = type;
        Enabled = enabled;
        Range = range;
        NearColor = nearColor;
        FarColor = farColor;
        TargetingColor = targetingColor;
        TargetedColor = targetedColor;
        Threshold = threshold > 0 ? threshold : range;
    }

    public XivChatType Type { get; set; }
    public bool Enabled { get; set; }
    public float Range { get; set; }
    public Vector4 NearColor { get; set; }
    public Vector4 FarColor { get; set; }
    public Vector4 TargetingColor { get; set; }
    public Vector4 TargetedColor { get; set; }
    public float Threshold { get; set; }
}
