using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace ChatProximity.Strategies;

public class NotDirtyMessageHandlerStrategy : IMessageHandlerStrategy
{
    /// <summary>
    /// Handles a not dirty message by building a new payload
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="colorKey">The color of the message</param>
    public void HandleMessage(ref SeString message, ushort colorKey)
    {
        ChatProximity.Log.Debug("Message not dirty");
        List<Payload> finalPayload =
        [
            new UIForegroundPayload(colorKey),
            new TextPayload(message.TextValue),
            UIForegroundPayload.UIForegroundOff

        ];

        message = new SeString(finalPayload);
    }
}
