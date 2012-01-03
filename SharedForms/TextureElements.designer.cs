namespace SharedForms
{
	partial class TextureElements
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
			this.TextureGrid = new System.Windows.Forms.DataGridView();
			this.LoadButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// TextureGrid
			// 
			this.TextureGrid.AllowUserToAddRows = false;
			this.TextureGrid.AllowUserToDeleteRows = false;
			this.TextureGrid.AllowUserToResizeRows = false;
			this.TextureGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TextureGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.TextureGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.TextureGrid.Location = new System.Drawing.Point(12, 12);
			this.TextureGrid.MultiSelect = false;
			this.TextureGrid.Name = "TextureGrid";
			this.TextureGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.TextureGrid.Size = new System.Drawing.Size(527, 239);
			this.TextureGrid.TabIndex = 7;
			this.TextureGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			// 
			// LoadButton
			// 
			this.LoadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadButton.Location = new System.Drawing.Point(12, 257);
			this.LoadButton.Name = "LoadButton";
			this.LoadButton.Size = new System.Drawing.Size(75, 23);
			this.LoadButton.TabIndex = 8;
			this.LoadButton.Text = "Load";
			this.LoadButton.UseVisualStyleBackColor = true;
			this.LoadButton.Click += new System.EventHandler(this.OnLoad);
			// 
			// TextureElements
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(551, 292);
			this.ControlBox = false;
			this.Controls.Add(this.LoadButton);
			this.Controls.Add(this.TextureGrid);
			this.Name = "TextureElements";
			this.Text = "Texture Lib";
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView TextureGrid;
		private System.Windows.Forms.Button LoadButton;
	}
}