using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Cursor = System.Windows.Forms.Cursor;

public class GlobalKeyListener
{
    private static IntPtr _hookID = IntPtr.Zero;
    private static TaskCompletionSource<(int X, int Y)> _tcs;
    private static LowLevelKeyboardProc _proc = HookCallback;

    // Import necessary functions from user32.dll
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public static async Task<(int X, int Y)> WaitForRightAltPressAsync()
    {
        // Initialize the TaskCompletionSource to await Right Alt key press
        _tcs = new TaskCompletionSource<(int X, int Y)>();

        // Set the global hook
        _hookID = SetHook(_proc);

        // Await the TaskCompletionSource result, which completes when Right Alt is pressed
        var result = await _tcs.Task;

        // Unhook after getting the result
        UnhookWindowsHookEx(_hookID);

        return result;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104; // System key press
        const int VK_PAUSE = 0x13; // Key code for the Pause/Break key


        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);

            // Check for Right Alt key or Ctrl + Alt (for AltGr)
            if (vkCode == VK_PAUSE)
            {
                Debug.WriteLine("Right Alt (AltGr) key detected");

                // Get the current cursor position
                int x = Cursor.Position.X;
                int y = Cursor.Position.Y;

                // Complete the task with the cursor position
                _tcs.SetResult((x, y));

                // Return non-zero to suppress Right Alt event (optional)
                return (IntPtr)1;
            }
        }

        // Pass the event to other applications
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

}
