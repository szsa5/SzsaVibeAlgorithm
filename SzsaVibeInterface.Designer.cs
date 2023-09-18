namespace SzsaVibeAlgorithm
{
    partial class SzsaVibeInterface
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            openFileDialog1 = new OpenFileDialog();
            selectInputButton = new Button();
            fileNameLabel = new Label();
            btnStop = new Button();
            btnPlay = new Button();
            pictureBox1 = new PictureBox();
            selectFileButton = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // selectInputButton
            // 
            selectInputButton.Location = new Point(153, 92);
            selectInputButton.Name = "selectInputButton";
            selectInputButton.Size = new Size(137, 23);
            selectInputButton.TabIndex = 0;
            selectInputButton.Text = "Select input video";
            selectInputButton.UseVisualStyleBackColor = true;
            // 
            // fileNameLabel
            // 
            fileNameLabel.Location = new Point(0, 0);
            fileNameLabel.Name = "fileNameLabel";
            fileNameLabel.Size = new Size(100, 23);
            fileNameLabel.TabIndex = 0;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(567, 448);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(88, 43);
            btnStop.TabIndex = 0;
            btnStop.Text = "stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += BtnStop_Click_1;
            // 
            // btnPlay
            // 
            btnPlay.Location = new Point(426, 448);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(90, 43);
            btnPlay.TabIndex = 1;
            btnPlay.Text = "play";
            btnPlay.UseMnemonic = false;
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += BtnPlay_Click_1;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(205, 51);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(649, 368);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // selectFileButton
            // 
            selectFileButton.Location = new Point(53, 455);
            selectFileButton.Name = "selectFileButton";
            selectFileButton.Size = new Size(112, 43);
            selectFileButton.TabIndex = 4;
            selectFileButton.Text = "Select file";
            selectFileButton.UseVisualStyleBackColor = true;
            selectFileButton.Click += selectFileButton_Click;
            // 
            // SzsaVibeInterface
            // 
            ClientSize = new Size(1009, 542);
            Controls.Add(selectFileButton);
            Controls.Add(pictureBox1);
            Controls.Add(btnPlay);
            Controls.Add(btnStop);
            Name = "SzsaVibeInterface";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private OpenFileDialog openFileDialog1;
        private Button selectInputButton;
        private Label fileNameLabel;
        private Button btnStop;
        private Button btnPlay;
        private PictureBox pictureBox1;
        private Button selectFileButton;
    }
}