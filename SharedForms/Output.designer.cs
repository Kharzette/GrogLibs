namespace SharedForms
{
	partial class Output
	{
		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.BuildProgress = new System.Windows.Forms.ProgressBar();
			this.ConsoleOut = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// BuildProgress
			// 
			this.BuildProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BuildProgress.Location = new System.Drawing.Point(12, 12);
			this.BuildProgress.Name = "BuildProgress";
			this.BuildProgress.Size = new System.Drawing.Size(634, 19);
			this.BuildProgress.TabIndex = 25;
			// 
			// ConsoleOut
			// 
			this.ConsoleOut.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ConsoleOut.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ConsoleOut.Location = new System.Drawing.Point(12, 37);
			this.ConsoleOut.Multiline = true;
			this.ConsoleOut.Name = "ConsoleOut";
			this.ConsoleOut.ReadOnly = true;
			this.ConsoleOut.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.ConsoleOut.Size = new System.Drawing.Size(634, 266);
			this.ConsoleOut.TabIndex = 26;
			this.ConsoleOut.TabStop = false;
			// 
			// Output
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(658, 315);
			this.ControlBox = false;
			this.Controls.Add(this.ConsoleOut);
			this.Controls.Add(this.BuildProgress);
			this.Name = "Output";
			this.Text = "Output";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar BuildProgress;
		private System.Windows.Forms.TextBox ConsoleOut;
	}
}