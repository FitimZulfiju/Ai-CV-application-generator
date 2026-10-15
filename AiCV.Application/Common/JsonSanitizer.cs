namespace AiCV.Application.Common;

/// <summary>
/// Provides utility methods to sanitize malformed JSON strings, such as unescaped control
/// characters (literal newlines, carriage returns, tabs) inside string literals, before
/// passing them to strict JSON parsers like System.Text.Json.
/// </summary>
public static class JsonSanitizer
{
    public static string Sanitize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json ?? string.Empty;
        }

        var sb = new StringBuilder(json.Length + 32);
        bool inString = false;
        bool isEscaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (c == '"' && !isEscaped)
            {
                inString = !inString;
                sb.Append(c);
                continue;
            }

            if (inString)
            {
                if (c == '\\')
                {
                    isEscaped = !isEscaped;
                    sb.Append(c);
                    continue;
                }

                if (!isEscaped)
                {
                    switch (c)
                    {
                        case '\n':
                            sb.Append(@"\n");
                            continue;
                        case '\r':
                            sb.Append(@"\r");
                            continue;
                        case '\t':
                            sb.Append(@"\t");
                            continue;
                        case '\b':
                            sb.Append(@"\b");
                            continue;
                        case '\f':
                            sb.Append(@"\f");
                            continue;
                        default:
                            if (c < 0x20)
                            {
                                sb.Append($@"\u{(int)c:x4}");
                                continue;
                            }
                            break;
                    }
                }

                isEscaped = false;
                sb.Append(c);
            }
            else
            {
                isEscaped = false;
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}

