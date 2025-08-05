using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Text.ReadOnly;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

namespace ChatProximity.Service;

public class ChatMessageService
{
    /// <summary>
    /// Handles a dirty message by modifying its payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="color">The computed color</param>
    public static void HandleMessage(ref SeString message, Vector4 color)
    {
        ChatProximity.Log.Verbose($"Message is {message.ToJson()}");
        var sb = new SeStringBuilder();

        // Extracting and processing payloads
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];

            if (payload is TextPayload textPayload)
            {
                ChatProximity.Log.Verbose($"i = {i}");
                var previousPayLoad = i > 0 ? message.Payloads[i - 1] : null;

                if (previousPayLoad is UIForegroundPayload { IsEnabled: true } previousPayload)
                    sb.PushColorType(previousPayload.ColorKey);
                else
                    sb.PushColorRgba((byte)(color.X*255), (byte)(color.Y*255), (byte)(color.Z*255), (byte)(color.W*255));

                sb.Append(textPayload.Text);
                sb.PopColor();
            }
            else if (payload is not UIForegroundPayload)
            {
                sb.Append(new ReadOnlySeStringSpan(payload.Encode()));
            }
        }

        // Update the message with the new payloads
        message = sb.ToReadOnlySeString().ToDalamudString();

        ChatProximity.Log.Verbose($"New message is {message.ToJson()}");
    }

    /// <summary>
    /// Handles dropping a message that exceeds a threshold.
    /// <param name="message">The message to handle</param>
    /// <param name="isHandled"></param>
    /// </summary>
    public static void DropMessage(ref SeString message, ref bool isHandled)
    {
        isHandled = true;
    }
}
