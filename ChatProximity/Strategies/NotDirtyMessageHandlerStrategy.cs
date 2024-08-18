using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

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
        message = new SeStringBuilder().PushColorType(colorKey).Append(message.TextValue).ToSeString().ToDalamudString();
    }
}
