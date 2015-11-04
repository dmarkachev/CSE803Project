using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BitmapLibrary
{
   public class ImageBlobInfo
   {
      /// <summary>
      /// For a given color, gives information about a blob within the given bitmap that is that color
      /// </summary>
      /// <param name="bitmap">The bitmap containing the blob</param>
      /// <param name="color">The color of the blob</param>
      /// <returns>An ImageBlobInfo object containing information of the blob of the given color</returns>
      public ImageBlobInfo( WriteableBitmap bitmap, Color color )
      {
         Color = color;
         Area = 0;
         Centroid = new Point( 0, 0 );
         SecondRowMoment = 0;
         SecondMixedMoment = 0;
         SecondColumnMoment = 0;
         MaxInertia = 0;
         MinInertia = 0;
         Circularity = 0;
         Perimeter = 0;

         int stride = ( bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7 ) / 8;
         int height = bitmap.PixelHeight;
         int width = bitmap.PixelWidth;

         byte[] pixelArray = new byte[bitmap.PixelHeight * stride];
         bitmap.CopyPixels( pixelArray, stride, 0 );

         // Calculate Area and Centroid
         double r = 0;
         double c = 0;
         for ( int row = 0; row < height; row++ )
         {
            for ( int column = 0; column < width; column++ )
            {
               int index = row * stride + 4 * column;
               var pixel = Pixel.GetPixel( pixelArray, index );
               if ( pixel.Color == color )
               {
                  Area++;

                  r += row; // add the row number
                  c += column; // add the column number
               }
            }
         }
         Centroid = new Point( c / Area, r / Area );

         // Calculate Second-Order Moments
         for ( int row = 0; row < height; row++ )
         {
            for ( int column = 0; column < width; column++ )
            {
               int index = row * stride + 4 * column;
               var pixel = Pixel.GetPixel( pixelArray, index );
               if ( pixel.Color == color )
               {
                  SecondRowMoment += Math.Pow( row - Centroid.Y, 2 );
                  SecondMixedMoment += ( row - Centroid.Y ) * ( column - Centroid.X );
                  SecondColumnMoment += Math.Pow( column - Centroid.X, 2 );
               }
            }
         }
         SecondRowMoment /= Area;
         SecondMixedMoment /= Area;
         SecondColumnMoment /= Area;

         // Calculate Inertia
         var theta = ( 2 * SecondMixedMoment ) / ( SecondColumnMoment - SecondRowMoment );
         theta = Math.Atan( theta );
         theta /= 2;

         var otherTheta = theta + Math.PI / 2;

         var inertiaOne = Math.Pow( Math.Sin( theta ), 2 ) * SecondColumnMoment +
                          Math.Sin( theta ) * Math.Cos( theta ) * SecondMixedMoment +
                          Math.Pow( Math.Cos( theta ), 2 ) * SecondRowMoment;

         var inertiaTwo = Math.Pow( Math.Sin( otherTheta ), 2 ) * SecondColumnMoment +
                          Math.Sin( otherTheta ) * Math.Cos( otherTheta ) * SecondMixedMoment +
                          Math.Pow( Math.Cos( otherTheta ), 2 ) * SecondRowMoment;

         MaxInertia = Math.Max( inertiaOne, inertiaTwo );
         MinInertia = Math.Min( inertiaOne, inertiaTwo );

         // Calculate Circulator
         double mean = 0;
         for ( int row = 0; row < height; row++ )
         {
            for ( int column = 0; column < width; column++ )
            {
               int index = row * stride + 4 * column;
               var pixel = Pixel.GetPixel( pixelArray, index );
               if ( pixel.Color == color )
               {
                  mean += Math.Sqrt( Math.Pow( column - Centroid.X, 2 ) + Math.Pow( row - Centroid.Y, 2 ) );
               }
            }
         }
         mean /= Area;

         double standardDeviation = 0;
         for ( int row = 0; row < height; row++ )
         {
            for ( int column = 0; column < width; column++ )
            {
               int index = row * stride + 4 * column;
               var pixel = Pixel.GetPixel( pixelArray, index );
               if ( pixel.Color == color )
               {
                  var value = Math.Sqrt( Math.Pow( column - Centroid.X, 2 ) + Math.Pow( row - Centroid.Y, 2 ) );
                  value -= mean;
                  value = Math.Pow( value, 2 );
                  standardDeviation += value;
               }
            }
         }
         standardDeviation /= Area;
         standardDeviation = Math.Sqrt( standardDeviation );

         Circularity = mean / standardDeviation;

         // Calculate Perimeter
         var filteredBitmap = BitmapColorer.EraseAllButCertainColor( bitmap, color );
         var perimeterBitmap = BitmapOperations.GetPerimeterBitmap( filteredBitmap );

         byte[] perimeterPixelArray = new byte[perimeterBitmap.PixelHeight * stride];
         perimeterBitmap.CopyPixels( perimeterPixelArray, stride, 0 );

         var perimeterPixelList = BitmapOperations.BuildPerimeterPath( perimeterPixelArray, bitmap.PixelHeight, bitmap.PixelWidth, stride );
         for ( int i = 0; i < perimeterPixelList.Count; i++ )
         {
            int comparisonIndex = i + 1;
            if ( i == perimeterPixelList.Count - 1 )
            {
               comparisonIndex = 0;
            }

            // If the current pixel and the next pixel are vertical or horizontal neighbors
            if ( perimeterPixelList[i] == perimeterPixelList[comparisonIndex] + stride ||
                 perimeterPixelList[i] == perimeterPixelList[comparisonIndex] - stride ||
                 perimeterPixelList[i] == perimeterPixelList[comparisonIndex] - 4 ||
                 perimeterPixelList[i] == perimeterPixelList[comparisonIndex] + 4 )
            {
               Perimeter++;
            }
            // If the current pixel and the next pixel are diagonal neighbors
            else if ( perimeterPixelList[i] == perimeterPixelList[comparisonIndex] + stride + 4 ||
                      perimeterPixelList[i] == perimeterPixelList[comparisonIndex] + stride - 4 ||
                      perimeterPixelList[i] == perimeterPixelList[comparisonIndex] - stride + 4 ||
                      perimeterPixelList[i] == perimeterPixelList[comparisonIndex] - stride - 4 )
            {
               Perimeter += 1.4;
            }
         }
      }

      public Color Color { get; set; }
      public int Area { get; set; }
      public Point Centroid { get; set; }
      public double SecondRowMoment { get; set; }
      public double SecondMixedMoment { get; set; }
      public double SecondColumnMoment { get; set; }
      public double MaxInertia { get; set; }
      public double MinInertia { get; set; }
      public double Circularity { get; set; }
      public double Perimeter { get; set; }

      public void SaveInfo( StreamWriter sw )
      {
         sw.WriteLine( "Blob Color: ({0}, {1}, {2})", Color.R, Color.G, Color.B );
         sw.WriteLine( "Area: {0} px", Area );
         sw.WriteLine( "Centroid: ({0}, {1})", Math.Round( Centroid.X, 1 ), Math.Round( Centroid.Y, 1 ) );
         sw.WriteLine( "Second-Order Row Moment: {0}", Math.Round( SecondRowMoment, 1 ) );
         sw.WriteLine( "Second-Order Mixed Moment: {0}", Math.Round( SecondMixedMoment, 1 ) );
         sw.WriteLine( "Second-order Column Moment: {0}", Math.Round( SecondColumnMoment, 1 ) );
         sw.WriteLine( "Max Inertia: {0}", Math.Round( MaxInertia, 1 ) );
         sw.WriteLine( "Min Inertia: {0}", Math.Round( MinInertia, 1 ) );
         sw.WriteLine( "Circularity: {0}", Math.Round( Circularity, 1 ) );
         sw.WriteLine( "Perimeter/Circumference: {0} px", Math.Round( Perimeter, 1 ) );
         sw.WriteLine( "" );
      }
   }
}
