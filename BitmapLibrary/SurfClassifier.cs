using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace BitmapLibrary
{
   static class SurfClassifier
   {
      public static bool FindModelImageInObservedImage( Image<Gray, byte> modelImage, Image<Gray, byte> observedImage )
      {
         var surfCpu = new SURFDetector(500, false);
         VectorOfKeyPoint modelKeyPoints;
         VectorOfKeyPoint observedKeyPoints;
         Matrix<int> indices;

         Matrix<byte> mask;
         int k = 2;
         double uniquenessThreshold = 0.8;
         if ( GpuInvoke.HasCuda )
         {
            GpuSURFDetector surfGpu = new GpuSURFDetector(surfCpu.SURFParams, 0.01f);
            using ( GpuImage<Gray, byte> gpuModelImage = new GpuImage<Gray, byte>( modelImage ) )
            //extract features from the object image
            using ( GpuMat<float> gpuModelKeyPoints = surfGpu.DetectKeyPointsRaw( gpuModelImage, null ) )
            using ( GpuMat<float> gpuModelDescriptors = surfGpu.ComputeDescriptorsRaw( gpuModelImage, null, gpuModelKeyPoints ) )
            using ( GpuBruteForceMatcher<float> matcher = new GpuBruteForceMatcher<float>( DistanceType.L2 ) )
            {
               modelKeyPoints = new VectorOfKeyPoint();
               surfGpu.DownloadKeypoints( gpuModelKeyPoints, modelKeyPoints );

               // extract features from the observed image
               using ( GpuImage<Gray, byte> gpuObservedImage = new GpuImage<Gray, byte>( observedImage ) )
               using ( GpuMat<float> gpuObservedKeyPoints = surfGpu.DetectKeyPointsRaw( gpuObservedImage, null ) )
               using ( GpuMat<float> gpuObservedDescriptors = surfGpu.ComputeDescriptorsRaw( gpuObservedImage, null, gpuObservedKeyPoints ) )
               using ( GpuMat<int> gpuMatchIndices = new GpuMat<int>( gpuObservedDescriptors.Size.Height, k, 1, true ) )
               using ( GpuMat<float> gpuMatchDist = new GpuMat<float>( gpuObservedDescriptors.Size.Height, k, 1, true ) )
               using ( GpuMat<Byte> gpuMask = new GpuMat<byte>( gpuMatchIndices.Size.Height, 1, 1 ) )
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
                     int nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                     if ( nonZeroCount >= 4 )
                     {
                        Features2DToolbox.GetHomographyMatrixFromMatchedFeatures( modelKeyPoints, observedKeyPoints, indices, mask, 2 );
                     }
                     if ( (double)nonZeroCount / mask.Height > 0.02 )
                     {
                        return true;
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
            using ( Matrix<float> dist = new Matrix<float>( observedDescriptors.Rows, k ) )
            {
               matcher.KnnMatch( observedDescriptors, indices, dist, k, null );
               mask = new Matrix<byte>( dist.Rows, 1 );
               mask.SetValue( 255 );
               Features2DToolbox.VoteForUniqueness( dist, uniquenessThreshold, mask );
            }

            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if ( nonZeroCount >= 4 )
            {
               nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation( modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20 );
               if ( nonZeroCount >= 4 )
               {
                  Features2DToolbox.GetHomographyMatrixFromMatchedFeatures( modelKeyPoints, observedKeyPoints, indices, mask, 2 );
               }
            }

            if ( (double)nonZeroCount/mask.Height > 0.02 )
            {
               return true;
            }
         }

         //Draw the matched keypoints
         //var result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints, indices, new Bgr(0, 0, 255), new Bgr(255, 0, 0), mask, Features2DToolbox.KeypointDrawType.DEFAULT);
         //result.Save( @"C:\Users\D.Markachev\Desktop\bleh-keypoints.jpg" );

         return false;
      }
   }
}
