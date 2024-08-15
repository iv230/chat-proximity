using System.Collections.Generic;
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
        if (message.Payloads[0] is not UIForegroundPayload)
        {
            message.Payloads.Insert(0, new UIForegroundPayload(colorKey));
        }

        // Extracting existing text chunks and their colors
        List<TextPayload> textPayloads = [];
        List<UIForegroundPayload> uiForegroundPayloads = [];
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];
            if (payload is TextPayload textPayload)
            {
                var uiForegroundPayload = (UIForegroundPayload) message.Payloads[i-1];

                // If disabled, must be colored
                if (!uiForegroundPayload.IsEnabled)
                {
                    uiForegroundPayload.ColorKey = colorKey;
                }
                
                textPayloads.Add(textPayload);
                uiForegroundPayloads.Add(uiForegroundPayload);

                ChatProximityPlugin.PluginLog.Verbose($"Chunk \"{textPayload.Text}\" got color {uiForegroundPayload.ColorKey}");
            }
        }

        // Building final payload; color, text, close color
        List<Payload> finalPayload = [];
        for (var i = 0; i < textPayloads.Count; i++)
        {
            finalPayload.Add(uiForegroundPayloads[i]);
            finalPayload.Add(textPayloads[i]);
            finalPayload.Add(new UIForegroundPayload(0));
        }
        
        finalPayload.Add(UIForegroundPayload.UIForegroundOff);
        
        message = new SeString(finalPayload);
    }
}
