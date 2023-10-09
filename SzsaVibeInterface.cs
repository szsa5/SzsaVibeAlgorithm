using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Text;

namespace SzsaVibeAlgorithm
{
    public partial class SzsaVibeInterface : Form
    {
        VideoCapture capture;
         bool isPlaying = false;
         int totalFrames;
         int currentFrameNum;
         Mat currentFrame;
         int fps;
         int width;
         int height;
        
        int totalMatches;

        static int numOfSamples = 20;
        static int radius = 10;
        static int minCardinality = 2;
        static int updateFactor = 16;

        static Random random = new Random();

        Color[,,] bgModelBuffer;
        int[][,] binaryMaskArray;

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

        private void PlayVideo()
        {
            if (capture == null)
                return;

            isPlaying = true;
            currentFrame = new Mat();
            currentFrameNum = 0;
            binaryMaskArray = new int[totalFrames][,];
            try
            {
                while (isPlaying == true && currentFrameNum < totalFrames)
                {
                    capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrameNum);
                    capture.Read(currentFrame);
                    Bitmap bitmapFrame = currentFrame.ToBitmap();

                    // If first frame, initialize background models
                    if (currentFrameNum == 0)
                    {
                        width = bitmapFrame.Width;
                        height = bitmapFrame.Height;
                        bgModelBuffer = InitBgModels(bitmapFrame);
                    }

                    //Classify frame into binary mask
                    Debug.WriteLine($"Classifying frame {currentFrameNum}");
                    int [,] binaryMask = ClassifyFrame(bitmapFrame);
                    binaryMaskArray[currentFrameNum] = binaryMask;

                    //pictureBox1.Image = currentFrame.ToBitmap();
                    currentFrameNum += 1;
                    //await Task.Delay(1000 / fps);
                }


                WriteVideo(binaryMaskArray);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void WriteVideo(int[][,] frames)
        {

            // Create a VideoWriter to save the frames as a video
            using VideoWriter writer = new VideoWriter("output.mp4", VideoWriter.Fourcc('X', '2', '6', '4'), fps, new Size(width, height), true);

            // Loop through each frame and convert the 2D int array into an Image<Rgb, byte>
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];

                // Create a new Image<Rgb, byte> to represent the frame
                Image<Rgb, byte> frameImage = new Image<Rgb, byte>(width, height);

                // Loop through each pixel and set the color based on the 0s and 1s in the array
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Convert the 0s and 1s to black and white color

                        Rgb color = (frame[x, y] == 0) ? new Rgb(0, 0, 0) : new Rgb(255, 255, 255);

                        // Set the color of the pixel in the frame image
                        frameImage[y, x] = color;
                    }
                }
                // Write the frame image to the VideoWriter
                writer.Write(frameImage);
            }

            MessageBox.Show("Video generated successfully.");
        }

        private int[,] ClassifyFrame(Bitmap bitmapFrame)
        {
            int[,] binaryMask = new int[width, height];
            

            // Loop through each pixel in the Bitmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int curMemPos = 0;
                    int currentMatches = 0;
                    for (int i = 0; i < numOfSamples; i++)
                    {
                        // Get the pixel value at (x, y)
                        Color currentPixelColor = bitmapFrame.GetPixel(x, y);

                        Color bgModelPixel = bgModelBuffer[i, x, y];

                        // Calculate distance from background model

                        int distance = Math.Abs(currentPixelColor.R - bgModelPixel.R) +
                                        Math.Abs(currentPixelColor.G - bgModelPixel.G) +
                                        Math.Abs(currentPixelColor.B - bgModelPixel.B);

                        if (distance <= (4.5 * radius))
                            currentMatches++;

                        
                        if (currentMatches >= minCardinality)
                        {

                            // Classify pixel as background and break
                            binaryMask[x, y] = 0;

                            /*
                            // TODO: SWAP MATCHES TO FIRST POSITIONS OF BUFFER?
                            VibeModel swap = bgModelBuffer[curMjemPos, x,y];
                            bgModelBuffer[curMemPos, x,y] = bgModelPixel;
                            bgModelBuffer[i, x, y] = swap;
                            curMemPos += 1;
                            */
                            break;
                        }
                    }

                    if (currentMatches < minCardinality)
                    {
                        // Classify pixel as foreground
                        binaryMask[x, y] = 1;
                        totalMatches++;
                    }
                }
            }

            return binaryMask;
        }

        private void SwapBuffer(VibeModel model)
        {
        }

        private static Color[,,] InitBgModels(Bitmap bitmapFrame)
        {
            Color[,,] bgModelArray = new Color[numOfSamples, bitmapFrame.Width, bitmapFrame.Height];
            for (int i = 0; i < numOfSamples; i++)
            {

                // Loop through each pixel in the Bitmap
                for (int y = 0; y < bitmapFrame.Height; y++)
                {
                    for (int x = 0; x < bitmapFrame.Width; x++)
                    {
                        // Get the pixel value at (x, y)
                        Color pixelColor = bitmapFrame.GetPixel(x, y);

                        // Add noise
                        Color noisyPixelColor = AddNoiseToColor(pixelColor);

                        // Create pixel object, set to background pixel
                        // Store the pixel value in the array
                        bgModelArray[i,x,y] = noisyPixelColor;
                        
                    }
                }
            }
            return bgModelArray;
        }

        public static Color AddNoiseToColor(Color color)
        {
            int maxNoise = 3;
            int red = color.R;
            int green = color.G;
            int blue = color.B;

            if (random.Next(2) == 0)
            {
                // Add random noise to each channel
                red += random.Next(-maxNoise, maxNoise + 1);
                green += random.Next(-maxNoise, maxNoise + 1);
                blue += random.Next(-maxNoise, maxNoise + 1);

                // Make sure the color values stay within the valid range of 0-255
                red = Math.Max(0, Math.Min(255, red));
                green = Math.Max(0, Math.Min(255, green));
                blue = Math.Max(0, Math.Min(255, blue));
            }

            return Color.FromArgb(color.A, red, green, blue);
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