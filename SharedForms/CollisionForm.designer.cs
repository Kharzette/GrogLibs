namespace SharedForms
{
	partial class CollisionForm
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
            this.StartRay = new System.Windows.Forms.Button();
            this.EndRay = new System.Windows.Forms.Button();
            this.ConsoleOut = new System.Windows.Forms.TextBox();
            this.RepeatRay = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StartRay
            // 
            this.StartRay.Location = new System.Drawing.Point(12, 12);
            this.StartRay.Name = "StartRay";
            this.StartRay.Size = new System.Drawing.Size(75, 23);
            this.StartRay.TabIndex = 0;
            this.StartRay.Text = "Start Ray";
            this.StartRay.UseVisualStyleBackColor = true;
            this.StartRay.Click += new System.EventHandler(this.OnStartRay);
            // 
            // EndRay
            // 
            this.EndRay.Location = new System.Drawing.Point(12, 41);
            this.EndRay.Name = "EndRay";
            this.EndRay.Size = new System.Drawing.Size(75, 23);
            this.EndRay.TabIndex = 1;
            this.EndRay.Text = "End Ray";
            this.EndRay.UseVisualStyleBackColor = true;
            this.EndRay.Click += new System.EventHandler(this.OnEndRay);
            // 
            // ConsoleOut
            // 
            this.ConsoleOut.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ConsoleOut.Location = new System.Drawing.Point(12, 99);
            this.ConsoleOut.Multiline = true;
            this.ConsoleOut.Name = "ConsoleOut";
            this.ConsoleOut.ReadOnly = true;
            this.ConsoleOut.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ConsoleOut.Size = new System.Drawing.Size(260, 153);
            this.ConsoleOut.TabIndex = 17;
            // 
            // RepeatRay
            // 
            this.RepeatRay.Location = new System.Drawing.Point(93, 12);
            this.RepeatRay.Name = "RepeatRay";
            this.RepeatRay.Size = new System.Drawing.Size(75, 23);
            this.RepeatRay.TabIndex = 18;
            this.RepeatRay.Text = "Repeat Ray";
            this.RepeatRay.UseVisualStyleBackColor = true;
            this.RepeatRay.Click += new System.EventHandler(this.OnRepeatRay);
            // 
            // CollisionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.Controls.Add(this.RepeatRay);
            this.Controls.Add(this.ConsoleOut);
            this.Controls.Add(this.EndRay);
            this.Controls.Add(this.StartRay);
            this.Name = "CollisionForm";
            this.Text = "CollisionForm";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button StartRay;
		private System.Windows.Forms.Button EndRay;
		private System.Windows.Forms.TextBox ConsoleOut;
		private System.Windows.Forms.Button RepeatRay;
	}
}