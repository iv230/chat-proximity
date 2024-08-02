using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ChatProximity.Strategies;

public class DirtyMessageHandlerStrategy : IMessageHandlerStrategy
{
    /// <summary>
    /// Handles a dirty message by modifying its payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="colorKey">The color of the message</param>
    public void HandleMessage(ref SeString message, ushort colorKey)
    {
        ChatProximityPlugin.PluginLog.Debug("Message dirty");

        // Adding a first element to color the first chunk
        if (message.Payloads[0] is UIForegroundPayload { IsEnabled: false })
        {
            message.Payloads.Insert(0, new UIForegroundPayload(colorKey));
        }

        // Then process all other chunks
        for (var i = 1; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];
            if (payload is UIForegroundPayload { IsEnabled: false } currentForegroundPayload)
            {
                currentForegroundPayload.ColorKey = colorKey;
            }
        }

        // Adding a last color to prevent propagation to other lines
        message.Payloads.Insert(message.Payloads.Count, new UIForegroundPayload(0));
    }
}
