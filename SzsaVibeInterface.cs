using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
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
        const int radius = 20;
        const int minCardinality = 2;
        const int updateFactor = 16;

        const int numOfPixelsToCompare = 20000;

        Color[,,] bgModelBuffer;
        int[][,] binaryMaskArray;

        public SzsaVibeInterface()
        {
            InitializeComponent();
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
                    bool isFirstFrame = currentFrameNum == 0;
                    Bitmap bitmapFrame = currentFrame.ToBitmap();

                    // If first frame, initialize background models
                    if (isFirstFrame)
                    {
                        width = bitmapFrame.Width;
                        height = bitmapFrame.Height;
                    }

                    // Lock bitmap
                    Rectangle rect = new Rectangle(0, 0, width, height);
                    BitmapData bmpData = bitmapFrame.LockBits(rect, ImageLockMode.ReadWrite, bitmapFrame.PixelFormat);

                    if (isFirstFrame)
                        bgModelBuffer = InitBgModels(bmpData);  
                    else
                        if(CameraMoved(bmpData))
                            bgModelBuffer = InitBgModels(bmpData);

                    // Classify frame into binary mask
                    Debug.WriteLine($"Classifying frame {currentFrameNum + 1} / {totalFrames} " +
                                    $"({Math.Round(currentFrameNum / (double)totalFrames * 100, 2)}%)");

                    int [,] binaryMask = ClassifyFrame(bmpData);
                    binaryMaskArray[currentFrameNum] = binaryMask;

                    // Update the model
                    UpdateBgModel(bmpData);

                // Unlock bitmap
                bitmapFrame.UnlockBits(bmpData);

                currentFrameNum += 1;
                }

                stopwatch.Stop();
                double elapsedTime = stopwatch.ElapsedMilliseconds / 1000.0;
                Debug.WriteLine($"It took {elapsedTime}s to generate the output.");

                WriteVideo(binaryMaskArray);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Debug.WriteLine(ex.Message);
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

        private int[,] ClassifyFrame(BitmapData bmpData)
        {
            int[,] binaryMask = new int[width, height];

            // Loop through each pixel in the Bitmap
            Parallel.For(0, height, y =>
            {
                for(int x = 0; x < width; x++)
                {
                    //int curMemPos = 0;
                    int currentMatches = 0;

                    for (int i = 0; i < numOfSamples; i++)
                    {
                        // Get the pixel value at (x, y)
                        Color currentPixelColor = GetPixelColor(bmpData, x, y);

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
            });



            return binaryMask;
        }

        public static Color GetPixelColor(BitmapData bmpData, int x, int y)
        {
            // Calculate the index of the pixel
            int stride = bmpData.Stride;
            int pixelSize = Image.GetPixelFormatSize(bmpData.PixelFormat) / 8;
            int index = y * stride + x * pixelSize;

            // Get the pixel data from the bitmap
            IntPtr pixelPtr = bmpData.Scan0 + index;

            // Retrieve the color components
            byte blue = System.Runtime.InteropServices.Marshal.ReadByte(pixelPtr);
            byte green = System.Runtime.InteropServices.Marshal.ReadByte(pixelPtr + 1);
            byte red = System.Runtime.InteropServices.Marshal.ReadByte(pixelPtr + 2);

            // Create and return the Color object
            return Color.FromArgb(red, green, blue);
        }

        private void SwapBuffer(VibeModel model)
        {
            throw new NotImplementedException();
        }

        private Color[,,] InitBgModels(BitmapData bmpData)
        {
            Color[,,] bgModelArray = new Color[numOfSamples, width, height];

            // Iterate through each bgModel
            Parallel.For(0, numOfSamples, i =>
            {
                // Iterate through each pixel in the Bitmap
                Parallel.For(0, height, y =>
                {
                    Parallel.For(0, width, x =>
                    {
                        // Get the pixel value at (x, y)
                        Color pixelColor = GetPixelColor(bmpData, x, y);

                        // Add noise
                        Color noisyPixelColor = AddNoiseToColor(pixelColor);

                        // Create pixel object, set to background pixel
                        // Store the pixel value in the array
                        bgModelArray[i, x, y] = noisyPixelColor;

                    });
                });
            });
            return bgModelArray;
        }

        private void UpdateBgModel(BitmapData bmpData)
        {
            //TODO: Optimize

            int randomUpdateChance = new Random().Next(1, updateFactor + 1);
            if (randomUpdateChance == 1)
            {
                int randomSampleIndex = new Random().Next(0, numOfSamples);
                // Loop through each pixel in the Bitmap
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (binaryMaskArray[currentFrameNum][x, y] == 0)
                            bgModelBuffer[randomSampleIndex, x, y] = GetPixelColor(bmpData, x, y);
                        else
                            return;

                        int randomEightUpdateChance = new Random().Next(1, updateFactor + 1);

                        if (randomEightUpdateChance == 1)
                        {
                            // Choose a random sample value from the 8-neighbourhood
                            Tuple<int, int> randomNeighborOffset = GetRandomNeighborOffset(height, width, x, y);
                            int eightX = randomNeighborOffset.Item1;
                            int eightY = randomNeighborOffset.Item2;
                            Color eightPixel = GetPixelColor(bmpData, eightX, eightY);

                            // Replace value chosen from the bg buffer by pixel value chosen from the 8-neighbourhood
                            bgModelBuffer[randomSampleIndex, eightX, eightY] = eightPixel;
                        }
                    }
                });
            }
        }

        public static Tuple<int, int> GetRandomNeighborOffset(int cols, int rows, int x, int y)
        {
            Random random = new();

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 }; // Relative X-coordinates of the 8 neighbors
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 }; // Relative Y-coordinates of the 8 neighbors

            // Create a list to store valid offsets
            List<int> validOffsets = new List<int>();

            int nx, ny;

            for(int i = 0; i < dx.Length; i++)
            {
                nx = x + dx[i];
                ny = y + dy[i];

                if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
                {
                    validOffsets.Add(i);
                }
            }

            int randomOffsetIndex = random.Next(validOffsets.Count);
            int randomOffset = validOffsets[randomOffsetIndex];

            int rx = x + dx[randomOffset];
            int ry = y + dy[randomOffset];

            return Tuple.Create(rx, ry); 

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

        public bool CameraMoved(BitmapData bitmapData)
        {
            int width = bitmapData.Width;
            int height = bitmapData.Height;

            bool cameraMoved = true;

            Random random = new();
            //Parallel.For(0, numOfPixelsToCompare, i =>
            //{
            for(int i = 0; numOfPixelsToCompare > i; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);

                // Get color from the bitmapData
                Color bitmapColor = GetPixelColor(bitmapData, x, y);

                Color bgColor = bgModelBuffer[0, x, y];

                // Compare with corresponding Color object from bgModel
                if (bitmapColor == bgModelBuffer[0, x, y])
                {
                    cameraMoved = false;
                    break;
                }
            }

            if (cameraMoved)
            {
                Debug.WriteLine("Camera moved");
            }
            return cameraMoved;
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
    }
}