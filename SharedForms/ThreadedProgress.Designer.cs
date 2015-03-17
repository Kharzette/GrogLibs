namespace SharedForms
{
	partial class ThreadedProgress
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
			this.Prog = new System.Windows.Forms.ProgressBar();
			this.SuspendLayout();
			// 
			// Prog
			// 
			this.Prog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Prog.Location = new System.Drawing.Point(12, 12);
			this.Prog.Name = "Prog";
			this.Prog.Size = new System.Drawing.Size(424, 23);
			this.Prog.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.Prog.TabIndex = 0;
			// 
			// ThreadedProgress
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(448, 50);
			this.ControlBox = false;
			this.Controls.Add(this.Prog);
			this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.Name = "ThreadedProgress";
			this.Text = "Working...";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ProgressBar Prog;
	}
}