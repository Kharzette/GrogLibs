namespace SharedForms
{
	partial class CelTweakForm
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
			this.CelTweakGrid = new System.Windows.Forms.DataGridView();
			this.ApplyShading = new System.Windows.Forms.Button();
			this.TextureSize = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.CelTweakGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TextureSize)).BeginInit();
			this.SuspendLayout();
			// 
			// CelTweakGrid
			// 
			this.CelTweakGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CelTweakGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.CelTweakGrid.Location = new System.Drawing.Point(12, 12);
			this.CelTweakGrid.Name = "CelTweakGrid";
			this.CelTweakGrid.Size = new System.Drawing.Size(260, 208);
			this.CelTweakGrid.TabIndex = 0;
			// 
			// ApplyShading
			// 
			this.ApplyShading.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyShading.Location = new System.Drawing.Point(13, 226);
			this.ApplyShading.Name = "ApplyShading";
			this.ApplyShading.Size = new System.Drawing.Size(75, 23);
			this.ApplyShading.TabIndex = 1;
			this.ApplyShading.Text = "Apply";
			this.ApplyShading.UseVisualStyleBackColor = true;
			this.ApplyShading.Click += new System.EventHandler(this.OnApplyShading);
			// 
			// TextureSize
			// 
			this.TextureSize.Location = new System.Drawing.Point(224, 226);
			this.TextureSize.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
			this.TextureSize.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.TextureSize.Name = "TextureSize";
			this.TextureSize.Size = new System.Drawing.Size(48, 20);
			this.TextureSize.TabIndex = 2;
			this.TextureSize.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.TextureSize.ValueChanged += new System.EventHandler(this.OnTextureSizeChanged);
			// 
			// CelTweakForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.ControlBox = false;
			this.Controls.Add(this.TextureSize);
			this.Controls.Add(this.ApplyShading);
			this.Controls.Add(this.CelTweakGrid);
			this.Name = "CelTweakForm";
			this.Text = "Cel Tweak";
			((System.ComponentModel.ISupportInitialize)(this.CelTweakGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TextureSize)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView CelTweakGrid;
		private System.Windows.Forms.Button ApplyShading;
		private System.Windows.Forms.NumericUpDown TextureSize;
	}
}