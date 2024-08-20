using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

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
        ChatProximity.Log.Debug("Message dirty");
        var sb = new SeStringBuilder();

        // Extracting and processing text and color payloads
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            if (message.Payloads[i] is TextPayload textPayload)
            {
                ushort effectiveColorKey;

                ChatProximity.Log.Verbose($"i = {i}");
                var previousPayLoad = i > 0 ? message.Payloads[i - 1] : null;

                if (previousPayLoad is UIForegroundPayload { IsEnabled: true } previousPayload)
                    effectiveColorKey = previousPayload?.ColorKey ?? colorKey;
                else
                    effectiveColorKey = colorKey;

                sb.PushColorType(effectiveColorKey);
                sb.Append(textPayload.Text);
                sb.PopColor();

                ChatProximity.Log.Verbose($"Chunk \"{textPayload.Text}\" got color {effectiveColorKey}");
            }
        }

        // Update the message with the new payloads
        message = sb.ToSeString().ToDalamudString();
    }
}
