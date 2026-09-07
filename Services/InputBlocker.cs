using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ArkCommander.Services;

public static class InputBlocker
{
    [DllImport("user32.dll")]
    private static extern bool BlockInput(bool fBlockIt);

    private static CancellationTokenSource? _cts;

    public static void Block(int autoReleaseMs = 5000)
    {
        try
        {
            BlockInput(true);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // страховка: даже если что-то пойдёт не так, блок снимется сам
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(autoReleaseMs, token);

                    if (!token.IsCancellationRequested)
                        BlockInput(false);
                }
                catch
                {
                }
            });
        }
        catch
        {
        }
    }

    public static void Unblock()
    {
        try
        {
            _cts?.Cancel();
            BlockInput(false);
        }
        catch
        {
        }
    }
}
