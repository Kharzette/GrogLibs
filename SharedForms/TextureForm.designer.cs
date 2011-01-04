namespace SharedForms
{
	partial class TextureForm
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
			if(disposing && (components != null))
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
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// TextureGrid
			// 
			this.TextureGrid.AllowUserToAddRows = false;
			this.TextureGrid.AllowUserToDeleteRows = false;
			this.TextureGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TextureGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.TextureGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.TextureGrid.Location = new System.Drawing.Point(12, 12);
			this.TextureGrid.MultiSelect = false;
			this.TextureGrid.Name = "TextureGrid";
			this.TextureGrid.ReadOnly = true;
			this.TextureGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.TextureGrid.Size = new System.Drawing.Size(396, 202);
			this.TextureGrid.TabIndex = 1;
			this.TextureGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			// 
			// Ok
			// 
			this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Ok.Location = new System.Drawing.Point(12, 224);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(91, 28);
			this.Ok.TabIndex = 2;
			this.Ok.Text = "Ok";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.OnOk);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Cancel.Location = new System.Drawing.Point(109, 224);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(82, 28);
			this.Cancel.TabIndex = 3;
			this.Cancel.Text = "Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.OnCancel);
			// 
			// TextureForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(420, 264);
			this.ControlBox = false;
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.Controls.Add(this.TextureGrid);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::SharedForms.Settings.Default, "TextureFormPos", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.Location = global::SharedForms.Settings.Default.TextureFormPos;
			this.MaximizeBox = false;
			this.Name = "TextureForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "TextureForm";
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView TextureGrid;
		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
	}
}