using System.Windows.Media;
using System;
using System.Dynamic;

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

   public class PixelWrapper
   {
       private byte[] pixelArray;

       private int index;

       public PixelWrapper(byte[] pixelArray)
       {
           this.pixelArray = pixelArray;
       }

       public void SetIndex(int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               this.index = index;
           }  
       }

       public void SetPixelColors(int R, int G, int B)
       {
           //blue
           pixelArray[index] = Convert.ToByte(B);
           //green
           pixelArray[index + 1] = Convert.ToByte(G);
           //red
           pixelArray[index + 2] = Convert.ToByte(R);
       }

       public void SetPixelGreyColor(int grey)
       {
           //blue
           pixelArray[index] = Convert.ToByte(grey);
           //green
           pixelArray[index + 1] = Convert.ToByte(grey);
           //red
           pixelArray[index + 2] = Convert.ToByte(grey);
       }

       public int getRed()
       {
           return pixelArray[index + 2];
       }

       public int getGreen()
       {
           return pixelArray[index + 1];
       }

       public int getBlue()
       {
           return pixelArray[index];
       }
   }
}
