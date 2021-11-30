using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace AugmentedRealityPlayer
{
    // Classe per la decodifica del contenuto (pattern) dei potenziali marker 
    class MarkerRecognition
    {
        // Proprietà di input della classe
        public UnmanagedImage GrayscaleImage { get; private set; } // Versione grayscale del frame di input
        public List<List<IntPoint>> AllSquareShapesCorners { get; private set; } // Quadruple di punti di corner di ogni blob quadrato (potenziale marker) rilevato
        public float MinConfidenceValue { get; private set; } // Soglia di confidenza per la decodifica del marker

        // Proprietà di output della classe
        public List<IntPoint> MarkerCorners { get; private set; } // Punti di corner del marker principale (a maggior confidenza) rilevato
        public UnmanagedImage DetectedMarkerImage { get; private set; } // Immagine "ritagliata" e "allineata" del marker principale rilevato
        public byte[,] DetectedMarkerCode { get; private set; } // Codice associato al marker principale rilevato
        public List<UnmanagedImage> AllDetectedMarkerImages { get; private set; } // Lista di immagini "ritagliate" e "allineate" per tutti i marker rilevati 
        public List<byte[,]> AllDetectedMarkerCodes { get; private set; } // Lista di codici associati a tutti i marker rilevati
        public bool IsMarkerFound { get; private set; } // Flag per segnalare il rilevamento di un marker di nostro interesse

        public MarkerRecognition(UnmanagedImage grayImg, List<List<IntPoint>> blobsCorners, float minConfValue)
        {
            GrayscaleImage = grayImg;
            AllSquareShapesCorners = blobsCorners;
            MinConfidenceValue = minConfValue;
        }

        public void Execute()
        {
            QuadrilateralTransformation quadrilateralTransformation;
            OtsuThreshold otsuThresholdFilter;
            float decodingConfidence = 0;
            float bestConfidence = 0;
            int bestMarkerIndex = 0;
            List<IntPoint> bestMarkerCorners = new List<IntPoint>();
            AllDetectedMarkerImages = new List<UnmanagedImage>();
            AllDetectedMarkerCodes = new List<byte[,]>();

            for (int i = 0; i < AllSquareShapesCorners.Count; i++)
            {
                // Trasformazione per "allineare" il marker -> TO DO


                // Binarizzazione dell'immagine del marker mediante filtro di Otsu -> TO DO


                // Decodifica del contenuto del marker -> TO DO


                // Calcolo dell'orientazione del marker (se otteniamo null come risultato, significa che il pattern desiderato non è stato trovato) -> TO DO


                // Se il pattern è stato trovato con una certa orientazione
                // E se il contenuto del marker è stato rilevato con una confidenza superiore al livello minimo
                // E la confidenza è superiore a quella con cui sono stati rilevati altri marker con lo stesso pattern
                // Questo è il marker su cui devo proiettare il contenuto aumentato
                if (MarkerCorners != null && decodingConfidence >= MinConfidenceValue && decodingConfidence > bestConfidence)
                {
                    bestMarkerCorners = MarkerCorners;
                    bestConfidence = decodingConfidence;
                    bestMarkerIndex = i;                          
                }
            }

            // Se il marker di nostro interesse è stato trovato
            if (bestConfidence > 0)
            {
                DetectedMarkerImage = AllDetectedMarkerImages[bestMarkerIndex];
                DetectedMarkerCode = AllDetectedMarkerCodes[bestMarkerIndex];
                MarkerCorners = bestMarkerCorners;
                IsMarkerFound = true;
            }
            // Se il marker di nostro interesse non è stato trovato
            else
            {
                DetectedMarkerImage = null;
                DetectedMarkerCode = null;
                MarkerCorners = null;
                IsMarkerFound = false;
            }
        }

        // Metodo per la decodifica del contenuto (pattern) di un marker
        private byte[,] MarkerDecoding(UnmanagedImage image, Rectangle rect, out float confidence)
        {
            int markerSize = 5;
            int markerStartX = rect.Left;
            int markerStartY = rect.Top;
            int markerWidth = rect.Width;
            int markerHeight = rect.Height;
            int cellWidth = markerWidth / markerSize;
            int cellHeight = markerHeight / markerSize;
            int cellOffsetX = (int)(cellWidth * 0.2);
            int cellOffsetY = (int)(cellHeight * 0.2);
            int cellScanX = (int)(cellWidth * 0.6);
            int cellScanY = (int)(cellHeight * 0.6);
            int cellScanArea = cellScanX * cellScanY;
            int[,] cellIntensity = new int[markerSize, markerSize];
            unsafe
            {
                int stride = image.Stride;
                byte* srcBase = (byte*)image.ImageData.ToPointer() +
                    (markerStartY + cellOffsetY) * stride +
                    markerStartX + cellOffsetX;
                byte* srcLine;
                byte* src;
                for (int gi = 0; gi < markerSize; gi++)
                {
                    srcLine = srcBase + cellHeight * gi * stride;
                    for (int y = 0; y < cellScanY; y++)
                    {
                        for (int gj = 0; gj < markerSize; gj++)
                        {
                            src = srcLine + cellWidth * gj;
                            for (int x = 0; x < cellScanX; x++, src++)
                            {
                                cellIntensity[gi, gj] += *src;
                            }
                        }
                        srcLine += stride;
                    }
                }
            }

            // Calcolo del valore di ciascuna cella del marker e calcolo del valore di confidenza
            byte[,] markerValues = new byte[markerSize, markerSize];
            confidence = 1f;
            for (int gi = 0; gi < markerSize; gi++)
            {
                for (int gj = 0; gj < markerSize; gj++)
                {
                    float fullness = (float)
                        (cellIntensity[gi, gj] / 255) / cellScanArea;
                    float conf = (float)System.Math.Abs(fullness - 0.5) + 0.5f;
                    markerValues[gi, gj] = (byte)((fullness > 0.5f) ? 1 : 0);
                    if (conf < confidence)
                        confidence = conf;
                }
            }
            return markerValues;
        }

        // Metodo per verifica la corrispondenza e l'orientazione del pattern (e conseguentemente l'orientazione dei corner del marker)
        private List<IntPoint> MarkerCornersOrientation(List<IntPoint> corners, byte[,] markerCode)
        {
            List<IntPoint> rotatedCorners = new List<IntPoint>();
            byte[,] marker_0degree = new byte[5, 5] {{0,0,0,0,0}, 
                                       {0,1,1,0,0},
                                       {0,1,0,1,0},  
                                       {0,0,1,0,0},
                                       {0,0,0,0,0}};
            byte[,] marker_90degree = new byte[5, 5] {{0,0,0,0,0}, 
                                       {0,0,1,0,0},
                                       {0,1,0,1,0},  
                                       {0,1,1,0,0},
                                       {0,0,0,0,0}};
            byte[,] marker_180degree = new byte[5, 5] {{0,0,0,0,0}, 
                                       {0,0,1,0,0},
                                       {0,1,0,1,0},  
                                       {0,0,1,1,0},
                                       {0,0,0,0,0}};
            byte[,] marker_270degree = new byte[5, 5] {{0,0,0,0,0}, 
                                       {0,0,1,1,0},
                                       {0,1,0,1,0},  
                                       {0,0,1,0,0},
                                       {0,0,0,0,0}};

            int distance0 = MarkerCodeDistance(marker_0degree, markerCode);
            int distance90 = MarkerCodeDistance(marker_90degree, markerCode);
            int distance180 = MarkerCodeDistance(marker_180degree, markerCode);
            int distance270 = MarkerCodeDistance(marker_270degree, markerCode);

            if (distance0 == 0)
            {
                rotatedCorners.Add(corners[0]);
                rotatedCorners.Add(corners[1]);
                rotatedCorners.Add(corners[2]);
                rotatedCorners.Add(corners[3]);
            }
            else if (distance90 == 0)
            {
                rotatedCorners.Add(corners[3]);
                rotatedCorners.Add(corners[0]);
                rotatedCorners.Add(corners[1]);
                rotatedCorners.Add(corners[2]);
            }
            else if (distance180 == 0)
            {
                rotatedCorners.Add(corners[2]);
                rotatedCorners.Add(corners[3]);
                rotatedCorners.Add(corners[0]);
                rotatedCorners.Add(corners[1]);
            }
            else if (distance270 == 0)
            {
                rotatedCorners.Add(corners[1]);
                rotatedCorners.Add(corners[2]);
                rotatedCorners.Add(corners[3]);
                rotatedCorners.Add(corners[0]);
            }
            else
            {
                rotatedCorners = null;
            }

            return (rotatedCorners);
        }

        // Metodo per determinare la diversità (distanza) tra due codici di marker (se due marker hanno lo stesso pattern -> distanza = 0)
        private int MarkerCodeDistance(byte[,] MarkerOne, byte[,] MarkerTwo)
        {
            int distance = 0;
            for (int k = 0; k < 5; k++)
                for (int j = 0; j < 5; j++)
                    distance += Math.Abs(MarkerOne[j, k] - MarkerTwo[j, k]);

            return (distance);
        }
    }
}
