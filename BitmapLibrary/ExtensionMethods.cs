using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BitmapLibrary
{
   public static class ExtensionMethods
   {
      /// <summary>
      /// A utlity method to allows parallel processing on a list of integers with a step size > 1
      /// </summary>
      /// <param name="fromInclusive">The first element of the list, inclusive</param>
      /// <param name="toExclusive">The last element of the list, exclusive</param>
      /// <param name="step">The step between each element in the list</param>
      /// <returns>The next element in the list being enumerated over</returns>
      public static IEnumerable<int> SteppedRange( int fromInclusive, int toExclusive, int step )
      {
         for ( var i = fromInclusive; i < toExclusive; i += step )
         {
            yield return i;
         }
      }

      /// <summary>
      /// Extension method for WriteableBitmap that saves the bitmap to a file
      /// </summary>
      /// <param name="bitmap">The bitmap to save</param>
      /// <param name="filename">The file to save the bitmap to</param>
      public static void Save( this WriteableBitmap bitmap, string filename )
      {
         using ( FileStream fs = new FileStream( filename, FileMode.Create ) )
         {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add( BitmapFrame.Create( bitmap ) );
            encoder.Save( fs );
            fs.Close();
         }
      }

      /// <summary>
      /// Takes a pixel array and given the information creates a new array with only the information
      /// that is within the provided rect of the original image
      /// </summary>
      /// <param name="array">The pixel array</param>
      /// <param name="x">The crop rect x offset</param>
      /// <param name="y">The crop rect y offset</param>
      /// <param name="width">The crop rect width</param>
      /// <param name="height">The crop rect height</param>
      /// <param name="stride">The stride of the original image</param>
      /// <returns>The pixel array containing only the cropped information</returns>
      public static byte[] CropPixelArray( this byte[] array, int x, int y, int width, int height, int stride )
      {
         var result = new byte[width * 4 * height];

         for ( var i = y; i < y + height; i++ )
         {
            var sourceIndex = ( x * 4 ) + ( i * stride );
            var destinationIndex = ( i - y ) * ( width * 4 );

            Array.Copy( array, sourceIndex, result, destinationIndex, width * 4 );
         }

         return result;
      }

      /// <summary>
      /// Sets all the pixels within a region given by the parameters to black in a pixel array
      /// </summary>
      /// <param name="array">The array</param>
      /// <param name="x">The x offset to start blacking out</param>
      /// <param name="y">The y offset to start blacking out</param>
      /// <param name="width">The width to start blacking out</param>
      /// <param name="height">The height to start blacking out</param>
      /// <param name="stride">The stride of the pixel array</param>
      public static void BlankPixels( this byte[] array, int x, int y, int width, int height, int stride )
      {
         for ( var i = y; i < y + height; i++ )
         {
            var sourceIndex = ( x * 4 ) + ( i * stride );
            for ( int j = 0; j < width * 4; j++ )
            {
               array[sourceIndex + j] = 0;
            }
         }
      }

      public static bool SignificantlyIntersects( this Rect containingRect, Rect otherRect )
      {
         if ( containingRect.Contains( otherRect ) || otherRect.Contains( containingRect ) )
         {
            return true;
         }

         if ( containingRect.IntersectsWith( otherRect ) )
         {
            var overlap = containingRect;
            overlap.Intersect( otherRect );
            return overlap.Width * overlap.Height > 0.5 * ( containingRect.Width * containingRect.Height ) ||
                   overlap.Width * overlap.Height > 0.5 * ( otherRect.Width * otherRect.Height );
         }
         return false;
      }
   }
}
