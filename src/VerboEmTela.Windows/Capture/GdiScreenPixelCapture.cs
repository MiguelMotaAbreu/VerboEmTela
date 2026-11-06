using System.ComponentModel;
using System.Runtime.InteropServices;
using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Windows.Capture;

public sealed class GdiScreenPixelCapture : IScreenPixelCapture
{
    private const uint SourceCopy = 0x00CC0020;
    private const uint CaptureLayeredWindows = 0x40000000;
    private const uint DibRgbColors = 0;
    private static readonly nint InvalidGdiObject = new(-1);

    public Rgb24Frame Capture(ScreenRegion region)
    {
        var screenDc = NativeMethods.GetDC(nint.Zero);
        if (screenDc == nint.Zero)
        {
            throw NativeFailure("Could not acquire the screen device context.");
        }

        nint memoryDc = nint.Zero;
        nint bitmap = nint.Zero;
        nint previousObject = nint.Zero;

        try
        {
            memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memoryDc == nint.Zero)
            {
                throw NativeFailure("Could not create a compatible device context.");
            }

            bitmap = NativeMethods.CreateCompatibleBitmap(
                screenDc,
                region.Width,
                region.Height);
            if (bitmap == nint.Zero)
            {
                throw NativeFailure("Could not create the capture bitmap.");
            }

            previousObject = NativeMethods.SelectObject(memoryDc, bitmap);
            if (previousObject == nint.Zero || previousObject == InvalidGdiObject)
            {
                throw NativeFailure("Could not select the capture bitmap.");
            }

            if (!NativeMethods.BitBlt(
                memoryDc,
                0,
                0,
                region.Width,
                region.Height,
                screenDc,
                region.Left,
                region.Top,
                SourceCopy | CaptureLayeredWindows))
            {
                throw NativeFailure("Could not copy pixels from the screen.");
            }

            var stride = checked(((region.Width * 3) + 3) & ~3);
            var bgrPixels = new byte[checked(stride * region.Height)];
            var bitmapInfo = BitmapInfo.CreateTopDown24Bit(
                region.Width,
                region.Height);

            var copiedLines = NativeMethods.GetDIBits(
                memoryDc,
                bitmap,
                0,
                (uint)region.Height,
                bgrPixels,
                ref bitmapInfo,
                DibRgbColors);

            if (copiedLines != region.Height)
            {
                throw NativeFailure("Could not read all captured bitmap rows.");
            }

            var rgbPixels = PixelBufferConverter.PaddedBgr24ToRgb24(
                bgrPixels,
                region.Width,
                region.Height,
                stride);

            return new Rgb24Frame(region.Width, region.Height, rgbPixels);
        }
        finally
        {
            if (previousObject != nint.Zero &&
                previousObject != InvalidGdiObject &&
                memoryDc != nint.Zero)
            {
                _ = NativeMethods.SelectObject(memoryDc, previousObject);
            }

            if (bitmap != nint.Zero)
            {
                _ = NativeMethods.DeleteObject(bitmap);
            }

            if (memoryDc != nint.Zero)
            {
                _ = NativeMethods.DeleteDC(memoryDc);
            }

            _ = NativeMethods.ReleaseDC(nint.Zero, screenDc);
        }
    }

    private static Win32Exception NativeFailure(string message)
    {
        return new Win32Exception(Marshal.GetLastWin32Error(), message);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;

        public static BitmapInfo CreateTopDown24Bit(int width, int height)
        {
            return new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 24,
                    Compression = 0,
                },
            };
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint GetDC(nint window);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(nint window, nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern nint CreateCompatibleDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern nint CreateCompatibleBitmap(
            nint deviceContext,
            int width,
            int height);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern nint SelectObject(nint deviceContext, nint graphicObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(nint graphicObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt(
            nint destinationDeviceContext,
            int destinationX,
            int destinationY,
            int width,
            int height,
            nint sourceDeviceContext,
            int sourceX,
            int sourceY,
            uint rasterOperation);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int GetDIBits(
            nint deviceContext,
            nint bitmap,
            uint firstScanLine,
            uint scanLineCount,
            [Out] byte[] pixels,
            ref BitmapInfo bitmapInfo,
            uint usage);
    }
}
