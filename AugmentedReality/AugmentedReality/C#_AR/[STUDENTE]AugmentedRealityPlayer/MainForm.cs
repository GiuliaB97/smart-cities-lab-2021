// VERSIONE STUDENTE: Ultimo aggiornamento 13/04/2014
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using AForge.Video;
using AForge.Video.DirectShow;
using AugmentedRealityPlayer.Properties;

namespace AugmentedRealityPlayer
{
    public partial class MainForm : Form
    {
        private Stopwatch stopWatch = null;

        // Classi di elaborazione
        MarkerDetection markerDetectionOperator;
        MarkerRecognition markerRecognitionOperator;
        PoseEstimation poseEstimationOperator;
        ImageAugmentation imageAugmentationOperator;

        public MainForm( )
        {
            InitializeComponent( );
        }

        // Metodo per l'elaborazione dei frame di input
        // N.B. Il frame è passato al metodo per riferimento
        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            // TO DO...


            /*
             *  // Rilevamento di potenziali marker
            markerDetectionOperator = new MarkerDetection(image, 30, 48, 48, 40);
            markerDetectionOperator.Execute();

            // Se sono stati trovati potenziali marker (blob) occorre decodificarne il contenuto
            if (markerDetectionOperator.AllSquareShapesCorners.Count > 0)
            {
                // Decodifica del contenuto (pattern) del marker
                markerRecognitionOperator = new MarkerRecognition(markerDetectionOperator.GrayscaleImage, markerDetectionOperator.AllSquareShapesCorners, 0.80f);
                markerRecognitionOperator.Execute();

                // Se è stato trovato il pattern del marker di nostro interesse
                if (markerRecognitionOperator.IsMarkerFound)
                {
                    // Calcolo della posa del marker rispetto alla camera
                    poseEstimationOperator = new PoseEstimation(markerRecognitionOperator.MarkerCorners, image.Width, image.Height);
                    poseEstimationOperator.Execute();

                    // Proiezione delle informazioni aumentanti
                    imageAugmentationOperator = new ImageAugmentation(image, Resources.Globe, poseEstimationOperator.MarkerCorners, poseEstimationOperator.TransformationMatrix);
                    imageAugmentationOperator.Execute();
                    image = imageAugmentationOperator.BitmapOutputImage;
                }
            }
             */
        }

        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            CloseCurrentVideoSource( );
        }

        private void exitToolStripMenuItem_Click( object sender, EventArgs e )
        {
            this.Close( );
        }

        // Apertura flusso video da webcam
        private void localVideoCaptureDeviceToolStripMenuItem_Click( object sender, EventArgs e )
        {
            VideoCaptureDeviceForm form = new VideoCaptureDeviceForm();

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                OpenVideoSource(form.VideoDevice);
            }
        }

        // Apertura flusso video da file
        private void openVideofileusingDirectShowToolStripMenuItem_Click( object sender, EventArgs e )
        {
            if ( openFileDialog.ShowDialog( ) == DialogResult.OK )
            {
                FileVideoSource fileSource = new FileVideoSource( openFileDialog.FileName );

                OpenVideoSource( fileSource );
            }
        }

        // Apertura flusso video da JPEG URL
        private void openJPEGURLToolStripMenuItem_Click( object sender, EventArgs e )
        {
            URLForm form = new URLForm( );

            form.Description = "Enter URL of an updating JPEG from a web camera:";
            form.URLs = new string[]
				{
					"http://195.243.185.195/axis-cgi/jpg/image.cgi?camera=1",
				};

            if ( form.ShowDialog( this ) == DialogResult.OK )
            {
                JPEGStream jpegSource = new JPEGStream( form.URL );

                OpenVideoSource( jpegSource );
            }
        }

        // Apertura flusso video da MJPEG URL
        private void openMJPEGURLToolStripMenuItem_Click( object sender, EventArgs e )
        {
            URLForm form = new URLForm( );

            form.Description = "Enter URL of an MJPEG video stream:";
            form.URLs = new string[]
				{
					"http://195.243.185.195/axis-cgi/mjpg/video.cgi?camera=4",
					"http://195.243.185.195/axis-cgi/mjpg/video.cgi?camera=3",
				};

            if ( form.ShowDialog( this ) == DialogResult.OK )
            {
                MJPEGStream mjpegSource = new MJPEGStream( form.URL );

                OpenVideoSource( mjpegSource );
            }
        }

        // Apertura flusso video
        private void OpenVideoSource( IVideoSource source )
        {
            // Set del cursore di attesa
            this.Cursor = Cursors.WaitCursor;

            // Chiusura del flusso video corrente 
            CloseCurrentVideoSource( );

            // Apertura del nuovo flusso video 
            videoSourcePlayer.VideoSource = source;
            videoSourcePlayer.Start( );

            // Reset stop watch
            stopWatch = null;

            // Start timer
            timer.Start( );

            this.Cursor = Cursors.Default;
        }

        // Chiusura del flusso video corrente (se presente)
        private void CloseCurrentVideoSource( )
        {
            if ( videoSourcePlayer.VideoSource != null )
            {
                videoSourcePlayer.SignalToStop( );

                // Attesa ~ 3 seconds
                for ( int i = 0; i < 30; i++ )
                {
                    if ( !videoSourcePlayer.IsRunning )
                        break;
                    System.Threading.Thread.Sleep( 100 );
                }

                if ( videoSourcePlayer.IsRunning )
                {
                    videoSourcePlayer.Stop( );
                }

                videoSourcePlayer.VideoSource = null;
            }
        }

        // Metodo per la gestione della "statistica" sul numero di frame elaborati al secondo
        private void timer_Tick( object sender, EventArgs e )
        {
            IVideoSource videoSource = videoSourcePlayer.VideoSource;

            if ( videoSource != null )
            {
                // Numero di frame elaborati dal precedenti "timer tick"
                int framesReceived = videoSource.FramesReceived;

                if ( stopWatch == null )
                {
                    stopWatch = new Stopwatch( );
                    stopWatch.Start( );
                }
                else
                {
                    stopWatch.Stop( );

                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    fpsLabel.Text = fps.ToString( "F2" ) + " fps";

                    stopWatch.Reset( );
                    stopWatch.Start( );
                }
            }
        }
    }
}
