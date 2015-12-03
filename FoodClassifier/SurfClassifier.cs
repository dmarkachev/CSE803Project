using System.Diagnostics;
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
                      IsModelInObserved( GetImage( "Banana.VeryRipeFlesh.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.Internal.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Banana.SuperFreshFlesh.jpg" ), observedImage );
         return found;
      }

      public static bool HasStrawberrySeeds( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Strawberry.Seeds1.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Seeds2.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Seeds3.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Seeds4.jpg" ), observedImage, 0.035 );
         return found;
      }

      public static bool HasStrawberryLeaves( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Strawberry.Leaves1.jpg" ), observedImage, 0.07 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Leaves2.jpg" ), observedImage, 0.07 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Leaves3.jpg" ), observedImage, 0.07 ) ||
                      IsModelInObserved( GetImage( "Strawberry.Leaves4.jpg" ), observedImage, 0.07 );
         return found;
      }

      public static bool HasCookieChips( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Cookie.Chips1.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Cookie.Chips2.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Cookie.Chips3.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Cookie.Chips4.jpg" ), observedImage );
         return found;
      }

      public static bool HasSausage( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "HotDog.Sausage1.jpg" ), observedImage, 0.049 ) ||
                      IsModelInObserved( GetImage( "HotDog.Sausage2.jpg" ), observedImage, 0.049 ) ||
                      IsModelInObserved( GetImage( "HotDog.Sausage3.jpg" ), observedImage, 0.049 ) ||
                      IsModelInObserved( GetImage( "HotDog.Sausage4.jpg" ), observedImage, 0.049 );
                      IsModelInObserved( GetImage( "HotDog.Sausage5.jpg" ), observedImage, 0.049 );
         return found;
      }

      public static bool HasSausageWithToppings( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "HotDog.SausageWithToppings1.jpg" ), observedImage, 0.05 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageWithToppings2.jpg" ), observedImage, 0.05 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageWithToppings3.jpg" ), observedImage, 0.05 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageWithToppings4.jpg" ), observedImage, 0.05 );
         return found;
      }

      public static bool HasSausageBetweenBuns( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "HotDog.SausageBetweenBuns1.jpg" ), observedImage, 0.06 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageBetweenBuns2.jpg" ), observedImage, 0.06 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageBetweenBuns3.jpg" ), observedImage, 0.06 ) ||
                      IsModelInObserved( GetImage( "HotDog.SausageBetweenBuns4.jpg" ), observedImage, 0.06 );
         return found;
      }

      public static bool HasBroccoliTop( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Broccoli.Top1.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Broccoli.Top2.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Broccoli.Top3.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Broccoli.Top4.jpg" ), observedImage, 0.035 ) ||
                      IsModelInObserved( GetImage( "Broccoli.Top5.jpg" ), observedImage, 0.035 );
         return found;
      }

      public static bool HasEggYolk( this Image<Gray, byte> observedImage )
      {
         bool found = IsModelInObserved( GetImage( "Egg.Yolk1.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Egg.Yolk2.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Egg.Yolk3.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Egg.Yolk4.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Egg.Yolk5.jpg" ), observedImage ) ||
                      IsModelInObserved( GetImage( "Egg.Yolk6.jpg" ), observedImage );
         return found;
      }

      private static Image<Gray, byte> GetImage( string resourceName )
      {
         var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "FoodClassifier.SurfImages." + resourceName );

         Debug.Assert( imageStream != null );
         return new Image<Gray, byte>( new Bitmap( imageStream ) );
      }

      private static bool IsModelInObserved( Image<Gray, byte> modelImage, Image<Gray, byte> observedImage, double similarityThreshold = 0.075 )
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

         var similarity = (double)keypointMatchCount / observedKeyPoints.Size;
         return similarity > similarityThreshold;
      }
   }
}
