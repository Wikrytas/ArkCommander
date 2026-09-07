namespace ArkCommander.Services;

public static class TemplateHelper
{
    public static string Build(string template, Dictionary<string, string> vars)
    {
        var r = template;
        foreach (var kv in vars)
            r = r.Replace("{" + kv.Key + "}", kv.Value);
        return r;
    }
}
