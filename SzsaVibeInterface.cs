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
         int currentFrameNum;
         Mat currentFrame;
         int totalFrames;
         int fps;
         int width;
         int height;

        const int numOfSamples = 20;
        const int radius = 10;
        const int minCardinality = 2;
        const int updateFactor = 16;

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

            // Start timer
            Stopwatch stopwatch = Stopwatch.StartNew();

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
                    Debug.WriteLine($"Classifying frame {currentFrameNum} / {totalFrames} " +
                                    $"({Math.Round(currentFrameNum / (double)totalFrames * 100, 2)}%)");

                    int [,] binaryMask = ClassifyFrame(bitmapFrame);
                    binaryMaskArray[currentFrameNum] = binaryMask;

                    // Update the model
                    UpdateBgModel(bitmapFrame);

                    //pictureBox1.Image = currentFrame.ToBitmap();
                    currentFrameNum += 1;
                    //await Task.Delay(1000 / fps);
                }

                stopwatch.Stop();
                double elapsedTime = stopwatch.ElapsedMilliseconds / 1000.0;
                Debug.WriteLine($"It took {elapsedTime}s to generate the output.");

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
                            Color swap = bgModelBuffer[curMemPos, x,y];
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
                    }
                }
            }

            return binaryMask;
        }

        private void SwapBuffer(VibeModel model)
        {
            throw new NotImplementedException();
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

        private void UpdateBgModel(Bitmap bitmapFrame)
        {
            //TODO: Optimize

            int randomUpdateValue = new Random().Next(1, updateFactor + 1);
            if (randomUpdateValue == 1)
            {
                int randomSampleValue = new Random().Next(0, numOfSamples);
                // Loop through each pixel in the Bitmap
                for (int y = 0; y < bitmapFrame.Height; y++)
                {
                    for (int x = 0; x < bitmapFrame.Width; x++)
                    {
                        if (binaryMaskArray[currentFrameNum][x, y] == 0)
                            bgModelBuffer[randomSampleValue, x, y] = bitmapFrame.GetPixel(x, y);

                        int randomEightValue = new Random().Next(0, updateFactor + 1);

                        if (randomEightValue == 1)
                        {
                            // Replace value chosen from the bg buffer by pixel value chosen from the 8-neighbourhood
                            Tuple<int,int> randomNeighborTuple = GetRandomNeighbor(height, width, x, y);
                            int eightX = randomNeighborTuple.Item1;
                            int eightY = randomNeighborTuple.Item2;
                            Color eightPixel = bitmapFrame.GetPixel(eightX, eightY);
                            bgModelBuffer[randomSampleValue, eightX, eightY] = eightPixel;
                        }
                    }
                }
            }
        }

        public static Tuple<int, int> GetRandomNeighbor(int rows, int cols, int x, int y)
        {
            Random random = new Random();

            //TODO: Optimize
            do
            {
                int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 }; // Relative X-coordinates of the 8 neighbors
                int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 }; // Relative Y-coordinates of the 8 neighbors

                int randomOffset = random.Next(8);

                int nx = x + dx[randomOffset];
                int ny = y + dy[randomOffset];

                if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
                {
                    return Tuple.Create(nx, ny); // Return the random neighbor
                }
            } while (true);
        }
    

        public static Color AddNoiseToColor(Color color)
        {

            Random random = new();

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