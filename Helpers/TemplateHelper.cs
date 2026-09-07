namespace ArkCommander.Helpers;

public static class TemplateHelper
{
    public static string Build(string template, Dictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        string result = template;

        foreach (var kv in values)
        {
            result = result.Replace("{" + kv.Key + "}", kv.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}