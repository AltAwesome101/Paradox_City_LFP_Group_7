using UnityEngine;

/// <summary>
/// Helper class to dynamically,readably customize strings via colours,font, size and etc...
/// </summary>

public static class RichTextFormatter
{
    public static string WithColour(this string text,Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    public static string WithBold(this string text) => $"<b>{text}</b>";
    public static string WithSize(this string text, int size) => $"<size = {size}>{text}</size>";
    public static string WithItalics(this string text) => $"<i>{text}</i>";
}
