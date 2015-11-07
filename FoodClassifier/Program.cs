using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
         resizedBitmap.DecodePixelHeight = (int)(scale * bitmap.PixelHeight);
         resizedBitmap.DecodePixelWidth = (int)(scale * bitmap.PixelWidth);
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
            false, // food 1
            false, // food 2
            false, // food 3
            false, // food 4
            false, // food 5
            false, // food 6
            false  // food 7
         };

         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         int bitmapWidth = bitmap.PixelWidth;
         int bitmapHeight = bitmap.PixelHeight;

         // Moving window to brute force search the image
         // May need to adjust increments to increase accuracy
         var classificationLock = new object();
         for ( int width = bitmapWidth - 1; width > 15; width -= 15 )
         {
            for ( int height = bitmapHeight - 1; height > 15; height -= 15 )
            {
               Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapWidth - width, bitmapWidth / 45 ), column =>
               {
                  Parallel.ForEach( ExtensionMethods.SteppedRange( 0, bitmapHeight - height, bitmapHeight / 45 ), row =>
                  {
                     for ( int i = 0; i < classifications.Count(); i++ )
                     {
                        // If we have not already identified this image as that object
                        // see if we can classify it with this window
                        if ( !classifications[i] )
                        {
                           var croppedPixelArray = pixelArray.CropPixelArray( column, row, width, height, stride );
                           bool classification = WeakClassifier( croppedPixelArray ) &&
                                                 MediumClassifier( croppedPixelArray ) &&
                                                 StrongClassifier( croppedPixelArray );
                           if ( classification )
                           {
                              lock ( classificationLock )
                              {
                                 classifications[i] = true;
                              }
                           }
                        }
                     }
                  } );
               } );
            }
         }
         return classifications;
      }

      private static bool WeakClassifier( byte[] pixelArray )
      {
         return false;
      }

      private static bool MediumClassifier( byte[] pixelArray )
      {
         return false;
      }

      private static bool StrongClassifier( byte[] pixelArray )
      {
         return false;
      }
   }
}
