using System;
using ChatProximity.Config;
using ChatProximity.Handlers;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace ChatProximity.Service;

public class ColorService(ChatProximity plugin)
{
    public ChatProximity Plugin { get; init; } = plugin;

    /// <summary>
    /// Get the color according to the mode and pre-calculated data.
    /// </summary>
    /// <param name="mode">The coloring mode</param>
    /// <param name="config">The used config for this type</param>
    /// <param name="distance">The pre-calculated distance (if applicable)</param>
    /// <returns>A UI payload containing the color</returns>
    public Vector4 GetColor(RecolorMode mode, ChatTypeConfig config, float? distance = null)
    {
        ChatProximity.Log.Debug($"Recolor mode is {mode}");
        Vector4 color;

        switch (mode)
        {
            case RecolorMode.Distance:
                color = GetDistanceColor(config, distance);
                break;

            case RecolorMode.Targeting:
                color = config.TargetingColor;
                break;

            case RecolorMode.Targeted:
                color = config.TargetedColor;
                break;
            
            case RecolorMode.FocusTarget:
                color = config.FocusTargetColor;
                break;

            case RecolorMode.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        ChatProximity.Log.Debug($"Got color {color}");
        return color;
    }

    /// <summary>
    /// Computes the new color a message based on the distance separating the two players
    /// </summary>
    /// <param name="config">The active config</param>
    /// <param name="distance">The distance between the two characters</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">If no distance provided while the recolor mdoe is Distance</exception>
    private Vector4 GetDistanceColor(ChatTypeConfig config, float? distance)
    {
        if (distance == null)
            throw new ArgumentException("Distance must be provided for RecolorMode.Distance");

        var ratio = Math.Clamp(distance.Value / config.Range, 0f, 1f);
        var curvedRatio = ApplyColorCurve(ratio);
        
        ChatProximity.Log.Debug($"Raw ratio: {ratio} - Curved ratio: {curvedRatio}");

        return LerpColor(config.NearColor, config.FarColor, curvedRatio);
    }

    /// <summary>
    /// Choose the ratio function according to the config
    /// </summary>
    /// <param name="ratio">The computed ratio</param>
    /// <returns></returns>
    private float ApplyColorCurve(float ratio)
    {
        return Plugin.Configuration.ColorCurve switch
        {
            ColorCurve.Linear => ratio,
            ColorCurve.EaseOutQuadratic => EaseOutQuadratic(ratio),
            _ => ratio,
        };
    }

    /// <summary>
    /// Ease out quadratic curve
    /// </summary>
    /// <param name="ratio">The computed ratio</param>
    /// <returns></returns>
    private static float EaseOutQuadratic(float ratio)
    {
        return 1f - ((1f - ratio) * (1f - ratio));
    }

    /// <summary>
    /// Compute the actual vector color
    /// </summary>
    /// <param name="from">Position 1</param>
    /// <param name="to">Position 2</param>
    /// <param name="ratio">Computed ratio</param>
    /// <returns>A vector4 representing the color</returns>
    private static Vector4 LerpColor(Vector4 from, Vector4 to, float ratio)
    {
        return new Vector4(
            from.X + ((to.X - from.X) * ratio),
            from.Y + ((to.Y - from.Y) * ratio),
            from.Z + ((to.Z - from.Z) * ratio),
            from.W + ((to.W - from.W) * ratio)
        );
    }
}
