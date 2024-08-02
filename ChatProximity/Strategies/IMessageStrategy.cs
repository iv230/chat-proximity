using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ChatProximity.Strategies;

public interface IMessageHandlerStrategy
{
    void HandleMessage(ref SeString message, ushort colorKey);
}

