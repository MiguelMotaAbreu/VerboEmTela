using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Layout;

public interface IPanelLayoutRecognizer
{
    LayoutRecognition Recognize(Rgb24Frame frame);
}
