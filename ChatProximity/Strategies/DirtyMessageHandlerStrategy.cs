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

        // Ensure the first element is a color payload
        if (message.Payloads[0] is not UIForegroundPayload)
        {
            message.Payloads.Insert(0, new UIForegroundPayload(colorKey));
            sb.PushColorType(colorKey);
        }

        // Extracting and processing text and color payloads
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            if (message.Payloads[i] is TextPayload textPayload)
            {
                var effectiveColorKey = message.Payloads[i - 1] is UIForegroundPayload { IsEnabled: false } previousPayload
                                            ? previousPayload?.ColorKey ?? colorKey
                                            : colorKey;
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
