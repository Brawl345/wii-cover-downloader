namespace WiiCoverDownloader
{
    partial class WiiCoverDownloaderWait
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WiiCoverDownloaderWait));
            this.labelWiiDownloaderWait = new System.Windows.Forms.Label();
            this.labelFirstTime = new System.Windows.Forms.Label();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // labelWiiDownloaderWait
            // 
            this.labelWiiDownloaderWait.AutoSize = true;
            this.labelWiiDownloaderWait.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWiiDownloaderWait.Location = new System.Drawing.Point(13, 23);
            this.labelWiiDownloaderWait.Name = "labelWiiDownloaderWait";
            this.labelWiiDownloaderWait.Size = new System.Drawing.Size(459, 24);
            this.labelWiiDownloaderWait.TabIndex = 1;
            this.labelWiiDownloaderWait.Text = "Please, wait while WiiCoverDownloader startup.";
            this.labelWiiDownloaderWait.UseWaitCursor = true;
            // 
            // labelFirstTime
            // 
            this.labelFirstTime.Font = new System.Drawing.Font("Arial", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFirstTime.Location = new System.Drawing.Point(13, 56);
            this.labelFirstTime.MaximumSize = new System.Drawing.Size(420, 25);
            this.labelFirstTime.MinimumSize = new System.Drawing.Size(420, 25);
            this.labelFirstTime.Name = "labelFirstTime";
            this.labelFirstTime.Size = new System.Drawing.Size(420, 25);
            this.labelFirstTime.TabIndex = 2;
            this.labelFirstTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelFirstTime.UseWaitCursor = true;
            // 
            // progressBar2
            // 
            this.progressBar2.Location = new System.Drawing.Point(-5, 91);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(491, 22);
            this.progressBar2.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar2.TabIndex = 4;
            this.progressBar2.UseWaitCursor = true;
            this.progressBar2.Value = 100;
            // 
            // WiiCoverDownloaderWait
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(482, 105);
            this.ControlBox = false;
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.labelFirstTime);
            this.Controls.Add(this.labelWiiDownloaderWait);
            this.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.MaximumSize = new System.Drawing.Size(500, 150);
            this.MinimumSize = new System.Drawing.Size(500, 150);
            this.Name = "WiiCoverDownloaderWait";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "WiiCoverDownloader";
            this.UseWaitCursor = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelWiiDownloaderWait;
        public System.Windows.Forms.Label labelFirstTime;
        private System.Windows.Forms.ProgressBar progressBar2;
    }
}