using System.Text.Json;

namespace OpenIPRadar.Providers.Common;

/// <summary>
/// Null-safe helpers for reading optional values out of a <see cref="JsonElement"/>, keeping the
/// provider parsers concise and resilient to missing or differently-typed fields.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>Returns a string property, or <c>null</c> if absent/empty/not a string.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string value, or <c>null</c>.</returns>
    public static string? TryGetString(this JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var s = value.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        return null;
    }

    /// <summary>Returns an integer property, or <c>null</c> if absent/not a number.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The integer value, or <c>null</c>.</returns>
    public static int? TryGetInt(this JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>Returns an ISO-8601 timestamp property, or <c>null</c> if absent/unparsable.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The timestamp, or <c>null</c>.</returns>
    public static DateTimeOffset? TryGetDateTimeOffset(this JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && value.TryGetDateTimeOffset(out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>Returns a Unix-seconds timestamp property, or <c>null</c> if absent/not a number.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The timestamp, or <c>null</c>.</returns>
    public static DateTimeOffset? TryGetUnixSeconds(this JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        return null;
    }

    /// <summary>Returns a string array property as a list, or <c>null</c> if absent/empty.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The list of non-empty strings, or <c>null</c>.</returns>
    public static List<string>? TryGetStringList(this JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = array.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();

        return list.Count == 0 ? null : list;
    }

    /// <summary>Returns an integer array property as a list, or <c>null</c> if absent/empty.</summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The list of integers, or <c>null</c>.</returns>
    public static List<int>? TryGetIntList(this JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = array.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
            .Select(e => e.GetInt32())
            .ToList();

        return list.Count == 0 ? null : list;
    }
}
