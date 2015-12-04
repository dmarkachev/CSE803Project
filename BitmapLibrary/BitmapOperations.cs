using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Color = System.Drawing.Color;

namespace BitmapLibrary
{
   
   public static class BitmapOperations
   {
       public static string saveDirectory = "";
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
      /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
      /// </summary>
      /// <param name="image">The Emgu CV Image</param>
      /// <returns>The equivalent BitmapSource</returns>
      private static BitmapSource ToBitmapSource(IImage image)
      {
          using (System.Drawing.Bitmap source = image.Bitmap)
          {
              IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

              BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

              // DeleteObject(ptr); //release the HBitmap
              return bs;
          }
      }

      private static System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
      {
          System.Drawing.Bitmap bmp;
          using (MemoryStream outStream = new MemoryStream())
          {
              BitmapEncoder enc = new BmpBitmapEncoder();
              enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
              enc.Save(outStream);
              bmp = new System.Drawing.Bitmap(outStream);
          }
          return bmp;
      }

      public static double ConvertToRadians(double angle)
      {
          return (Math.PI / 180) * angle;
      }
      public static double RadianToDegree(double angle)
      {
          return angle * (180.0 / Math.PI);
      }

       public static double CalculateBlobAngle(CvBlob blob)
       {
           //.5*atan2(2.*blob->u11,(blob->u20-blob->u02));

           double dy = 2.0*blob.BlobMoments.U11;
           double dx = blob.BlobMoments.U20 - blob.BlobMoments.U02;

           double result = 0.5*Math.Atan2(dy, dx);

           return result;
       }

       public static WriteableBitmap RotateWriteableBitmap(WriteableBitmap writeableBitmap, double degrees)
       {
           Bitmap normalBitmap = BitmapFromWriteableBitmap(writeableBitmap);
           var cvImage = new Image<Gray, byte>(new Bitmap(normalBitmap));
           cvImage = cvImage.Rotate(degrees, new Gray(0), false);
           BitmapSource bitmapSource = ToBitmapSource(cvImage);
           var rotatedWriteableBitmap = new WriteableBitmap(bitmapSource);

           return rotatedWriteableBitmap;
       }

      public static WriteableBitmap DrawBlobBoundingBoxsCV(WriteableBitmap writeableBitmap)
       {
           Bitmap normalBitmap = BitmapFromWriteableBitmap(writeableBitmap);
           var cvImage = new Image<Gray, byte>(new Bitmap(normalBitmap));

           //var classifications = ClassifyBitmap( writeableBitmap, cvImage );
           if (cvImage != null)
           {
               // This takes our nice looking color png and converts it to black and white
               Image<Gray, byte> greyImg = cvImage.Convert<Gray, byte>();

               // We again threshold it based on brightness...BUT WE INVERT THE PNG. BLOB DETECTOR DETECTS WHITE NOT BLACK
               // this will esentially eliminate the color differences
               // you could also do cool things like threshold only certain colors here for a color based blob detector
               Image<Gray, Byte> greyThreshImg = greyImg.ThresholdBinaryInv(new Gray(150), new Gray(255));

               Emgu.CV.Cvb.CvBlobs resultingImgBlobs = new Emgu.CV.Cvb.CvBlobs();
               Emgu.CV.Cvb.CvBlobDetector bDetect = new Emgu.CV.Cvb.CvBlobDetector();
               uint numWebcamBlobsFound = bDetect.Detect(greyThreshImg, resultingImgBlobs);

               // This is a simple way of just drawing all blobs reguardless of their size and not iterating through them
               // It draws on top of whatever you input. I am inputting the threshold image. Specifying an alpha to draw with of 0.5 so its half transparent.
               //Emgu.CV.Image<Bgr, byte> blobImg = bDetect.DrawBlobs(webcamThreshImg, resultingWebcamBlobs, Emgu.CV.Cvb.CvBlobDetector.BlobRenderType.Default, 0.5);

               // Here we can iterate through each blob and use the slider to set a threshold then draw a red box around it
               Image<Rgb, byte> blobImg = greyThreshImg.Convert<Rgb, byte>();
               Rgb red = new Rgb(255, 0, 0);

               int blobNumber = 0;

               // Lets try and iterate the blobs?
               foreach (Emgu.CV.Cvb.CvBlob targetBlob in resultingImgBlobs.Values)
               {
                   int imageArea = blobImg.Width*blobImg.Height;
                   int blobArea = targetBlob.Area;
                   int blobBoundingBoxArea = targetBlob.BoundingBox.Width*targetBlob.BoundingBox.Height;

                   // Only use blobs with area greater than some threshold
                   // If the blob bounding rect is basically size of the whole image ignore it for now
                   if (blobArea > 200.0 && blobBoundingBoxArea < (imageArea*0.99))
                   {                       
                       Rectangle rectangle = targetBlob.BoundingBox;

                       int CentroidX = (int)targetBlob.Centroid.X;
                       int CentroidY = (int)targetBlob.Centroid.Y;

                       int croppedWidth = rectangle.Width + 50;
                       int croppedHeight = rectangle.Height + 50;

                       int CroppedX = CentroidX - (int)(croppedWidth / 2.0);
                       int CroppedY = CentroidY - (int)(croppedHeight / 2.0);

                       var croppedBlobBitmap = writeableBitmap.Crop(CroppedX, CroppedY, croppedWidth, croppedHeight);

                       double blobAngle = -RadianToDegree(CalculateBlobAngle(targetBlob));

                       CroppedX = (int) (croppedWidth/2.0);
                       CroppedY = (int) (croppedHeight/2.0);

                       var rotatedandCroppedBlobBitmap = RotateWriteableBitmap(croppedBlobBitmap, blobAngle);

                       var refinedBitmap = DrawBlobBoundingBoxsAroundCroppedBitmap(rotatedandCroppedBlobBitmap);
                       var areaCheck = refinedBitmap.PixelHeight*refinedBitmap.PixelWidth;

                       if (areaCheck >= 200)
                       {
                           blobNumber++;

                           var thresholded = refinedBitmap.Clone();
                           ThresholdBitmap(thresholded, 10, false);
                           BitmapColorer.ColorBitmap(thresholded);

                           System.Windows.Media.Color blobColor = PixelColorOfCentralBlob(thresholded);
                           BitmapColorer.EraseAllButCertainColorandWhite(thresholded, blobColor);

                           var orientedBitmap = FlipThresholdBitmapIfNecessary(thresholded, blobColor);
                           string fileName1 = saveDirectory + "\\croppedBlob" + blobNumber + ".png";

                           ExtensionMethods.Save(orientedBitmap, fileName1);
                       }
                   }
               }          
           }

           return writeableBitmap;
       }

       
      public static WriteableBitmap DrawBlobBoundingBoxsAroundCroppedBitmap(WriteableBitmap writeableBitmap)
       {
           Bitmap normalBitmap = BitmapFromWriteableBitmap(writeableBitmap);
           var cvImage = new Image<Gray, byte>(new Bitmap(normalBitmap));

           if (cvImage != null)
           {
               Image<Gray, byte> greyImg = cvImage.Convert<Gray, byte>();

               Image<Gray, Byte> greyThreshImg = greyImg.ThresholdBinaryInv(new Gray(150), new Gray(255));

               Emgu.CV.Cvb.CvBlobs resultingImgBlobs = new Emgu.CV.Cvb.CvBlobs();
               Emgu.CV.Cvb.CvBlobDetector bDetect = new Emgu.CV.Cvb.CvBlobDetector();
               uint numWebcamBlobsFound = bDetect.Detect(greyThreshImg, resultingImgBlobs);

               Image<Rgb, byte> blobImg = greyThreshImg.Convert<Rgb, byte>();
               Rgb red = new Rgb(255, 0, 0);

               int blobNumber = 0;

               foreach (Emgu.CV.Cvb.CvBlob targetBlob in resultingImgBlobs.Values)
               {
                   int imageArea = blobImg.Width*blobImg.Height;
                   int blobArea = targetBlob.Area;
                   int blobBoundingBoxArea = targetBlob.BoundingBox.Width*targetBlob.BoundingBox.Height;

                   if (blobArea > 200.0 && blobBoundingBoxArea < (imageArea*0.99))
                   {
                       blobNumber++;
                       Rectangle rectangle = targetBlob.BoundingBox;
                       Rect convertedRect = new Rect(rectangle.X - 10, rectangle.Y - 10, rectangle.Width + 20, rectangle.Height + 20);
                       //BitmapColorer.DrawRectangle(writeableBitmap, convertedRect);

                       writeableBitmap = writeableBitmap.Crop(rectangle.X - 10, rectangle.Y - 10, rectangle.Width + 20, rectangle.Height + 20);
                   }
               }           
           }

           return writeableBitmap;
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

       public static WriteableBitmap ResizeBitmap(BitmapImage bitmap)
       {
           // Resize to fit in 400x400 box for faster processing
           double scale = Math.Min(400.0/bitmap.PixelWidth, 400.0/bitmap.PixelHeight);
           var resizedBitmap = new BitmapImage();
           resizedBitmap.BeginInit();
           resizedBitmap.UriSource = bitmap.UriSource;
           resizedBitmap.DecodePixelHeight = (int) (scale*bitmap.PixelHeight);
           resizedBitmap.DecodePixelWidth = (int) (scale*bitmap.PixelWidth);
           resizedBitmap.EndInit();

           // Reformat to BGR
           var properFormatBitmap = new FormatConvertedBitmap();
           properFormatBitmap.BeginInit();
           properFormatBitmap.Source = resizedBitmap;
           properFormatBitmap.DestinationFormat = PixelFormats.Bgr32;
           properFormatBitmap.EndInit();

           var writeableBitmap = new WriteableBitmap(properFormatBitmap); // The ready to go bitmap

           return writeableBitmap;
       }

       public static void analyzeBitmapGradient(BitmapImage bitmapImage)
       {
           var resizedWritableBitmap = BitmapOperations.ResizeBitmap(bitmapImage);

           BitmapOperations.DrawGradientScaleBitmap(resizedWritableBitmap);

           resizedWritableBitmap = BitmapOperations.DrawBlobBoundingBoxsCV(resizedWritableBitmap);

           string fileName1 = saveDirectory + "\\outputImage.png";
           ExtensionMethods.Save(resizedWritableBitmap, fileName1);
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
      public static void CropThresholdBitmap(WriteableBitmap bitmap, int thickness, bool whiteCropBorder)
      {
          int objectValue = whiteCropBorder ? 255 : 0;
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          bitmap.CopyPixels(pixelByteArray, stride, 0);

          for (int column = 0; column < bitmap.PixelWidth; column++)
          {
              for (int row = 0; row < bitmap.PixelHeight; row++)
              {
                  int index = row * stride + 4 * column;
                  var whiteBlack = Convert.ToByte(objectValue);

                  if (row >= 0 && row <= thickness)
                  {
                      pixelByteArray[index] = whiteBlack;
                      pixelByteArray[index + 1] = whiteBlack;
                      pixelByteArray[index + 2] = whiteBlack;
                  }

                  if (row >= (bitmap.PixelHeight - thickness) && row <= bitmap.PixelHeight)
                  {
                      pixelByteArray[index] = whiteBlack;
                      pixelByteArray[index + 1] = whiteBlack;
                      pixelByteArray[index + 2] = whiteBlack;
                  }

                  if (column >= 0 && column <= thickness)
                  {
                      pixelByteArray[index] = whiteBlack;
                      pixelByteArray[index + 1] = whiteBlack;
                      pixelByteArray[index + 2] = whiteBlack;
                  }

                  if (column >= (bitmap.PixelWidth - thickness) && column <= bitmap.PixelWidth)
                  {
                      pixelByteArray[index] = whiteBlack;
                      pixelByteArray[index + 1] = whiteBlack;
                      pixelByteArray[index + 2] = whiteBlack;
                  }
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
      }

      public static WriteableBitmap FlipThresholdBitmapIfNecessary(WriteableBitmap bitmap, System.Windows.Media.Color blobColor)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          int topCount = 0;
          int bottomCount = 0;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          bitmap.CopyPixels(pixelByteArray, stride, 0);

          for (int column = 0; column < bitmap.PixelWidth; column++)
          {
              for (int row = 0; row < bitmap.PixelHeight; row++)
              {
                  int index = row * stride + 4 * column;

                  var pixelColor = new System.Windows.Media.Color();

                  pixelColor = System.Windows.Media.Color.FromRgb(pixelByteArray[index + 2],
                      pixelByteArray[index + 1], pixelByteArray[index]);

                  if (pixelColor == blobColor)
                  {
                      if (row < bitmap.PixelHeight/2.0)
                      {
                          topCount++;
                      }
                      else
                      {
                          bottomCount++;
                      }                      
                  }
              }
          }

          if (topCount > bottomCount)
          {
             return bitmap.Rotate(180);
          }
          else
          {
              return bitmap;
          }
      }


      public static System.Windows.Media.Color PixelColorOfCentralBlob(WriteableBitmap bitmap)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          ConcurrentDictionary<System.Windows.Media.Color, int > colorDict = 
              new ConcurrentDictionary<System.Windows.Media.Color, int>();

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          bitmap.CopyPixels(pixelByteArray, stride, 0);

          //Draw a Horizontal Line accross the middle
          for (int column = 0; column < bitmap.PixelWidth; column++)
          {
                  var color = new System.Windows.Media.Color();

                  int row = (int)(bitmap.PixelHeight/2.0);
                  int index = row * stride + 4 * column;

                  int R = Convert.ToInt32(pixelByteArray[index+2]);
                  int G = Convert.ToInt32(pixelByteArray[index+1]);
                  int B = Convert.ToInt32(pixelByteArray[index]);

                  color = System.Windows.Media.Color.FromRgb(pixelByteArray[index + 2],
                      pixelByteArray[index + 1], pixelByteArray[index]);

              if (!(R == 255 && B == 255 && G == 255) && !(R== 0 && B == 0 && G == 0))
              {
                  colorDict.AddOrUpdate(color, 1, (id, count) => count + 1);  
              }
          }

          int maxValue = 0;
          var colorKey  = new System.Windows.Media.Color();
          foreach (var item in colorDict)
          {
              if (item.Value > maxValue)
              {
                  colorKey = item.Key;
                  maxValue = item.Value;
              }
          }
          return colorKey;
      }



       public static void DrawGradientScaleBitmap(WriteableBitmap writeableBitmap)
       {
           BitmapOperations.GrayscaleBitmap(writeableBitmap);
           BitmapOperations.GradientScaleBitmap(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap2(writeableBitmap);
           BitmapOperations.GradientScaleBitmap3(writeableBitmap);
           BitmapOperations.ThresholdBitmap(writeableBitmap, 10, false);
           BitmapOperations.CropThresholdBitmap(writeableBitmap, 5, false);
       }

      /// <summary>
      /// Loops through all the pixels and averages the RGB values to grayscale the image
      /// </summary>
      /// <param name="bitmap">The bitmap to be grayscaled</param>
      public static void GradientScaleBitmap(WriteableBitmap bitmap)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          byte[] backupPixelArray = new byte[bitmap.PixelHeight * stride];

          bitmap.CopyPixels(pixelByteArray, stride, 0);
          bitmap.CopyPixels(backupPixelArray, stride, 0);

          var referencePixel = new PixelWrapper(backupPixelArray);
          var pixel = new PixelWrapper(pixelByteArray);

          for (int column = 0; column < bitmap.PixelWidth; column += 1)
          {
              for (int row = 0; row < bitmap.PixelHeight; row += 1)
              {
                  int index = row * stride + 4 * column;

                  int topLeftPixelIndex = index - stride - 4;
                  int bottomLeftPixelIndex = index + stride - 4;
                  int topPixelIndex = index - stride;
                  int bottomPixelIndex = index + stride;
                  int topRightPixelIndex = index - stride + 4;
                  int bottomRightPixelIndex = index + stride + 4;
                  int leftPixelIndex = index - 4;
                  int rightPixelIndex = index + 4;

                  double DyL = (referencePixel.getGray(topLeftPixelIndex) - referencePixel.getGray(bottomLeftPixelIndex)) / 2.0;
                  double DyM = (referencePixel.getGray(topPixelIndex) - referencePixel.getGray(bottomPixelIndex)) / 2.0;
                  double DyR = (referencePixel.getGray(topRightPixelIndex) - referencePixel.getGray(bottomRightPixelIndex)) / 2.0;

                  double DxT = (referencePixel.getGray(topRightPixelIndex) - referencePixel.getGray(topLeftPixelIndex)) / 2.0;
                  double DxM = (referencePixel.getGray(rightPixelIndex) - referencePixel.getGray(leftPixelIndex)) / 2.0;
                  double DxB = (referencePixel.getGray(bottomRightPixelIndex) - referencePixel.getGray(bottomLeftPixelIndex)) / 2.0;

                  double Fy = (DyL + DyM + DyR) / 3.0;
                  double Fx = (DxT + DxM + DxB) / 3.0;

                  if (Fx >= 0 && Fx < 0.0001)
                  {
                      Fx = 0.0001;
                  }

                  if (Fy >= 0 && Fy < 0.0001)
                  {
                      Fy = 0.0001;
                  }

                  if (Fx < 0 && Fx > -0.0001)
                  {
                      Fx = -0.0001;
                  }

                  if (Fy < 0 && Fy > -0.0001)
                  {
                      Fy = -0.0001;
                  }

                  double gradientMagnitude = Math.Pow(Math.Pow(Fx, 2) + Math.Pow(Fy, 2), 0.5);

                  double Theta = Math.Atan2(Fy, Fx);
                  double angle = Theta * (180 / Math.PI) + 180;

                  int angleIntensity = Convert.ToInt32(angle);

                  int gradientIntensity = Convert.ToInt32(gradientMagnitude);

                  if (gradientIntensity > 255)
                      gradientIntensity = 255;

                  if (angle < 5 && gradientIntensity > 80)
                  {
                      //pixel.SetPixelColors(0, 255, 0, index);
                  }

                  angleIntensity = Convert.ToInt32((255.0 / 360.0) * angleIntensity);

                  if (gradientIntensity > 0)
                  {
                      pixel.SetPixelColors(gradientIntensity, angleIntensity, 255, index);
                  }
                  else
                  {
                      pixel.SetPixelColors(0, 0, 0, index);
                  }
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
      }


      public static void GradientScaleBitmap2(WriteableBitmap bitmap)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          byte[] backupPixelArray = new byte[bitmap.PixelHeight * stride];

          bitmap.CopyPixels(pixelByteArray, stride, 0);
          bitmap.CopyPixels(backupPixelArray, stride, 0);

          var referencePixel = new PixelWrapper(backupPixelArray);
          var pixel = new PixelWrapper(pixelByteArray);

          for (int column = 0; column < bitmap.PixelWidth; column += 1)
          {
              for (int row = 0; row < bitmap.PixelHeight; row += 1)
              {
                  int index = row * stride + 4 * column;

                  int topLeftPixelIndex = index - stride - 4;
                  int bottomLeftPixelIndex = index + stride - 4;
                  int topPixelIndex = index - stride;
                  int bottomPixelIndex = index + stride;
                  int topRightPixelIndex = index - stride + 4;
                  int bottomRightPixelIndex = index + stride + 4;
                  int leftPixelIndex = index - 4;
                  int rightPixelIndex = index + 4;

                  double A2 = 0;
                  double I2 = 0;
                  double dotProduct = 0;

                  double A1 = referencePixel.getGreen(index) * (360 / 255.0);
                  double I1 = referencePixel.getRed(index);
                  double totalMag = I1;

                  if (totalMag > 1)
                  {

                  }

                  A2 = referencePixel.getGreen(topLeftPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(topLeftPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  A2 = referencePixel.getGreen(bottomLeftPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(bottomLeftPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;
                  A2 = referencePixel.getGreen(topPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(topPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  A2 = referencePixel.getGreen(bottomPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(bottomPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;


                  A2 = referencePixel.getGreen(topRightPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(topRightPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  A2 = referencePixel.getGreen(bottomRightPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(bottomRightPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  A2 = referencePixel.getGreen(leftPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(leftPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  A2 = referencePixel.getGreen(rightPixelIndex) * (360 / 255.0);
                  I2 = referencePixel.getRed(rightPixelIndex);

                  dotProduct = I1 * I2 * Math.Cos((Math.PI / 180.0) * (A2 - A1));
                  totalMag += dotProduct;

                  if (totalMag < 0.1)
                      totalMag = 0;
                  else
                  {
                      totalMag = totalMag / 24;
                  }

                  if (totalMag > 255)
                  {
                      totalMag = 255;
                  }
                  if (totalMag < 0)
                  {
                      totalMag = 0;
                  }

                  int finalMag = Convert.ToInt32(totalMag);
                  int finalAngle = pixel.getGreen(index);
                  pixel.SetPixelColors(finalMag, finalAngle, 255, index);
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
      }

      public static void GradientScaleBitmap3(WriteableBitmap bitmap)
      {
          int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

          byte[] pixelByteArray = new byte[bitmap.PixelHeight * stride];
          byte[] backupPixelArray = new byte[bitmap.PixelHeight * stride];

          bitmap.CopyPixels(pixelByteArray, stride, 0);
          bitmap.CopyPixels(backupPixelArray, stride, 0);

          var referencePixel = new PixelWrapper(backupPixelArray);
          var pixel = new PixelWrapper(pixelByteArray);

          for (int column = 0; column < bitmap.PixelWidth; column += 1)
          {
              for (int row = 0; row < bitmap.PixelHeight; row += 1)
              {
                  int index = row * stride + 4 * column;

                  int finalIntensity = pixel.getRed(index);
                  pixel.SetPixelGreyColor(finalIntensity, index);
              }
          }

          bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixelByteArray, stride, 0);
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
