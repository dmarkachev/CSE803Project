using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BitmapLibrary
{
   public static class BitmapColorer
   {
      /// <summary>
      /// Colors the connected objects in the bitmap. Note this function assumes the image is thresholded
      /// </summary>
      /// <param name="bitmap">The bitmap to color</param>
      /// <returns>A list of final colors in the image, one for each object</returns>
      public static List<Color> ColorBitmap( WriteableBitmap bitmap )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         var colorMergeDictionary = new Dictionary<int, int>();
         var colors = new List<Color>();
         var random = new Random();
         for ( int i = 0; i < bitmap.PixelHeight; i++ )
         {
            for ( int j = 0; j < bitmap.PixelWidth; j++ )
            {
               int index = i * stride + 4 * j;

               // Check if it is a foreground object and has not been colored
               if ( GetPixelColor( pixelArray, index ) == Colors.Black )
               {
                  var color = GetPixelColoring( pixelArray, index, stride, colors, random, colorMergeDictionary );
                  pixelArray[index] = color.B;
                  pixelArray[index + 1] = color.G;
                  pixelArray[index + 2] = color.R;

                  // Keep track of all the colors in the image so
                  // we can merge them later
                  if ( !colors.Contains( color ) )
                  {
                     colors.Add( color );
                     colorMergeDictionary[colors.Count - 1] = colors.Count - 1;
                  }
               }
            }
         }

         var trueColors = MergeConnectedColors( pixelArray, colors, colorMergeDictionary );

         bitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), pixelArray, stride, 0 );

         return trueColors;
      }

      /// <summary>
      /// Merges colors in the colors array as defined by the colorMergeDictionary
      /// </summary>
      /// <param name="pixelArray">The 1D array of bytes that represents the colors of the bitmap</param>
      /// <param name="colors">All the colors currently on the image</param>
      /// <param name="colorMergeDictionary">The dictionary that defines which colors need to be merged</param>
      /// <returns></returns>
      private static List<Color> MergeConnectedColors( byte[] pixelArray, List<Color> colors, Dictionary<int, int> colorMergeDictionary )
      {
         // Resolve any chains in the color merging dictionary.
         // For example if 4 -> 2, and 2 -> 1, then we make 4 -> 1
         var trueColors = new List<Color>();
         for ( int i = 0; i < colorMergeDictionary.Count; i++ )
         {
            while ( true )
            {
               if ( colorMergeDictionary[i] == colorMergeDictionary[colorMergeDictionary[i]] )
               {
                  if ( !trueColors.Contains( colors[colorMergeDictionary[i]] ) )
                  {
                     trueColors.Add( colors[colorMergeDictionary[i]] );
                  }
                  break;
               }
               colorMergeDictionary[i] = colorMergeDictionary[colorMergeDictionary[i]];
            }
         }

         // Do the color merging
         for ( int i = 0; i < pixelArray.Length; i += 4 )
         {
            var pixelColor = GetPixelColor( pixelArray, i );
            if ( pixelColor != Colors.White )
            {
               int colorIndex = colors.IndexOf( pixelColor );
               int colorCorrection = colorMergeDictionary[colorIndex];

               if ( pixelColor != colors[colorCorrection] )
               {
                  pixelArray[i] = colors[colorCorrection].B;
                  pixelArray[i + 1] = colors[colorCorrection].G;
                  pixelArray[i + 2] = colors[colorCorrection].R;
               }
            }
         }
         return trueColors;
      }

      /// <summary>
      /// Returns the lowest index color (of the array of colors already made) of the left, top left, top, and top right neighboring pixels.
      /// If none of those pixels have been colored, return a new random color
      /// </summary>
      /// <param name="pixelArray">The 1D array of bytes that represents the colors of the bitmap</param>
      /// <param name="index">The starting index of 4 bytes that represent the pixel color</param>
      /// <param name="stride">The stride (bytes per row) of the bitmap</param>
      /// <param name="currentColors">The array of colors already used in the image</param>
      /// <param name="colorGenerator">A Random() object used to generate a random color</param>
      /// <param name="colorMergeDictionary">A map for post-processing to correct any colors</param>
      /// <returns>A color corresponding to either the lowest index color of the neighbors, the color of the neighbors if they all match, or a new random color</returns>
      private static Color GetPixelColoring( byte[] pixelArray, int index, int stride, List<Color> currentColors, Random colorGenerator, Dictionary<int, int> colorMergeDictionary )
      {
         Color leftPixelColor = Colors.White;
         Color topLeftPixelColor = Colors.White;
         Color topPixelColor = Colors.White;
         Color topRightPixelColor = Colors.White;

         GetNeighborColors( pixelArray, index, stride, ref leftPixelColor, ref topLeftPixelColor, ref topPixelColor, ref topRightPixelColor );

         // All neighbors are background pixels
         if ( leftPixelColor == Colors.White && topLeftPixelColor == Colors.White &&
              topPixelColor == Colors.White && topRightPixelColor == Colors.White )
         {
            // return new color
            return Color.FromRgb( Convert.ToByte( colorGenerator.Next( 1, 254 ) ),
                                  Convert.ToByte( colorGenerator.Next( 1, 254 ) ),
                                  Convert.ToByte( colorGenerator.Next( 1, 254 ) ) );
         }

         // All neighbors are the same color
         if ( leftPixelColor == topLeftPixelColor && leftPixelColor == topPixelColor &&
              leftPixelColor == topRightPixelColor )
         {
            // return their color
            return leftPixelColor;
         }

         // Multiple different colorings:
         // Return the smallest index color
         var chosenColor = currentColors.First( x => x == leftPixelColor || x == topLeftPixelColor || x == topPixelColor || x == topRightPixelColor );

         // Add the color merges to the dictionary to ensure the colors that are touching are converted.
         // We do this by going to every color and settings its merge color to be that of the chosen color,
         // or the color it is already set to merge so if its index is smaller.
         if ( leftPixelColor != Colors.White )
         {
            colorMergeDictionary[currentColors.IndexOf( leftPixelColor )] =
               Math.Min( currentColors.IndexOf( chosenColor ),
                  colorMergeDictionary[currentColors.IndexOf( leftPixelColor )] );
         }
         if ( topLeftPixelColor != Colors.White )
         {
            colorMergeDictionary[currentColors.IndexOf( topLeftPixelColor )] =
               Math.Min( currentColors.IndexOf( chosenColor ),
                  colorMergeDictionary[currentColors.IndexOf( topLeftPixelColor )] );
         }
         if ( topPixelColor != Colors.White )
         {
            colorMergeDictionary[currentColors.IndexOf( topPixelColor )] =
               Math.Min( currentColors.IndexOf( chosenColor ),
                  colorMergeDictionary[currentColors.IndexOf( topPixelColor )] );
         }
         if ( topRightPixelColor != Colors.White )
         {
            colorMergeDictionary[currentColors.IndexOf( topRightPixelColor )] =
               Math.Min( currentColors.IndexOf( chosenColor ),
                  colorMergeDictionary[currentColors.IndexOf( topRightPixelColor )] );
         }

         return chosenColor;
      }

      /// <summary>
      /// Returns the coloring of the left, top left, top, and top right pixels. Sets that color to Transparent if it is a background pixel
      /// </summary>
      /// <param name="pixelArray">The 1D array of bytes that represents the colors of the bitmap</param>
      /// <param name="index">The index of the first byte of the pixel whose neighbors we are checking</param>
      /// <param name="stride">The stride (bytes per row) of the image</param>
      /// <param name="leftPixelColor">This is set to the coloring of the pixel to the left of the target pixel</param>
      /// <param name="topLeftPixelColor">This is set to the coloring of the pixel to the top left of the target pixel</param>
      /// <param name="topPixelColor">This is set to the coloring of the pixel above the target pixel</param>
      /// <param name="topRightPixelColor">This is set to the coloring of the pixel to the top right of the target pixel</param>
      private static void GetNeighborColors( byte[] pixelArray, int index, int stride, ref Color leftPixelColor, ref Color topLeftPixelColor, ref Color topPixelColor, ref Color topRightPixelColor )
      {
         var leftPixel = Pixel.GetPixel( pixelArray, index - 4 );
         if ( leftPixel != null )
         {
            leftPixelColor = leftPixel.Color;
         }
         var topleftPixel = Pixel.GetPixel( pixelArray, index - stride - 4 );
         if ( topleftPixel != null )
         {
            topLeftPixelColor = topleftPixel.Color;
         }
         var topPixel = Pixel.GetPixel( pixelArray, index - stride );
         if ( topPixel != null )
         {
            topPixelColor = topPixel.Color;
         }
         var topRightPixel = Pixel.GetPixel( pixelArray, index - stride + 4 );
         if ( topRightPixel != null )
         {
            topRightPixelColor = topRightPixel.Color;
         }
      }

      /// <summary>
      /// Takes a bitmap whites out all the pixels of a color not of the given color
      /// </summary>
      /// <param name="bitmap">The bitmap to start with</param>
      /// <param name="color">The color to keep</param>
      /// <returns>A bitmap that only has the given color remaining</returns>
      public static WriteableBitmap EraseAllButCertainColor( WriteableBitmap bitmap, Color color )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
         byte[] newPixelByteArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelByteArray, stride, 0 );

         for ( int i = 0; i < bitmap.PixelWidth; i++ )
         {
            for ( int j = 0; j < bitmap.PixelHeight; j++ )
            {
               int index = j * stride + 4 * i;
               if ( GetPixelColor( pixelByteArray, index ) != color )
               {
                  newPixelByteArray[index] = 255;
                  newPixelByteArray[index + 1] = 255;
                  newPixelByteArray[index + 2] = 255;
               }
               else
               {
                  newPixelByteArray[index] = color.B;
                  newPixelByteArray[index + 1] = color.G;
                  newPixelByteArray[index + 2] = color.R;
               }
            }
         }
         var newBitmap = bitmap.Clone();
         newBitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), newPixelByteArray, stride, 0 );

         return newBitmap;
      }


      public static void EraseAllButCertainColorandWhite(WriteableBitmap bitmap, System.Windows.Media.Color color)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          bitmap.CopyPixels(pixelByteArray, stride, 0);

          for (int column = 0; column < bitmap.PixelWidth; column++)
          {
              for (int row = 0; row < bitmap.PixelHeight; row++)
              {
                  int index = row * stride + 4 * column;

                  if (GetPixelColor(pixelByteArray, index) != color && GetPixelColor(pixelByteArray, index) != Colors.White)
                  {
                      pixelByteArray[index] = 0;
                      pixelByteArray[index + 1] = 0;
                      pixelByteArray[index + 2] = 0;
                  }
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
      }

      /// <summary>
      /// Draws a red rectangle on the bitmap
      /// </summary>
      /// <param name="bitmap">The bitmap to draw on</param>
      /// <param name="rect">The position of the rectangle to draw</param>
      public static void DrawRectangle( WriteableBitmap bitmap, Rect rect )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelByteArray, stride, 0 );

         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               if ( ( column == rect.X && row >= rect.Y && row <= rect.Y + rect.Height ) || 
                    ( row == rect.Y && column >= rect.X && column <= rect.X + rect.Width ) || 
                    ( column == rect.X + rect.Width && row >= rect.Y && row <= rect.Y + rect.Height ) || 
                    ( row == rect.Y + rect.Height && column >= rect.X && column <= rect.X + rect.Width ) )
               {
                  int index = row * stride + 4 * column;
                  pixelByteArray[index] = 0;
                  pixelByteArray[index + 1] = 0;
                  pixelByteArray[index + 2] = 255;
               }
            }
         }
         bitmap.WritePixels( new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight ), pixelByteArray, stride, 0 );
      }

      private static Color GetPixelColor( byte[] pixelArray, int index )
      {
         // Pixels are in BGR format
         return Color.FromRgb( pixelArray[index + 2], pixelArray[index + 1], pixelArray[index] );
      }

      public static Rect GetBoundingBoxOfColor( WriteableBitmap bitmap, Color color )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         var boundingBox = new Rect( int.MaxValue, int.MaxValue, 0, 0 );
         for ( int column = 0; column < bitmap.PixelWidth; column++ )
         {
            for ( int row = 0; row < bitmap.PixelHeight; row++ )
            {
               int index = row * stride + 4 * column;
               if ( GetPixelColor( pixelArray, index ) == color )
               {
                  boundingBox.X = Math.Min( boundingBox.X, column );
                  boundingBox.Y = Math.Min( boundingBox.Y, row );
                  boundingBox.Width = Math.Max( boundingBox.Width, column - boundingBox.X );
                  boundingBox.Height = Math.Max( boundingBox.Height, row - boundingBox.Y );
               }
            }
         }

         return boundingBox;
      }
   }
}
