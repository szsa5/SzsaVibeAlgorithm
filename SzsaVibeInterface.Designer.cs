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
            selectFileButton = new Button();
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
            // selectFileButton
            // 
            selectFileButton.Location = new Point(31, 26);
            selectFileButton.Name = "selectFileButton";
            selectFileButton.Size = new Size(112, 43);
            selectFileButton.TabIndex = 4;
            selectFileButton.Text = "Select file";
            selectFileButton.UseVisualStyleBackColor = true;
            selectFileButton.Click += selectFileButton_Click;
            // 
            // SzsaVibeInterface
            // 
            ClientSize = new Size(176, 100);
            Controls.Add(selectFileButton);
            Name = "SzsaVibeInterface";
            ResumeLayout(false);
        }

        #endregion

        private OpenFileDialog openFileDialog1;
        private Button selectInputButton;
        private Label fileNameLabel;
        private Button selectFileButton;
    }
}