/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

namespace Firely.Fhir.Validation;

internal static class MessageValueHelpers
{
    // A longer value is cut short in a message, so a string of up to 1 MB doesn't end up in the outcome whole.
    private const int MAX_VALUE_LENGTH_IN_MESSAGE = 100;

    /// <summary>
    /// Whether <see cref="ShortenForMessage"/> cuts the value short.
    /// </summary>
    public static bool IsTooLongForMessage(this string value) => value.Length > MAX_VALUE_LENGTH_IN_MESSAGE;

    /// <summary>
    /// Returns the value as a message shows it: its first 100 characters followed by "..." when it is longer.
    /// </summary>
    public static string ShortenForMessage(this string value)
    {
        if (!value.IsTooLongForMessage())
            return value;

        // Don't cut a surrogate pair in half.
        var shown = char.IsHighSurrogate(value[MAX_VALUE_LENGTH_IN_MESSAGE - 1]) ? MAX_VALUE_LENGTH_IN_MESSAGE - 1 : MAX_VALUE_LENGTH_IN_MESSAGE;
        return value[..shown] + "...";
    }
}
