using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Windows.Capture;

public interface IScreenPixelCapture
{
    Rgb24Frame Capture(ScreenRegion region);
}
