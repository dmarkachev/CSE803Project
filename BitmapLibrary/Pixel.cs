using System.Windows.Media;

namespace BitmapLibrary
{
   public class Pixel
   {
      private Pixel( byte blue, byte green, byte red )
      {
         Color = Color.FromRgb( red, green, blue );
      }

      public Color Color { get; private set; }

      public byte Red
      {
         get { return Color.R; }
      }

      public byte Green
      {
         get { return Color.G; }
      }

      public byte Blue
      {
         get { return Color.B; }
      }

      public static Pixel GetPixel( byte[] pixelArray, int index )
      {
         if ( index >= 0 && index < pixelArray.Length && index % 4 == 0 )
         {
            return new Pixel( pixelArray[index], pixelArray[index + 1], pixelArray[index + 2] );
         }
         return null;
      }
   }
}
