using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace FoodClassifier
{
   public static class SurfClassifier
   {
      public static double GetSurfSimilarity( Image<Gray, byte> modelImage, Image<Gray, byte> observedImage )
      {
         var surfCpu = new SURFDetector(500, false);
         VectorOfKeyPoint modelKeyPoints;
         VectorOfKeyPoint observedKeyPoints;
         Matrix<int> indices;

         Matrix<byte> mask;
         int k = 2;
         double uniquenessThreshold = 0.8;
         int keypointMatchCount = 0;
         if ( GpuInvoke.HasCuda )
         {
            GpuSURFDetector surfGpu = new GpuSURFDetector(surfCpu.SURFParams, 0.01f);
            using ( var gpuModelImage = new GpuImage<Gray, byte>( modelImage ) )
            //extract features from the object image
            using ( GpuMat<float> gpuModelKeyPoints = surfGpu.DetectKeyPointsRaw( gpuModelImage, null ) )
            using ( GpuMat<float> gpuModelDescriptors = surfGpu.ComputeDescriptorsRaw( gpuModelImage, null, gpuModelKeyPoints ) )
            using ( GpuBruteForceMatcher<float> matcher = new GpuBruteForceMatcher<float>( DistanceType.L2 ) )
            {
               modelKeyPoints = new VectorOfKeyPoint();
               surfGpu.DownloadKeypoints( gpuModelKeyPoints, modelKeyPoints );

               // extract features from the observed image
               using ( var gpuObservedImage = new GpuImage<Gray, byte>( observedImage ) )
               using ( GpuMat<float> gpuObservedKeyPoints = surfGpu.DetectKeyPointsRaw( gpuObservedImage, null ) )
               using ( GpuMat<float> gpuObservedDescriptors = surfGpu.ComputeDescriptorsRaw( gpuObservedImage, null, gpuObservedKeyPoints ) )
               using ( var gpuMatchIndices = new GpuMat<int>( gpuObservedDescriptors.Size.Height, k, 1, true ) )
               using ( var gpuMatchDist = new GpuMat<float>( gpuObservedDescriptors.Size.Height, k, 1, true ) )
               using ( var gpuMask = new GpuMat<byte>( gpuMatchIndices.Size.Height, 1, 1 ) )
               using ( var stream = new Emgu.CV.GPU.Stream() )
               {
                  matcher.KnnMatchSingle( gpuObservedDescriptors, gpuModelDescriptors, gpuMatchIndices, gpuMatchDist, k, null, stream );
                  indices = new Matrix<int>( gpuMatchIndices.Size );
                  mask = new Matrix<byte>( gpuMask.Size );

                  //gpu implementation of voteForUniquess
                  using ( GpuMat<float> col0 = gpuMatchDist.Col( 0 ) )
                  using ( GpuMat<float> col1 = gpuMatchDist.Col( 1 ) )
                  {
                     GpuInvoke.Multiply( col1, new MCvScalar( uniquenessThreshold ), col1, stream );
                     GpuInvoke.Compare( col0, col1, gpuMask, CMP_TYPE.CV_CMP_LE, stream );
                  }

                  observedKeyPoints = new VectorOfKeyPoint();
                  surfGpu.DownloadKeypoints( gpuObservedKeyPoints, observedKeyPoints );

                  //wait for the stream to complete its tasks
                  //We can perform some other CPU intesive stuffs here while we are waiting for the stream to complete.
                  stream.WaitForCompletion();

                  gpuMask.Download( mask );
                  gpuMatchIndices.Download( indices );

                  if ( GpuInvoke.CountNonZero( gpuMask ) >= 4 )
                  {
                     keypointMatchCount = Features2DToolbox.VoteForSizeAndOrientation( modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20 );
                     if ( keypointMatchCount >= 4 )
                     {
                        Features2DToolbox.GetHomographyMatrixFromMatchedFeatures( modelKeyPoints, observedKeyPoints, indices, mask, 2 );
                     }
                  }
               }
            }
         }
         else
         {
            //extract features from the object image
            modelKeyPoints = surfCpu.DetectKeyPointsRaw( modelImage, null );
            Matrix<float> modelDescriptors = surfCpu.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

            // extract features from the observed image
            observedKeyPoints = surfCpu.DetectKeyPointsRaw( observedImage, null );
            Matrix<float> observedDescriptors = surfCpu.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
            BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
            matcher.Add( modelDescriptors );

            indices = new Matrix<int>( observedDescriptors.Rows, k );
            using ( var dist = new Matrix<float>( observedDescriptors.Rows, k ) )
            {
               matcher.KnnMatch( observedDescriptors, indices, dist, k, null );
               mask = new Matrix<byte>( dist.Rows, 1 );
               mask.SetValue( 255 );
               Features2DToolbox.VoteForUniqueness( dist, uniquenessThreshold, mask );
            }

            keypointMatchCount = CvInvoke.cvCountNonZero( mask );
            if ( keypointMatchCount >= 4 )
            {
               keypointMatchCount = Features2DToolbox.VoteForSizeAndOrientation( modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20 );
               if ( keypointMatchCount >= 4 )
               {
                  Features2DToolbox.GetHomographyMatrixFromMatchedFeatures( modelKeyPoints, observedKeyPoints, indices, mask, 2 );
               }
            }
         }

         //Draw the matched keypoints
         //var result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints, indices, new Bgr(255, 255, 255), new Bgr(255, 255, 255), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

         return (double)keypointMatchCount / mask.Height;
      }
   }
}
