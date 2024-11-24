namespace ChatProximity.Handlers;

/// <summary>
/// Specifies the different modes of recoloring for chat messages.
/// </summary>
public enum RecolorMode
{
    /// <summary>
    /// No recoloring is applied to the chat messages.
    /// </summary>
    None,

    /// <summary>
    /// Recolors messages based on the distance to the sender.
    /// </summary>
    Distance,

    /// <summary>
    /// Recolors messages when the sender is targeting the player.
    /// </summary>
    Targeted,

    /// <summary>
    /// Recolors messages when the player is targeting the sender.
    /// </summary>
    Targeting
}
