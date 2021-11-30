using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;


namespace AugmentedRealityPlayer
{
    // Classe per il rilevamento della presenza di potenziali marker
    class MarkerDetection
    {
        // Proprietà di input della classe
        public Bitmap BitmapInputImage {get; private set;} // Frame di input
        public int EdgeThreshold { get; private set; } // Soglia per il rilevamento degli edge 
        public int BlobMinHeight { get; private set; } // Altezza minima dei blob da rilevare
        public int BlobMinWidth { get; private set; } // Larghezza minima dei blob da rilevare
        public int BrightnessDifference { get; private set; } // Differenza di luminosità sinistra/destra di un edge

        // Proprietà di output della classe
        public UnmanagedImage UnmanagedInputImage { get; private set; } // Versione unmanaged del frame di input  
        public UnmanagedImage GrayscaleImage { get; private set; } // Versione grayscale del frame di input
        public UnmanagedImage EdgeImage { get; private set; } // Risultato del rilevamento degli edge    
        public UnmanagedImage EdgeImageThresholded { get; private set; } // Risultato dell'applicazione del thresholding 
        public Blob[] DetectedBlobs { get; private set; } // Blob rilevati 
        public List<List<IntPoint>> AllSquareShapesCorners { get; private set; } // Quadruple di punti di corner di ogni blob quadrato (potenziale marker) rilevato


        public MarkerDetection(Bitmap img, int threshold, int blobMinH, int blobMinW, int brightnessDiff)
        {
            BitmapInputImage = (Bitmap)img.Clone(); // Uso una copia locale
            UnmanagedInputImage = UnmanagedImage.FromManagedImage(BitmapInputImage);
            EdgeThreshold = threshold;
            BlobMinHeight = blobMinH;
            BlobMinWidth = blobMinW;
            BrightnessDifference = brightnessDiff;
        }

        public void Execute()
        {
            // Conversione grayscale 
            if (BitmapInputImage.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                GrayscaleImage = UnmanagedInputImage;
            }
            else
            {
                GrayscaleImage = UnmanagedImage.Create(BitmapInputImage.Width, BitmapInputImage.Height, PixelFormat.Format8bppIndexed);
                Grayscale.CommonAlgorithms.BT709.Apply(UnmanagedInputImage, GrayscaleImage);
            }

            // Rilevamento degli edge --> TO DO
            DifferenceEdgeDetector edgeDetector = new DifferenceEdgeDetector();
            EdgeImage = edgeDetector.Apply(GrayscaleImage);

            // Sogliatura degli edge rilevati --> TO DO
            Threshold thresholdFilter = new Threshold(EdgeThreshold);
            EdgeImageThresholded = thresholdFilter.Apply(EdgeImage);

            // Rilevamento dei blob significativi --> TO DO
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinHeight = BlobMinHeight;
            blobCounter.MinWidth = BlobMinWidth;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            blobCounter.ProcessImage(EdgeImageThresholded);
            DetectedBlobs = blobCounter.GetObjectsInformation();

            // Verifica se i blob significativi trovati sono dei marker 
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            List<IntPoint> edgePoints;
            List<IntPoint> corners;
            List<IntPoint> leftEdgePoints, rightEdgePoints;
            AllSquareShapesCorners = new List<List<IntPoint>>();

            // Ciclo iterativo su tutti i blob trovati 
            if (DetectedBlobs != null)
            for (int i = 0, n = DetectedBlobs.Length; i < n; i++)
            {
                edgePoints = blobCounter.GetBlobsEdgePoints(DetectedBlobs[i]);
                corners = null;
                // Verifica se si tratta di un quadrilatero
                if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                {
                    leftEdgePoints = null;
                    rightEdgePoints = null;
                    blobCounter.GetBlobsLeftAndRightEdges(DetectedBlobs[i], out leftEdgePoints, out rightEdgePoints);
                    float diff = CalculateAverageEdgesBrightnessDifference(leftEdgePoints, rightEdgePoints, GrayscaleImage);
                    // Se la differenza di luminosità sinistra/destra dell'edge è superiore alla soglia, abbiamo un potenziale marker
                    if (diff > BrightnessDifference)
                    {
                        corners = CoordinatesOrdering(corners);
                        AllSquareShapesCorners.Add(corners);
                    }
                }
            }
        }

        // Funzione utilizzata per calcolare la differenza di luminosità media tra pixel interni ed esterni rispetto ad un edge
        private float CalculateAverageEdgesBrightnessDifference(List<IntPoint> leftEdgePoints, List<IntPoint> rightEdgePoints, UnmanagedImage image)
        {
            List<IntPoint> leftEdgePoints1 = new List<IntPoint>();
            List<IntPoint> leftEdgePoints2 = new List<IntPoint>();
            List<IntPoint> rightEdgePoints1 = new List<IntPoint>();
            List<IntPoint> rightEdgePoints2 = new List<IntPoint>();
            int tx1, tx2, ty;
            int widthM1 = image.Width - 1;
            int stepSize = 3;
            for (int k = 0; k < leftEdgePoints.Count; k++)
            {
                tx1 = leftEdgePoints[k].X - stepSize;
                tx2 = leftEdgePoints[k].X + stepSize;
                ty = leftEdgePoints[k].Y;
                leftEdgePoints1.Add(new IntPoint((tx1 < 0) ? 0 : tx1, ty));
                leftEdgePoints2.Add(new IntPoint((tx2 > widthM1) ? widthM1 : tx2, ty));
                tx1 = rightEdgePoints[k].X - stepSize;
                tx2 = rightEdgePoints[k].X + stepSize;
                ty = rightEdgePoints[k].Y;
                rightEdgePoints1.Add(new IntPoint((tx1 < 0) ? 0 : tx1, ty));
                rightEdgePoints2.Add(new IntPoint((tx2 > widthM1) ? widthM1 : tx2, ty));
            }
            byte[] leftValues1 = image.Collect8bppPixelValues(leftEdgePoints1);
            byte[] leftValues2 = image.Collect8bppPixelValues(leftEdgePoints2);
            byte[] rightValues1 = image.Collect8bppPixelValues(rightEdgePoints1);
            byte[] rightValues2 = image.Collect8bppPixelValues(rightEdgePoints2);
            float diff = 0;
            int pixelCount = 0;
            for (int k = 0; k < leftEdgePoints.Count; k++)
            {
                if (rightEdgePoints[k].X - leftEdgePoints[k].X > stepSize * 2)
                {
                    diff += (leftValues1[k] - leftValues2[k]);
                    diff += (rightValues2[k] - rightValues1[k]);
                    pixelCount += 2;
                }
            }
            return diff / pixelCount;
        }

        // Funzione per stabilire un ordinamento dei 4 punti di corner trovati (senso orario a partire dal pixel più in alto e più a sinistra)
        private List<IntPoint> CoordinatesOrdering(List<IntPoint> corners)
        {
            IntPoint firstCorner = new IntPoint(Int32.MaxValue, Int32.MaxValue);
            IntPoint secondCorner = new IntPoint(Int32.MaxValue, Int32.MaxValue);
            IntPoint thirdCorner;
            IntPoint fourthCorner;

            // Trova il corner che sta più in alto
            for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i].Y < firstCorner.Y)

                    firstCorner = corners[i];
            }
            corners.Remove(firstCorner);

            // Trova il secondo corner che sta più in alto
            for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i].Y < secondCorner.Y)

                    secondCorner = corners[i];
            }
            corners.Remove(secondCorner);

            // Dei due corner trovati, il primo è quello che sta più a sinistra, il secondo è quello che sta più a destra
            if (firstCorner.X > secondCorner.X)
            {
                var temp = firstCorner;
                firstCorner = secondCorner;
                secondCorner = temp;
            }

            // Dei due corner rimasti, il terzo è quello che sta più a destra, il quarto quello che sta più a sinistra
            if (corners[0].X > corners[1].X)
            {
                thirdCorner = corners[0];
                fourthCorner = corners[1];
            }
            else
            {
                thirdCorner = corners[1];
                fourthCorner = corners[0];
            }

            // Costruzione della lista di corner ordinata in senso orario
            List<IntPoint> orderedCornersList = new List<IntPoint>();
            orderedCornersList.Add(firstCorner);
            orderedCornersList.Add(secondCorner);
            orderedCornersList.Add(thirdCorner);
            orderedCornersList.Add(fourthCorner);

            return (orderedCornersList);

        }

    }
}
