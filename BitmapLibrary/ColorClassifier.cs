using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BitmapLibrary
{
   public static class ColorClassifier
   {
      private static readonly double[] FaceTemplate =
      {
         6.356666667,
         0.068333333,
         0,
         0,
         0.001666667,
         0.003333333,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         9.003333333,
         0.103333333,
         0,
         0,
         4.251666667,
         3.883333333,
         0.006666667,
         0,
         0,
         0,
         0.001666667,
         0,
         0,
         0,
         0,
         0,
         0.41,
         0.02,
         0,
         0,
         3.62,
         28.40666667,
         0.288333333,
         0,
         0,
         5.105,
         2.505,
         0.001666667,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         0,
         2.17,
         0,
         0,
         0,
         12.45833333,
         16.8,
         0.008333333,
         0,
         0,
         2.646666667,
         1.873333333
      };

      public static double[] GetColorBins( WriteableBitmap bitmap, bool normalize = false )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         return GetColorBins( pixelArray, normalize );
      }

      public static double[] GetColorBins( byte[] pixelArray, bool normalize = false )
      {
         var bins = new double[64];

         for ( int i = 0; i < 64; i++ )
         {
            bins[i] = 0;
         }

         for ( int pixelIndex = 0; pixelIndex < pixelArray.Count(); pixelIndex += 4 )
         {
            byte colorRepresentation = 0;

            // Shift right 6 to get 2 high bits, then shift left for its position in the final byte
            byte highRed = (byte)( ( pixelArray[pixelIndex + 2] >> 6 ) << 4 );
            byte highGreen = (byte)( ( pixelArray[pixelIndex + 1] >> 6 ) << 2 );
            byte highBlue = (byte)( pixelArray[pixelIndex] >> 6 );

            colorRepresentation |= highRed;
            colorRepresentation |= highGreen;
            colorRepresentation |= highBlue;

            bins[colorRepresentation]++;
         }

         return normalize ? NormalizeBins( bins ) : bins;
      }

      public static double[] GetColorBinsWithinBlob(byte[] pixelArray, byte[] thresholdedArray, bool normalize = false)
      {
          var bins = new double[64];

          for (int i = 0; i < 64; i++)
          {
              bins[i] = 0;
          }

          for (int pixelIndex = 0; pixelIndex < pixelArray.Count(); pixelIndex += 4)
          {
              int pixelValue = Convert.ToInt32(thresholdedArray[pixelIndex]);

              if (pixelValue == 0) // only count pixels within the blob in distance calculation
              {
                  byte colorRepresentation = 0;

                  // Shift right 6 to get 2 high bits, then shift left for its position in the final byte
                  byte highRed = (byte) ((pixelArray[pixelIndex + 2] >> 6) << 4);
                  byte highGreen = (byte) ((pixelArray[pixelIndex + 1] >> 6) << 2);
                  byte highBlue = (byte) (pixelArray[pixelIndex] >> 6);

                  colorRepresentation |= highRed;
                  colorRepresentation |= highGreen;
                  colorRepresentation |= highBlue;

                  bins[colorRepresentation]++;
              }
          }

          return normalize ? NormalizeBins(bins) : bins;
      }

      private static double[] NormalizeBins( double[] bins )
      {
         double total = 0.0;
         for ( int i = 0; i < bins.Count(); i++ )
         {
            total += bins[i];
         }

         var normalizedBins = new double[bins.Count()];
         for ( int i = 0; i < bins.Count(); i++ )
         {
            normalizedBins[i] = Math.Round( ( bins[i] / total ) * 100.0, 2 );
         }

         return normalizedBins;
      }

      public static List<Rect> FindFaces( WriteableBitmap bitmap )
      {
         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         int bitmapWidth = bitmap.PixelWidth;
         int bitmapHeight = bitmap.PixelHeight;

         var faceLock = new Object();
         var faces = new List<Rect>();
         for ( int width = bitmapWidth - 1; width > 15; width -= 15 )
         {
            for ( int height = bitmapHeight - 1; height > 15; height -= 15 )
            {
               Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapWidth - width, bitmapWidth / 45 ), column =>
               {
                  Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapHeight - height, bitmapHeight / 45 ), row =>
                  {
                     var croppedPixelArray = pixelArray.CropPixelArray( column, row, width, height, stride );
                     var colorBins = GetColorBins( croppedPixelArray, true );

                     var distance = CalculateBinDistance( colorBins, FaceTemplate );

                     if ( distance <= 36 /*distance threshold*/ )
                     {
                        var rect = new Rect( new Point( column, row ), new Size( width, height ) );
                        lock ( faceLock )
                        {
                           if ( !faces.Any( x => x.Contains( rect ) ) )
                           {
                              faces.Add( rect );                              
                           }
                        }
                     }
                  } );
               } );
            }
         }

         var significantFaces = faces.Select( x => MergeSignificantIntersections( x, faces ) ).Distinct().ToList();
         while ( true )
         {
            var oldCount = significantFaces.Count;
            significantFaces = significantFaces.Select( x => MergeSignificantIntersections( x, significantFaces ) ).Distinct().ToList();
            var newCount = significantFaces.Count;
            if ( oldCount == newCount )
            {
               break;
            }
         }

         Console.WriteLine( "Found {0} possible face(s)", faces.Count );
         Console.WriteLine( "Found {0} possible significant face(s)", significantFaces.Count );
         Console.WriteLine( "Press any key to continue..." );
         Console.ReadKey();

         return significantFaces;
      }

      /// <summary>
      /// Calculates the average distance between bins in two histograms
      /// </summary>
      /// <param name="firstBins">The first histogram</param>
      /// <param name="secondBins">The second histogram</param>
      /// <returns>The average distance between bins in the histograms</returns>
      public static double CalculateBinDistance( double[] firstBins, double[] secondBins )
      {
         Debug.Assert( firstBins.Count() == secondBins.Count() );

         var difference = 0.0;
         for ( int i = 0; i < firstBins.Count(); i++ )
         {
            if ( firstBins[i] == secondBins[i] )
            {
               continue;
            }
            difference += Math.Pow( firstBins[i] - secondBins[i], 2 ) / ( firstBins[i] + secondBins[i] );
         }

         return difference;
      }

      public static Rect MergeSignificantIntersections( Rect rect, List<Rect> otherRects )
      {
         foreach ( var otherRect in otherRects )
         {
            if ( rect.SignificantlyIntersects( otherRect ) )
            {
               rect.Union( otherRect );
            }
         }

         return rect;
      }
   }
}
