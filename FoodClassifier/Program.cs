using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BitmapLibrary;

namespace FoodClassifier
{
   internal static class Program
   {
      private static void Main( string[] args )
      {
         bool success = false;
         string directory = "";
         string fileName = "";
         if ( args.Length >= 1 )
         {
            directory = Path.GetDirectoryName( args[0] );
            fileName = Path.GetFileNameWithoutExtension( args[0] );

            success = !string.IsNullOrEmpty( fileName );
         }

         if ( !success )
         {
            Console.WriteLine( "Could not parse arguments: FoodClassifier.exe <filepath>" );
            Console.ReadKey();
            return;
         }
         if ( string.IsNullOrEmpty( directory ) )
         {
            directory = "";
         }

         var bitmap = new BitmapImage( new Uri( args[0], string.IsNullOrEmpty( directory ) ? UriKind.Relative : UriKind.Absolute ) );

         // Resize to fit in 400x400 box for faster processing
         double scale = Math.Min( 400.0/bitmap.PixelWidth, 400.0/bitmap.PixelHeight );
         var resizedBitmap = new BitmapImage();
         resizedBitmap.BeginInit();
         resizedBitmap.UriSource = bitmap.UriSource;
         resizedBitmap.DecodePixelHeight = (int)( scale * bitmap.PixelHeight );
         resizedBitmap.DecodePixelWidth = (int)( scale * bitmap.PixelWidth );
         resizedBitmap.EndInit();

         // Reformat to BGR
         var properFormatBitmap = new FormatConvertedBitmap();
         properFormatBitmap.BeginInit();
         properFormatBitmap.Source = resizedBitmap;
         properFormatBitmap.DestinationFormat = PixelFormats.Bgr32;
         properFormatBitmap.EndInit();

         var writeableBitmap = new WriteableBitmap( properFormatBitmap ); // The ready to go bitmap

         var classifications = ClassifyBitmap( writeableBitmap );
      }

      private static List<bool> ClassifyBitmap( WriteableBitmap bitmap )
      {
         // Let's pick 7 items to classify
         var classifications = new List<bool>
         {
            false, // banana
            false, // strawberry
            false, // cookie
            false, // hotdog
            false, // broccoli
            false, // french fries
            false  // egg
         };

         int stride = ( bitmap.PixelWidth*bitmap.Format.BitsPerPixel + 7 )/8;

         byte[] pixelArray = new byte[bitmap.PixelHeight*stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         int bitmapWidth = bitmap.PixelWidth;
         int bitmapHeight = bitmap.PixelHeight;

         // Moving window to brute force search the image
         // May need to adjust increments to increase accuracy
         var classificationLock = new object();
         Parallel.For( 0, classifications.Count(), i =>
         {
            // If we have not already identified this image as that object
            // see if we can classify it with this window
            if ( !classifications[i] )
            {
               bool classification = ClassifyByColor( pixelArray, bitmapWidth, bitmapHeight, stride, ClassificationColorBins.FoodColors[i] ) &&
                                     ClassifyByTexture( pixelArray ) &&
                                     StrongClassifier( pixelArray );
               if ( classification )
               {
                  lock ( classificationLock )
                  {
                     classifications[i] = true;
                  }
               }
            }
         } );

         return classifications;
      }

      /// <summary>
      /// The weak classify that classifies by comparing the color histogram
      /// of the given bitmap and returns true if it is close enough
      /// to the target histogram
      /// </summary>
      /// <param name="pixelArray">The pixel array of the bitmap to classify</param>
      /// <param name="bitmapWidth">The width of the bitmap</param>
      /// <param name="bitmapHeight">The height of the bitmap</param>
      /// <param name="stride">The number of bytes per row</param>
      /// <param name="targetColor">The histogram of the color of the target object</param>
      /// <returns>True if this classifier thinks the bitmap is the object</returns>
      private static bool ClassifyByColor( byte[] pixelArray, int bitmapWidth, int bitmapHeight, int stride, double[] targetColor )
      {
         var colorDistancePixelArray = (byte[])pixelArray.Clone();
         var pixelArrayLock = new object();

         // Threshold the image for possible areas where the object is
         Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapWidth, 8 ), column =>
         {
            Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapHeight, 8 ), row =>
            {
               int width = Math.Min( 8, bitmapWidth - column );
               int height = Math.Min( 8, bitmapHeight - row );
               var croppedPixelArray = pixelArray.CropPixelArray( column, row, width, height, stride );
               var colorBins = ColorClassifier.GetColorBins( croppedPixelArray, true );
               var distance = ColorClassifier.CalculateBinDistance( colorBins, targetColor );

               // Possible areas where the object we are looking for is are black
               byte newColor = distance >= 120 ? (byte)255 : (byte)0;
               for ( int i = 0; i < width; i++ )
               {
                  for ( int j = 0; j < height; j++ )
                  {
                     int index = ( row + j )*stride + 4*( column + i );
                     lock ( pixelArrayLock )
                     {
                        colorDistancePixelArray[index] = colorDistancePixelArray[index + 1] = colorDistancePixelArray[index + 2] = newColor;
                     }
                  }
               }
            } );
         } );

         // Look at each blob and determine if it is our
         // object with a stricted threshold
         var tempBitmap = new WriteableBitmap( bitmapWidth, bitmapHeight, 96, 96, PixelFormats.Bgr32, null );
         tempBitmap.WritePixels( new Int32Rect( 0, 0, bitmapWidth, bitmapHeight ), colorDistancePixelArray, stride, 0 );
         var blobColors = BitmapColorer.ColorBitmap( tempBitmap );
         foreach ( var color in blobColors )
         {
            var boundingBox = BitmapColorer.GetBoundingBoxOfColor( tempBitmap, color );
            var croppedPixelArray = pixelArray.CropPixelArray( (int)boundingBox.X, (int)boundingBox.Y, (int)boundingBox.Width, (int)boundingBox.Height, stride );
            var colorBins = ColorClassifier.GetColorBins( croppedPixelArray, true );
            var distance = ColorClassifier.CalculateBinDistance( colorBins, targetColor );
            if ( distance <= 60 )
            {
               return true;
            }
         }
         return false;
      }

      private static bool ClassifyByTexture( byte[] pixelArray )
      {
         return false;
      }

      private static bool StrongClassifier( byte[] pixelArray )
      {
         return false;
      }
   }
}
