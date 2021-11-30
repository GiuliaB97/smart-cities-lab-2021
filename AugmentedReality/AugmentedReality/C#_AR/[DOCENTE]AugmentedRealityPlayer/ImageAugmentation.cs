using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Math;
using AForge.Imaging.Filters;
using AForge;

namespace AugmentedRealityPlayer
{
    // Classe per il rendering delle informazioni aumentanti sull'immagine (frame) di input
    class ImageAugmentation
    {
        // Proprietà di input della classe
        public Bitmap BitmapInputImage { get; private set; } // Frame di input
        public Bitmap BitmapAugmentedContent { get; private set; } // Contenuto aumentante (immagine da proiettare)
        public List<IntPoint> MarkerCorners { get; private set; } // Punti di corner del marker principale (a maggior confidenza) rilevato
        public Matrix4x4 TransformationMatrix { get; private set; } // Matrice di trasformazione (o matrice di rototraslazione)

        // Proprietà di output della classe
        public Bitmap BitmapOutputImage { get; private set; } // Frame di output

        public ImageAugmentation(Bitmap img, Bitmap contentImg, List<IntPoint> corners, Matrix4x4 trasfMatrix)
        {
            BitmapInputImage = (Bitmap)img.Clone();
            BitmapOutputImage = (Bitmap)img.Clone();
            BitmapAugmentedContent = contentImg.Clone(new Rectangle(0, 0, contentImg.Width, contentImg.Height), BitmapInputImage.PixelFormat);
            MarkerCorners = corners;
            TransformationMatrix = trasfMatrix;
        }

        public void Execute()
        {
            int halfWidth = BitmapInputImage.Width / 2;
            int halfHeight = BitmapInputImage.Height / 2;

            // Proiezione dell'immagine (image warping) -> TO DO...
            BackwardQuadrilateralTransformation myFilter = new BackwardQuadrilateralTransformation(BitmapAugmentedContent, MarkerCorners);
            myFilter.ApplyInPlace(BitmapOutputImage);

            // Proiezione del sistema di riferimento cartesiano solidale con il marker
            Graphics g = Graphics.FromImage(BitmapOutputImage);
           
            // Modello degli assi cartesiani
            Vector3[] axesModel = new Vector3[]
            {
                new Vector3( 0, 0, 0 ),
                new Vector3( 50, 0, 0 ),
                new Vector3( 0, 50, 0 ),
                new Vector3( 0, 0, -50 ),
            };

            AForge.Point[] projectedAxes = PerformProjection(axesModel, TransformationMatrix, BitmapInputImage.Width);

            // Disegno dell'asse X
            Pen bluePen = new Pen(Color.Blue, 5);
            g.DrawLine(bluePen, halfWidth + projectedAxes[0].X, halfHeight - projectedAxes[0].Y, halfWidth + projectedAxes[1].X, halfHeight - projectedAxes[1].Y);

            // Disegno dell'asse Y
            Pen redPen = new Pen(Color.Red, 5);
            g.DrawLine(redPen, halfWidth + projectedAxes[0].X, halfHeight - projectedAxes[0].Y, halfWidth + projectedAxes[2].X, halfHeight - projectedAxes[2].Y);

            // Disegno dell'asse Z
            Pen limePen = new Pen(Color.Lime, 5);
            g.DrawLine(limePen, halfWidth + projectedAxes[0].X, halfHeight - projectedAxes[0].Y, halfWidth + projectedAxes[3].X, halfHeight - projectedAxes[3].Y);          
        }

        // Metodo per eseguire la proiezione di un modello
        // Esso viene ruotato e traslato in maniera solidale al marker mediante uso della matrice di trasformazione
        private AForge.Point[] PerformProjection(Vector3[] model, Matrix4x4 transformationMatrix, int viewSize)
        {
            AForge.Point[] projectedPoints = new AForge.Point[model.Length];

            // -> TO DO
            for (int i = 0; i < model.Length; i++)
            {
                Vector3 scenePoint = (transformationMatrix * model[i].ToVector4()).ToVector3();

                projectedPoints[i] = new AForge.Point((int)(scenePoint.X / scenePoint.Z * viewSize),(int)(scenePoint.Y / scenePoint.Z * viewSize));
            }

            return projectedPoints;
        }

    }
}
