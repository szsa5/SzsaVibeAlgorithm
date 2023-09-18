using Emgu.CV;
using Emgu.CV.Structure;
using System;

namespace SzsaVibeAlgorithm
{
    public partial class SzsaVibeInterface : Form
    {
        private VideoCapture capture;
        private bool isPlaying = false;
        private int totalFrames;
        private int currentFrameNum;
        private Mat currentFrame;
        private int fps;

        private int numOfSamples = 20;
        private int radius = 20;
        private int minCardinality = 2;
        private int updateFactor = 16;

        public SzsaVibeInterface()
        {
            InitializeComponent();
        }

        private void selectFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Video Files (*.mp4, *.avi)| *.mp4;*.avi";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                capture = new VideoCapture(ofd.FileName);
                totalFrames = Convert.ToInt32(capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount));
                fps = Convert.ToInt32(capture.Get(Emgu.CV.CvEnum.CapProp.Fps));
                PlayVideo();
            }
        }

        private async void PlayVideo()
        {
            if (capture == null)
                return;

            isPlaying = true;
            currentFrame = new Mat();
            currentFrameNum = 0;

            try
            {
                while (isPlaying == true && currentFrameNum < totalFrames)
                {
                    capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrameNum);
                    capture.Read(currentFrame);
                    Bitmap bitmapFrame = currentFrame.ToBitmap();

                    // If first frame
                    if (currentFrameNum == 0)
                    {
                        Pixel[,] bgModel = InitBgModel(bitmapFrame);
                    }
                    int currentMatches = 0;

                    pictureBox1.Image = currentFrame.ToBitmap();
                    currentFrameNum += 1;
                    await Task.Delay(1000 / fps);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static Pixel[,] InitBgModel(Bitmap bitmapFrame)
        {
            int width = bitmapFrame.Width;
            int height = bitmapFrame.Height;

            // Create a 2D array to store pixel values
            Pixel[,] pixels = new Pixel[width, height];

            // Loop through each pixel in the Bitmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get the pixel value at (x, y)
                    Color pixelColor = bitmapFrame.GetPixel(x, y);

                    // Create pixel object, set to background pixel
                    Pixel pixel = new(pixelColor, false);

                    // Store the pixel value in the array
                    pixels[x, y] = pixel;
                }
            }

            return pixels;
        }

        private void BtnPlay_Click_1(object sender, EventArgs e)
        {
            if (capture != null)
            {
                isPlaying = true;
                PlayVideo();
            }

            else
                isPlaying = false;
        }


        private void BtnStop_Click_1(object sender, EventArgs e)
        {
            isPlaying = false;
            currentFrameNum = 0;
            pictureBox1.Image = null;
            pictureBox1.Invalidate();
        }
    }
}