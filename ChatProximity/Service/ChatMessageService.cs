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
        // We create a variable `sb`, a string builder used to rebuild a properly colored message that will replace the
        // original one
        var sb = new SeStringBuilder();

        // We iterate through all message payloads to process them individually without interfering with other plugins
        // actions
        for (var i = 0; i < message.Payloads.Count; i++)
        {
            var payload = message.Payloads[i];

            if (payload is TextPayload textPayload)
            {
                ChatProximity.Log.Verbose($"i = {i}");
                var previousPayLoad = i > 0 ? message.Payloads[i - 1] : null;

                // Here I we whether to modify the payload color or not:
                // darken "white" text while preserving colors already modified by other plugins
                if (previousPayLoad is UIForegroundPayload { IsEnabled: true } previousPayload)
                    sb.PushColorType(previousPayload.ColorKey);
                else
                    sb.PushColorRgba((byte)(color.X*255), (byte)(color.Y*255), (byte)(color.Z*255), (byte)(color.W*255));

                sb.Append(textPayload.Text);
                sb.PopColor();
            }
            // Otherwise, keep it as-is in the rebuilt message
            else if (payload is not UIForegroundPayload)
            {
                sb.Append(new ReadOnlySeStringSpan(payload.Encode()));
            }
        }

        // Replace the original message reference with the newly created one
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
