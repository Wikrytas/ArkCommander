using System.Runtime.InteropServices;

namespace ArkCommander.Helpers;

public static class KeyMapper
{
    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    public static bool TryGetVk(string keyName, out uint vk)
    {
        vk = 0;

        if (string.IsNullOrWhiteSpace(keyName))
            return false;

        var key = keyName.Trim().ToUpperInvariant();

        switch (key)
        {
            case "ENTER":
            case "RETURN":
                vk = 0x0D;
                return true;

            case "TAB":
                vk = 0x09;
                return true;

            case "ESC":
            case "ESCAPE":
                vk = 0x1B;
                return true;

            case "INSERT":
                vk = 0x2D;
                return true;

            case "DELETE":
                vk = 0x2E;
                return true;

            case "HOME":
                vk = 0x24;
                return true;

            case "END":
                vk = 0x23;
                return true;

            case "SPACE":
                vk = 0x20;
                return true;
        }

        if (key.Length > 1 && key[0] == 'F')
        {
            if (int.TryParse(key.Substring(1), out int f) && f >= 1 && f <= 24)
            {
                vk = 0x70u + (uint)f - 1;
                return true;
            }
        }

        if (key.Length == 1)
        {
            char c = key[0];

            if (char.IsDigit(c) || char.IsLetter(c))
            {
                short scan = VkKeyScan(c);

                if (scan != -1)
                {
                    vk = (uint)(scan & 0xFF);
                    return true;
                }
            }
        }

        return false;
    }
}