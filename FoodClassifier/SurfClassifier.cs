﻿using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace FoodClassifier
{
   public static class SurfClassifier
   {
      public static bool HasBananaStem( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Banana.StemStub1.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.StemStub2.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.StemStub3.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.StemStub4.jpg" ), observedImage );
         return found;
      }

      public static bool HasLongBananaStem( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Banana.Stem1.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.Stem2.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.Stem3.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.Stem4.jpg" ), observedImage );
         return found;
      }

      public static bool HasBananaFlesh( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Banana.FreshFlesh.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.RipeFlesh.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.VeryRipeFlesh.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.Internal.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.SuperFreshFlesh.jpg" ), observedImage );
         return found;
      }

      public static bool HasStrawberrySeeds( this Image<Gray, byte> observedImage )
      {
         //bool found = IsModelInObserved( GetImage( "Strawberry.Seed1.jpg" ), observedImage );
         return false;
      }

      private static Image<Gray, byte> GetImage( string resourceName )
      {
         var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "FoodClassifier.SurfImages." + resourceName );

         Debug.Assert( imageStream != null );
         return new Image<Gray, byte>( new Bitmap( imageStream ) );
      }

      private static bool IsModelInObserved( Image<Gray, byte> modelImage, Image<Gray, byte> observedImage )
      {
         var surfCpu = new SURFDetector(500, false);

         Matrix<byte> mask;
         int k = 2;
         double uniquenessThreshold = 0.8;

         //extract features from the object image
         var modelKeyPoints = surfCpu.DetectKeyPointsRaw( modelImage, null );
         Matrix<float> modelDescriptors = surfCpu.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

         // extract features from the observed image
         var observedKeyPoints = surfCpu.DetectKeyPointsRaw( observedImage, null );
         Matrix<float> observedDescriptors = surfCpu.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
         BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
         matcher.Add( modelDescriptors );

         var indices = new Matrix<int>( observedDescriptors.Rows, k );
         using ( var dist = new Matrix<float>( observedDescriptors.Rows, k ) )
         {
            matcher.KnnMatch( observedDescriptors, indices, dist, k, null );
            mask = new Matrix<byte>( dist.Rows, 1 );
            mask.SetValue( 255 );
            Features2DToolbox.VoteForUniqueness( dist, uniquenessThreshold, mask );
         }

         int keypointMatchCount = CvInvoke.cvCountNonZero( mask );
         if ( keypointMatchCount >= 4 )
         {
            keypointMatchCount = Features2DToolbox.VoteForSizeAndOrientation( modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20 );
            if ( keypointMatchCount >= 4 )
            {
               Features2DToolbox.GetHomographyMatrixFromMatchedFeatures( modelKeyPoints, observedKeyPoints, indices, mask, 2 );
            }
         }

         var similarity = (double)keypointMatchCount / mask.Height;
         return similarity > 0.075;
      }
   }
}
