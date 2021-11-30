using System;
using System.Collections.Generic;
using System.Text;
using AForge;
using AForge.Math;
using AForge.Math.Geometry;

namespace AugmentedRealityPlayer
{
    // Classe per la stima della posa del marker rispetto alla camera
    class PoseEstimation
    {
        // Proprietà di input della classe
        public List<IntPoint> MarkerCorners { get; private set; } // Punti di corner del marker principale (a maggior confidenza) rilevato
        public int ImageWidth { get; private set; } // Larghezza del frame di input
        public int ImageHeight { get; private set; } // Altezza del frame di input

        // Proprietà di output della classe
        public Matrix4x4 TransformationMatrix { get; private set; } // Matrice di trasformazione (o matrice di rototraslazione)

        public PoseEstimation(List<IntPoint> markerCorners, int w, int h)
        {
            MarkerCorners = markerCorners;
            ImageWidth = w;
            ImageHeight = h;
        }

        public void Execute()
        {
            int halfWidth = ImageWidth / 2;
            int halfHeight = ImageHeight / 2;

            // Ricalcolo delle coordinate dei punti di corner del marker stabilendo come origine degli assi il centro del frame di input
            AForge.Point[] newMarkerCornerPoints = new AForge.Point[4]
            {
                new AForge.Point(MarkerCorners[0].X - halfWidth, halfHeight - MarkerCorners[0].Y),
                new AForge.Point(MarkerCorners[1].X - halfWidth, halfHeight - MarkerCorners[1].Y),
                new AForge.Point(MarkerCorners[2].X - halfWidth, halfHeight - MarkerCorners[2].Y),
                new AForge.Point(MarkerCorners[3].X - halfWidth, halfHeight - MarkerCorners[3].Y) 
            };

            // Coordinate "mondo" (del mondo reale) dei punti di corner del marker (supponendo il sistema di riferimento centrato sull'oggetto)
            Vector3[] markerModelCornerPoints = new Vector3[]
            {
                new Vector3( -50f, 50f,  0),
                new Vector3(  50f, 50f,  0),
                new Vector3(  50f, -50f, 0),
                new Vector3( -50f, -50f, 0),              
            };

            // Calcolo della matrice di trasformazione (o matrice di rototraslazione) --> TO DO
            
        }
    }

}
