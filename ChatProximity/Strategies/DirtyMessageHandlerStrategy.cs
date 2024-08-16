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

        // Ensure the first element is a color payload
        if (message.Payloads[0] is not UIForegroundPayload)
        {
            message.Payloads.Insert(0, new UIForegroundPayload(colorKey));
        }

        // Extracting and processing text and color payloads
        var finalPayloads = new List<Payload>();

        for (var i = 0; i < message.Payloads.Count; i++)
        {
            if (message.Payloads[i] is TextPayload textPayload)
            {
                var previousPayload = message.Payloads[i - 1] as UIForegroundPayload;

                if (previousPayload is { IsEnabled: false })
                {
                    previousPayload.ColorKey = colorKey;
                }

                finalPayloads.Add(previousPayload ?? new UIForegroundPayload(colorKey));
                finalPayloads.Add(textPayload);
                finalPayloads.Add(new UIForegroundPayload(0)); // Close the color tag

                ChatProximityPlugin.PluginLog.Verbose($"Chunk \"{textPayload.Text}\" got color {colorKey}");
            }
        }

        // Add the color off payload at the end
        finalPayloads.Add(UIForegroundPayload.UIForegroundOff);

        // Update the message with the new payloads
        message = new SeString(finalPayloads);
    }
}
