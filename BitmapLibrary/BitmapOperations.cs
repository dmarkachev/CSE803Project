using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BitmapLibrary
{
   public static class BitmapOperations
   {
      /// <summary>
      /// A simplified way of creating a writeable bitmap
      /// </summary>
      /// <param name="width">The desired width of the bitmap</param>
      /// <param name="height">The desired height of the bitmap</param>
      /// <returns>A blank width x height WriteableBitmap</returns>
      public static WriteableBitmap CreateBitmap( int width, int height )
      {
         return new WriteableBitmap( width, height, 96, 96, PixelFormats.Bgr32, null );
      }

      /// <summary>
      /// Returns a bitmap where everything is white except the boundary pixels of the given bitmap
      /// </summary>
      /// <param name="bitmap">The bitmap we are getting the perimeter off</param>
      /// <returns>A bitmap where the only non-white pixels are boundary pixels</returns>
      public static WriteableBitmap GetPerimeterBitmap( WriteableBitmap bitmap )
      {
         // Erode to remove the boundaary pixels
         var erodedTwiceBitmap = GetErodedBitmap( bitmap );

         // Find the difference between the two bitmap to get a bitmap
         // with only boundary pixels
         return ExclusiveOrBitmaps( bitmap, erodedTwiceBitmap );
      }

      /// <summary>
      /// Returns a list of pixel indexes representing every colored pixel in the bitmap assuming all
      /// non-white pixels are connected and represent the boundry of an object
      /// </summary>
      /// <param name="pixelArray">The 1D pixel byte array of the bitmap</param>
      /// <param name="height">The height of the image</param>
      /// <param name="width">The width of the iamge</param>
      /// <param name="stride">The stride of the image</param>
      /// <returns>An array of ints that correspond to the indexes of the boundry pixels in the pixel array, 
      /// where one pixel neighbors the next pixel in the array</returns>
      public static List<int> BuildPerimeterPath( byte[] pixelArray, int height, int width, int stride )
      {
         var perimeterList = new List<int>();

         // Search for a perimeter pixel
         var foundPixel = -1;
         for ( int row = 0; row < height && foundPixel < 0; row++ )
         {
            for ( int column = 0; column < width && foundPixel < 0; column++ )
            {
               int index = row * stride + 4 * column;
               var pixel = Pixel.GetPixel( pixelArray, index );
               if ( pixel.Color == Colors.Black )
               {
                  foundPixel = index;
               }
            }
         }

         // Follow the neighbors of the perimeter pixels until we come back to the start
         do
         {
            perimeterList.Add( foundPixel );
            pixelArray[foundPixel] = 255;    // Mark the pixel as "found"

            var topRightPixel = Pixel.GetPixel( pixelArray, foundPixel - stride + 4 );
            var rightPixel = Pixel.GetPixel( pixelArray, foundPixel + 4 );
            var bottomRightPixel = Pixel.GetPixel( pixelArray, foundPixel + 4 + stride );
            var bottomPixel = Pixel.GetPixel( pixelArray, foundPixel + stride );
            var bottomLeftPixel = Pixel.GetPixel( pixelArray, foundPixel + stride - 4 );
            var leftPixel = Pixel.GetPixel( pixelArray, foundPixel - 4 );
            var topLeftPixel = Pixel.GetPixel( pixelArray, foundPixel - 4 - stride );
            var topPixel = Pixel.GetPixel( pixelArray, foundPixel - stride );

            if ( topRightPixel != null && topRightPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel - stride + 4;
            }
            else if ( rightPixel != null && rightPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel + 4;
            }
            else if ( bottomRightPixel != null && bottomRightPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel + stride + 4;
            }
            else if ( bottomPixel != null && bottomPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel + stride;
            }
            else if ( bottomLeftPixel != null && bottomLeftPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel + stride - 4;
            }
            else if ( leftPixel != null && leftPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel - 4;
            }
            else if ( topLeftPixel != null && topLeftPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel - stride - 4;
            }
            else if ( topPixel != null && topPixel.Color == Colors.Black )
            {
               foundPixel = foundPixel - stride;
            }
         } while ( !perimeterList.Contains( foundPixel ) );

         return perimeterList;
      }

      /// <summary>
      /// Calculates the differences between the two bitmap and returns a bitmap where those differences
      /// are black and the rest of the bitmap is white
      /// </summary>
      /// <param name="firstBitmap">The first bitmap to compare</param>
      /// <param name="secondBitmap">The second bitmap to compare</param>
      /// <returns>A bitmap where all the black pixels represent where the two bitmaps were different
      /// and the white pixels represent where they were the same</returns>
      private static WriteableBitmap ExclusiveOrBitmaps( WriteableBitmap firstBitmap, WriteableBitmap secondBitmap )
      {
         int stride = ( firstBitmap.PixelWidth * firstBitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] firstPixelArray = new byte[firstBitmap.PixelHeight * stride];
         byte[] secondPixelArray = new byte[secondBitmap.PixelHeight * stride];
         byte[] ordPixelArray = new byte[secondBitmap.PixelHeight * stride];
         firstBitmap.CopyPixels( firstPixelArray, stride, 0 );
         secondBitmap.CopyPixels( secondPixelArray, stride, 0 );

         for ( int i = 0; i < firstBitmap.PixelWidth; i++ )
         {
            for ( int j = 0; j < firstBitmap.PixelHeight; j++ )
            {
               int index = j * stride + 4 * i;

               var firstBitmapPixel = Pixel.GetPixel( firstPixelArray, index );
               var secondBitmapPixel = Pixel.GetPixel( secondPixelArray, index );

               // If the pixels are the same, they are white in the exclusive or bitmap
               if ( firstBitmapPixel.Color == secondBitmapPixel.Color )
               {
                  ordPixelArray[index] = 255;
                  ordPixelArray[index + 1] = 255;
                  ordPixelArray[index + 2] = 255;
               }
               else
               {
                  // Otherwise black
                  ordPixelArray[index] = 0;
                  ordPixelArray[index + 1] = 0;
                  ordPixelArray[index + 2] = 0;
               }
            }
         }
         var ordBitmap = firstBitmap.Clone();
         ordBitmap.WritePixels( new Int32Rect( 0, 0, firstBitmap.PixelWidth, firstBitmap.PixelHeight ), ordPixelArray, stride, 0 );

         return ordBitmap;
      }

      /// <summary>
      /// Returns a copy of the given bitmap eroded by 1 pixel
      /// </summary>
      /// <param name="bitmap">The bitmap to be eroded</param>
      /// <returns>The eroded bitmap</returns>
      public static WriteableBitmap GetErodedBitmap( WriteableBitmap bitmap )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         byte[] newPixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               int index = row * stride + 4 * column;

               // If we are on the boundry just put white in the new image
               if ( column == 0 || row == 0 || column == bitmap.PixelWidth - 1 || row == bitmap.PixelHeight - 1 )
               {
                  newPixelArray[index] = 255;
                  newPixelArray[index + 1] = 255;
                  newPixelArray[index + 2] = 255;
                  continue;
               }

               var currentPixel = Pixel.GetPixel( pixelArray, index );
               var topPixel = Pixel.GetPixel( pixelArray, index - stride );
               var leftPixel = Pixel.GetPixel( pixelArray, index - 4 );
               var rightPixel = Pixel.GetPixel( pixelArray, index + 4 );
               var bottomPixel = Pixel.GetPixel( pixelArray, index + stride );

               // If the current pixel and all of its neighbored pixels are colored (not white), the new pixel is black
               if ( topPixel.Color != Colors.White &&
                    leftPixel.Color != Colors.White &&
                    rightPixel.Color != Colors.White &&
                    bottomPixel.Color != Colors.White &&
                    currentPixel.Color != Colors.White )
               {
                  newPixelArray[index] = 0;
                  newPixelArray[index + 1] = 0;
                  newPixelArray[index + 2] = 0;
               }
               else
               {
                  // Otherwise we make the pixel white
                  newPixelArray[index] = 255;
                  newPixelArray[index + 1] = 255;
                  newPixelArray[index + 2] = 255;
               }
            }
         }
         var erodedBitmap = bitmap.Clone();
         erodedBitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), newPixelArray, stride, 0 );

         return erodedBitmap;
      }

      /// <summary>
      /// Returns a copy of the given bitmap dilated by 1
      /// </summary>
      /// <param name="bitmap">The bitmap to be dilated</param>
      /// <returns>The dilated bitmap</returns>
      public static WriteableBitmap GetDilatedBitmap( WriteableBitmap bitmap )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         byte[] newPixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               int index = row * stride + 4 * column;

               // If we are on the boundry just put white in the new image
               if ( column == 0 || row == 0 || column == bitmap.PixelWidth - 1 || row == bitmap.PixelHeight - 1 )
               {
                  newPixelArray[index] = 255;
                  newPixelArray[index + 1] = 255;
                  newPixelArray[index + 2] = 255;
                  continue;
               }

               var currentPixel = Pixel.GetPixel( pixelArray, index );
               var topPixel = Pixel.GetPixel( pixelArray, index - stride );
               var leftPixel = Pixel.GetPixel( pixelArray, index - 4 );
               var rightPixel = Pixel.GetPixel( pixelArray, index + 4 );
               var bottomPixel = Pixel.GetPixel( pixelArray, index + stride );

               // If the current pixel or one if its neighbored pixels are colored (not white), the new pixel is black
               if ( topPixel.Color != Colors.White ||
                    leftPixel.Color != Colors.White ||
                    rightPixel.Color != Colors.White ||
                    bottomPixel.Color != Colors.White ||
                    currentPixel.Color != Colors.White )
               {
                  newPixelArray[index] = 0;
                  newPixelArray[index + 1] = 0;
                  newPixelArray[index + 2] = 0;
               }
               else
               {
                  // Otherwise we make the pixel white
                  newPixelArray[index] = 255;
                  newPixelArray[index + 1] = 255;
                  newPixelArray[index + 2] = 255;
               }
            }
         }
         var dilatedBitmap = bitmap.Clone();
         dilatedBitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), newPixelArray, stride, 0 );

         return dilatedBitmap;
      }

      /// <summary>
      /// Loops through all the pixels and averages the RGB values to grayscale the image
      /// </summary>
      /// <param name="bitmap">The bitmap to be grayscaled</param>
      public static void GrayscaleBitmap( WriteableBitmap bitmap )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelByteArray, stride, 0 );

         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               int index = row * stride + 4 * column;
               var gray = Convert.ToByte( ( pixelByteArray[index] + pixelByteArray[index + 1] + pixelByteArray[index + 2] ) / 3 );
               pixelByteArray[index] = gray;
               pixelByteArray[index + 1] = gray;
               pixelByteArray[index + 2] = gray;
            }
         }
         bitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), pixelByteArray, stride, 0 );
      }


      /// <summary>
      /// Loops through all the pixels and averages the RGB values to grayscale the image
      /// </summary>
      /// <param name="bitmap">The bitmap to be grayscaled</param>
      public static void BlueScaleBitmap(WriteableBitmap bitmap)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          bitmap.CopyPixels(pixelByteArray, stride, 0);

          var pixel = new PixelWrapper(pixelByteArray);

          for (int column = 1; column < bitmap.PixelWidth; column+=4)
          {
              for (int row = 1; row < bitmap.PixelHeight; row+=4)
              {
                  int index = row*stride + 4*column;

                  int leftPixelIndex = index - 4;
                  int topLeftPixelIndex = index - stride - 4;
                  int topPixelIndex = index - stride;
                  int bottomPixelIndex = index + stride;
                  int topRightPixelIndex = index - stride + 4;

                  pixel.SetIndex(index);
                  pixel.SetPixelGreyColor(100);
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
      }


      /// <summary>
      /// Thresholds the supplied bitmap using the supplied theshold value
      /// </summary>
      /// <param name="bitmap">The bitmap to be thresholded</param>
      /// <param name="threshold">The theshold value by which to process the bitmap</param>
      public static void ThresholdBitmap( WriteableBitmap bitmap, int threshold, bool invert )
      {
         int objectValue = invert ? 255 : 0;
         int backgroundValue = invert ? 0 : 255;
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelByteArray, stride, 0 );

         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               int index = row * stride + 4 * column;
               var color = Pixel.GetPixel( pixelByteArray, index ).Color;
               var whiteBlack = Convert.ToByte( pixelByteArray[index] >= threshold ? backgroundValue : objectValue );
               pixelByteArray[index] = whiteBlack;
               pixelByteArray[index + 1] = whiteBlack;
               pixelByteArray[index + 2] = whiteBlack;
            }
         }
         bitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), pixelByteArray, stride, 0 );
      }

      public static void GaussianBlur( WriteableBitmap bitmap, int radius )
      {
         int sz = radius * 2 + 1;
         var kernel = new int[sz];
         var multable = new int[sz, 256];
         for ( int i = 1; i <= radius; i++ )
         {
            int szi = radius - i;
            int szj = radius + i;
            kernel[szj] = kernel[szi] = ( szi + 1 ) * ( szi + 1 );
            for ( int j = 0; j < 256; j++ )
            {
               multable[szj, j] = multable[szi, j] = kernel[szj] * j;
            }
         }
         kernel[radius] = ( radius + 1 ) * ( radius + 1 );
         for ( int j = 0; j < 256; j++ )
         {
            multable[radius, j] = kernel[radius] * j;
         }

         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         byte[] newPixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         int pixelCount = bitmap.PixelWidth * bitmap.PixelHeight;
         int[] b = new int[pixelCount];
         int[] g = new int[pixelCount];
         int[] r = new int[pixelCount];

         int[] b2 = new int[pixelCount];
         int[] g2 = new int[pixelCount];
         int[] r2 = new int[pixelCount];

         int index = 0;
         for ( int row = 0; row < bitmap.PixelHeight; row++ )
         {
            for ( int column = 0; column < bitmap.PixelWidth; column++ )
            {
               int pixelIndex = row * stride + 4 * column;
               b[index] = pixelArray[pixelIndex];
               g[index] = pixelArray[pixelIndex + 1];
               r[index] = pixelArray[pixelIndex + 2];

               ++index;
            }
         }

         int bsum;
         int gsum;
         int rsum;
         int sum;
         int read;
         int start = 0;
         index = 0;
         for ( int i = 0; i < bitmap.PixelHeight; i++ )
         {
            for ( int j = 0; j < bitmap.PixelWidth; j++ )
            {
               bsum = gsum = rsum = sum = 0;
               read = index - radius;

               for ( int z = 0; z < kernel.Length; z++ )
               {
                  if ( read >= start && read < start + bitmap.PixelWidth )
                  {
                     bsum += multable[z, b[read]];
                     gsum += multable[z, g[read]];
                     rsum += multable[z, r[read]];
                     sum += kernel[z];
                  }
                  ++read;
               }

               b2[index] = ( bsum / sum );
               g2[index] = ( gsum / sum );
               r2[index] = ( rsum / sum );

               ++index;
            }
            start += bitmap.PixelWidth;
         }

         for ( int row = 0; row < bitmap.PixelHeight; row++ )
         {
            int y = row - radius;
            start = y * bitmap.PixelWidth;
            for ( int column = 0; column < bitmap.PixelWidth; column++ )
            {
               bsum = gsum = rsum = sum = 0;
               read = start + column;
               var tempy = y;
               for ( int z = 0; z < kernel.Length; z++ )
               {
                  if ( tempy >= 0 && tempy < bitmap.PixelHeight )
                  {
                     bsum += multable[z, b2[read]];
                     gsum += multable[z, g2[read]];
                     rsum += multable[z, r2[read]];
                     sum += kernel[z];
                  }
                  read += bitmap.PixelWidth;
                  ++tempy;
               }

               int pixelIndex = row * stride + 4 * column;
               newPixelArray[pixelIndex] = (byte)( bsum / sum );
               newPixelArray[pixelIndex + 1] = (byte)( gsum / sum );
               newPixelArray[pixelIndex + 2] = (byte)( rsum / sum );
            }
         }

         bitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), newPixelArray, stride, 0 );
      }
   }
}
