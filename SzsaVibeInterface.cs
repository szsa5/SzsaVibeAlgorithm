using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace SzsaVibeAlgorithm
{
    public partial class SzsaVibeInterface : Form
    {
        VideoCapture capture;
        bool isPlaying = false;
        string curFileName = String.Empty;
        int currentFrameNum;
        Mat currentFrame;
        int totalFrames;
        int fps;
        int width;
        int height;
        double curProgressPercent;

        const sbyte numOfSamples = 20;
        const sbyte radius = 20;
        const sbyte minCardinality = 2;
        const sbyte updateFactor = 5;

        const sbyte maxNoise = 3;
        const double pixelPercent = 0.02;

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

                    // Check for camera movement
                    if (isFirstFrame)
                        bgModelBuffer = InitBgModels(bmpData);
                    else
                        if (CameraMoved(bmpData))
                            bgModelBuffer = InitBgModels(bmpData);

                    // Print progress
                    curProgressPercent = (currentFrameNum + 1) / (double)totalFrames * 100;
                    Debug.WriteLine($"Classifying frame {currentFrameNum+1} / {totalFrames} " +
                                    $"({Math.Round(curProgressPercent, 2)}%)");

                    // Classify frame into binary mask
                    int[,] binaryMask = ClassifyFrame(bmpData);
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
            using VideoWriter writer = new VideoWriter($"{curFileName.Split('.')[0]}_vibe_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.mp4", VideoWriter.Fourcc('X', '2', '6', '4'), fps, new Size(width, height), true);

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
                for (int x = 0; x < width; x++)
                {
                    int curMemPos = 0;
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


                            // Swap matches to first pos. of buffer
                            Color swap = bgModelBuffer[curMemPos, x, y];
                            bgModelBuffer[curMemPos, x, y] = bgModelPixel;
                            bgModelBuffer[i, x, y] = swap;
                            curMemPos += 1;

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
            int randomUpdateChance = Random.Shared.Next(1, updateFactor + 1);
            if (randomUpdateChance == 1)
            {
                // Loop through each pixel in the Bitmap
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (binaryMaskArray[currentFrameNum][x, y] == 0) {
                            int randomSampleIndex = Random.Shared.Next(0, numOfSamples);
                            bgModelBuffer[randomSampleIndex, x, y] = GetPixelColor(bmpData, x, y);
                        
                            int randomEightUpdateChance = Random.Shared.Next(1, updateFactor + 1);
                            if (randomEightUpdateChance == 1)
                            {
                                // Choose a random sample value from the 8-neighbourhood
                                Tuple<int, int> randomNeighborOffset = GetRandomNeighborOffset(height, width, x, y);
                                int eightX = randomNeighborOffset.Item1;
                                int eightY = randomNeighborOffset.Item2;
                                Color eightPixel = GetPixelColor(bmpData, eightX, eightY);

                                randomSampleIndex = Random.Shared.Next(0, numOfSamples);
                                // Replace value chosen from the bg buffer by pixel value chosen from the 8-neighbourhood
                                bgModelBuffer[randomSampleIndex, eightX, eightY] = eightPixel;
                            }
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

            for (int i = 0; i < dx.Length; i++)
            {
                nx = x + dx[i];
                ny = y + dy[i];

                if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
                {
                    validOffsets.Add(i);
                }
            }

            int randomOffsetIndex = Random.Shared.Next(validOffsets.Count);
            int randomOffset = validOffsets[randomOffsetIndex];

            int rx = x + dx[randomOffset];
            int ry = y + dy[randomOffset];

            return Tuple.Create(rx, ry);

        }

        public static Color AddNoiseToColor(Color color)
        {

            Random random = new();
            int red = color.R;
            int green = color.G;
            int blue = color.B;

            if (Random.Shared.Next(2) == 0)
            {
                // Add random noise to each channel
                red += Random.Shared.Next(-maxNoise, maxNoise + 1);
                green += Random.Shared.Next(-maxNoise, maxNoise + 1);
                blue += Random.Shared.Next(-maxNoise, maxNoise + 1);

                // Make sure the color values stay within the valid range of 0-255
                red = Math.Max(0, Math.Min(255, red));
                green = Math.Max(0, Math.Min(255, green));
                blue = Math.Max(0, Math.Min(255, blue));
            }

            return Color.FromArgb(color.A, red, green, blue);
        }

        public bool CameraMoved(BitmapData bitmapData)
        {
            int stepx = (int)(width * pixelPercent);
            int stepy = (int)(height * pixelPercent);

            for(int y = 0; y < height; y+=stepy)
            {
                for (int x = 0; x < width; x+=stepx)
                {
                    // Get color from the bitmapData
                    Color bitmapColor = GetPixelColor(bitmapData, x, y);

                    // Compare with corresponding Color object from bgModel
                    if (bitmapColor == bgModelBuffer[0, x, y])
                    {
                        return false;
                    }
                }
            }

            Debug.WriteLine($"Camera moved at frame {currentFrameNum}, time: {Math.Round((curProgressPercent/100)*(totalFrames/fps),2)}s");
            return true;
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
                curFileName = ofd.FileName;
                PlayVideo();
            }
        }
    }
}