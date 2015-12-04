using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BitmapLibrary;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Color = System.Windows.Media.Color;

namespace FoodClassifier
{
   internal static class Program
   {
      public enum FoodType
      {
         Banana,
         Strawberry,
         Cookie,
         HotDog,
         Broccoli,
         FrenchFries,
         Egg
      }

       static string directory = "";
      private static void Main( string[] args )
      {
         bool success = false;
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

        // give this directory to the bitmap operations class
         BitmapOperations.saveDirectory = directory;

         // Scale the image up if it is too small or down if it is too big
         double scale = 1.0;
         if ( bitmap.PixelHeight < 400 && bitmap.PixelWidth < 400 )
         {
            scale = Math.Min( 400.0 / bitmap.PixelWidth, 400.0 / bitmap.PixelHeight );
         }
         else if ( bitmap.PixelHeight > 1000 && bitmap.PixelWidth > 1000 )
         {
            scale = Math.Min( 1000.0 / bitmap.PixelWidth, 1000.0 / bitmap.PixelHeight );
         }
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
         var cvImage = new Image<Gray, byte>( new Bitmap( args[0] ) );
         cvImage = cvImage.Resize( scale, INTER.CV_INTER_CUBIC );

         // var classifications = ClassifyBitmap( writeableBitmap, cvImage );

         BitmapOperations.analyzeBitmapGradient(bitmap);

      }

      private static List<bool> ClassifyBitmap( WriteableBitmap bitmap, Image<Gray, byte> cvImage )
      {
         // Let's pick 7 items to classify
         var classifications = new List<bool>
         {
            false, // banana 0
            false, // strawberry 1
            false, // cookie 2
            false, // hotdog 3
            false, // broccoli 4
            false, // french fries 5
            false  // egg 6
         };

         int stride = ( bitmap.PixelWidth*bitmap.Format.BitsPerPixel + 7 )/8;

         byte[] pixelArray = new byte[bitmap.PixelHeight*stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         int bitmapWidth = bitmap.PixelWidth;
         int bitmapHeight = bitmap.PixelHeight;

         // Lets limit the number of "distant" colors we classify
         var colorDistances = new List<double>
         {
            0, // banana 0
            0, // strawberry 1
            0, // cookie 2
            0, // hotdog 3
            0, // broccoli 4
            0, // french fries 5
            0  // egg 6
         };

         // Get the color distances
         Parallel.For( 0, classifications.Count(), i =>
         {
            colorDistances[i] = GetColorDistance( pixelArray, bitmapWidth, bitmapHeight, stride, ClassificationColorBins.FoodColors[i] );
            if ( colorDistances[i] < 50 )
            {
               // Always classify objects with this close a color
               classifications[i] = ClassifyByTexture( pixelArray ) && ClassifyWithSurf( (FoodType)i, cvImage );
            }
         } );

         // If we didn't have enough close color distances, classify some farther ones
         var closeDistanceCount = colorDistances.Count( x => x < 50 );
         if ( closeDistanceCount < 2 )
         {
            colorDistances.ForEach( x =>
            {
               if ( x < 50 )
               {
                  x = double.PositiveInfinity;
               }
            });

            // Classify the closest color
            var minValue = colorDistances.Min();
            if ( minValue < 105 )
            {
               var indexOfMinValue = colorDistances.IndexOf( minValue );
               classifications[indexOfMinValue] = ClassifyByTexture( pixelArray ) && ClassifyWithSurf( (FoodType)indexOfMinValue, cvImage );
            }

            if ( closeDistanceCount == 0 )
            {
               // Classify the second closest color
               var secondMin = double.PositiveInfinity;
               foreach ( var colorDistance in colorDistances )
               {
                  if ( colorDistance != minValue && colorDistance < secondMin )
                  {
                     secondMin = colorDistance;
                  }
               }
               if ( secondMin < 105 )
               {
                  var indexOfSecondValue = colorDistances.IndexOf( secondMin );
                  classifications[indexOfSecondValue] = ClassifyByTexture( pixelArray ) && ClassifyWithSurf( (FoodType)indexOfSecondValue, cvImage );
               }
            }          
         }
         
         for ( int i = 0; i < classifications.Count(); i++ )
         {
            if ( classifications[i] )
            {
                  MessageBox.Show( ( (FoodType)i ).ToString() );
            }
         }

         return classifications;
      }

      /// <summary>
      /// Returns the distance from the target color histogram the given pixel array is
      /// </summary>
      /// <param name="pixelArray">The pixel array of the bitmap to get the distance of</param>
      /// <param name="bitmapWidth">The width of the bitmap</param>
      /// <param name="bitmapHeight">The height of the bitmap</param>
      /// <param name="stride">The number of bytes per row</param>
      /// <param name="targetColor">The histogram of the color of the target object</param>
      /// <returns>The minimum distance a subrect of the image is from the target histogram</returns>
      private static double GetColorDistance( byte[] pixelArray, int bitmapWidth, int bitmapHeight, int stride, double[] targetColor )
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

               // Possible areas where the object we are looking for is are black 0
               byte newColor = distance >= 125 ? (byte)255 : (byte)0;
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

         double minDistance = double.PositiveInfinity;
         var allBlobDistance = GetColorBinDistance(pixelArray, colorDistancePixelArray, stride, targetColor, tempBitmap, Colors.Black);
         if ( allBlobDistance <= 105 )
         {
            minDistance = allBlobDistance;
         }

         var blobColors = BitmapColorer.ColorBitmap( tempBitmap );
         foreach ( var color in blobColors )
         {
            var distance = GetColorBinDistance( pixelArray, colorDistancePixelArray, stride, targetColor, tempBitmap, color );
            if ( distance <= minDistance )
            {
               minDistance = distance;
            }
         }
         return minDistance;
      }

      private static double GetColorBinDistance(byte[] pixelArray, byte[] thresholdedPixelArray, int stride, double[] targetHistogram, WriteableBitmap tempBitmap, Color color)
      {
         var boundingBox = BitmapColorer.GetBoundingBoxOfColor( tempBitmap, color );

         var croppedPixelArray = pixelArray.CropPixelArray( (int)boundingBox.X, (int)boundingBox.Y, (int)boundingBox.Width, (int)boundingBox.Height, stride );
         var croppedThresholdedArray = thresholdedPixelArray.CropPixelArray((int)boundingBox.X, (int)boundingBox.Y, (int)boundingBox.Width, (int)boundingBox.Height, stride);
         var colorBins = ColorClassifier.GetColorBinsWithinBlob(croppedPixelArray, croppedThresholdedArray, true);
         var distance = ColorClassifier.CalculateBinDistance( colorBins, targetHistogram );
         return distance;
      }

      private static bool ClassifyByTexture( byte[] pixelArray )
      {
         return true;
      }

      private static bool ClassifyWithSurf( FoodType foodType, Image<Gray, byte> cvImage )
      {
         switch ( foodType )
         {
            case FoodType.Banana:
               return cvImage.HasBananaStem() && cvImage.HasBananaFlesh() && cvImage.HasLongBananaStem();
            case FoodType.Strawberry:
               return cvImage.HasStrawberrySeeds() && cvImage.HasStrawberryLeaves();
            case FoodType.Cookie:
               return cvImage.HasCookieChips();
            case FoodType.HotDog:
               return cvImage.HasSausageBetweenBuns() && ( cvImage.HasSausage() || cvImage.HasSausageWithToppings() );
            case FoodType.Broccoli:
               return cvImage.HasBroccoliTop();
            case FoodType.FrenchFries:
               return cvImage.HasFrenchFryParts();
            case FoodType.Egg:
               return cvImage.HasEggYolk();
            default:
               return false;
         }
      }
   }
}
