namespace Quoridor1
{
    partial class Form1
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
            pictureBox1 = new PictureBox();
            リセット = new Button();
            btnBenchmark = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = SystemColors.GradientActiveCaption;
            pictureBox1.Location = new Point(346, 37);
            pictureBox1.Margin = new Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(800, 800);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.MouseUp += pictureBox1_MouseUp;
            // 
            // リセット
            // 
            リセット.Location = new Point(1205, 55);
            リセット.Name = "リセット";
            リセット.Size = new Size(112, 34);
            リセット.TabIndex = 1;
            リセット.Text = "リセット";
            リセット.UseVisualStyleBackColor = true;
            リセット.Click += リセット_Click;
            // 
            // btnBenchmark
            // 
            btnBenchmark.Location = new Point(1205, 127);
            btnBenchmark.Name = "btnBenchmark";
            btnBenchmark.Size = new Size(112, 34);
            btnBenchmark.TabIndex = 2;
            btnBenchmark.Text = "100戦";
            btnBenchmark.UseVisualStyleBackColor = true;
            btnBenchmark.Click += btnBenchmark_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1456, 1050);
            Controls.Add(btnBenchmark);
            Controls.Add(リセット);
            Controls.Add(pictureBox1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private Button リセット;
        private Button btnBenchmark;
    }
}
