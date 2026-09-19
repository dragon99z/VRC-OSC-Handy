using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VRC_OSC_Handy.Func
{
    public class ByteImageConverter
    {

        public static ImageSource ByteToImage(byte[] imageData)
        {
            BitmapImage biImg = new BitmapImage();
            using (var ms = new MemoryStream(imageData))
            {
                biImg.BeginInit();
                biImg.CacheOption = BitmapCacheOption.OnLoad; // decode fully so the stream can be disposed
                biImg.StreamSource = ms;
                biImg.EndInit();
            }
            biImg.Freeze(); // safe to use from any thread (called from background update loops)

            return biImg;
        }

    }
}
