using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VerboEmTela.Windows.Capture;

public sealed class Win32DisplayMonitorProvider : IDisplayMonitorProvider
{
    private const uint PrimaryMonitorFlag = 0x00000001;

    public IReadOnlyList<DisplayMonitor> GetMonitors()
    {
        var monitors = new List<DisplayMonitor>();
        Exception? callbackFailure = null;

        bool AddMonitor(
            nint monitorHandle,
            nint deviceContext,
            nint monitorRectangle,
            nint state)
        {
            _ = deviceContext;
            _ = monitorRectangle;
            _ = state;

            var monitorInfo = MonitorInfoEx.Create();
            if (!NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                callbackFailure = NativeFailure(
                    "Could not read information about a display monitor.");
                return false;
            }

            var width = monitorInfo.Monitor.Right - monitorInfo.Monitor.Left;
            var height = monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top;

            try
            {
                monitors.Add(new DisplayMonitor(
                    monitorInfo.DeviceName,
                    new ScreenRegion(
                        monitorInfo.Monitor.Left,
                        monitorInfo.Monitor.Top,
                        width,
                        height),
                    (monitorInfo.Flags & PrimaryMonitorFlag) != 0));
            }
            catch (Exception exception)
                when (exception is ArgumentException or OverflowException)
            {
                callbackFailure = new InvalidOperationException(
                    "Windows returned invalid display-monitor information.",
                    exception);
                return false;
            }

            return true;
        }

        MonitorEnumerationCallback callback = AddMonitor;
        var completed = NativeMethods.EnumDisplayMonitors(
            nint.Zero,
            nint.Zero,
            callback,
            nint.Zero);
        GC.KeepAlive(callback);

        if (callbackFailure is not null)
        {
            throw callbackFailure;
        }

        if (!completed)
        {
            throw NativeFailure("Could not enumerate display monitors.");
        }

        return monitors;
    }

    private static Win32Exception NativeFailure(string message)
    {
        return new Win32Exception(Marshal.GetLastWin32Error(), message);
    }

    private delegate bool MonitorEnumerationCallback(
        nint monitorHandle,
        nint deviceContext,
        nint monitorRectangle,
        nint state);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public NativeRectangle Monitor;
        public NativeRectangle WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public static MonitorInfoEx Create()
        {
            return new MonitorInfoEx
            {
                Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
                DeviceName = string.Empty,
            };
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumDisplayMonitors(
            nint deviceContext,
            nint clippingRectangle,
            MonitorEnumerationCallback callback,
            nint state);

        [DllImport(
            "user32.dll",
            EntryPoint = "GetMonitorInfoW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(
            nint monitorHandle,
            ref MonitorInfoEx monitorInfo);
    }
}
