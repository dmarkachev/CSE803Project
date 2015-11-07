using System;
using System.IO;
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
      }
   }
}
