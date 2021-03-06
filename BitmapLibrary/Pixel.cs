﻿using System;
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

   public class PixelWrapper
   {
       private byte[] pixelArray;

       public PixelWrapper(byte[] pixelArray)
       {
           this.pixelArray = pixelArray;
       }

       public void SetPixelColors(int R, int G, int B, int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               //blue
               pixelArray[index] = Convert.ToByte(B);
               //green
               pixelArray[index + 1] = Convert.ToByte(G);
               //red
               pixelArray[index + 2] = Convert.ToByte(R);
           }
       }

       public void SetPixelGreyColor(int grey, int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               //blue
               pixelArray[index] = Convert.ToByte(grey);
               //green
               pixelArray[index + 1] = Convert.ToByte(grey);
               //red
               pixelArray[index + 2] = Convert.ToByte(grey);
           }
       }

       public int getRed(int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               return pixelArray[index + 2];
           }
           else
           {
               return 0;
           }
       }

       public int getGreen(int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               return pixelArray[index + 1];
           }
           else
           {
               return 0;
           }
       }

       public int getBlue(int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               return pixelArray[index];
           }
           else
           {
               return 0;
           }
       }
       public int getGray(int index)
       {
           if (index >= 0 && index < pixelArray.Length && index % 4 == 0)
           {
               return pixelArray[index];
           }
           else
           {
               return 0;
           }
       }
   }
}
