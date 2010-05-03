namespace ColladaConvert
{
	partial class TechniqueList
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
			this.TechniqueGrid = new System.Windows.Forms.DataGridView();
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.TechniqueGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// TechniqueGrid
			// 
			this.TechniqueGrid.AllowUserToAddRows = false;
			this.TechniqueGrid.AllowUserToDeleteRows = false;
			this.TechniqueGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TechniqueGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.TechniqueGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.TechniqueGrid.Location = new System.Drawing.Point(12, 12);
			this.TechniqueGrid.MultiSelect = false;
			this.TechniqueGrid.Name = "TechniqueGrid";
			this.TechniqueGrid.ReadOnly = true;
			this.TechniqueGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.TechniqueGrid.Size = new System.Drawing.Size(366, 206);
			this.TechniqueGrid.TabIndex = 6;
			this.TechniqueGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			// 
			// Ok
			// 
			this.Ok.Location = new System.Drawing.Point(12, 224);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(91, 28);
			this.Ok.TabIndex = 7;
			this.Ok.Text = "Ok";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.OnOk);
			// 
			// Cancel
			// 
			this.Cancel.Location = new System.Drawing.Point(109, 224);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(82, 28);
			this.Cancel.TabIndex = 8;
			this.Cancel.Text = "Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.OnCancel);
			// 
			// TechniqueList
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(390, 264);
			this.ControlBox = false;
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.Controls.Add(this.TechniqueGrid);
			this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::ColladaConvert.Properties.Settings.Default, "TechniqueListPos", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.Location = global::ColladaConvert.Properties.Settings.Default.TechniqueListPos;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TechniqueList";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "TechniqueList";
			((System.ComponentModel.ISupportInitialize)(this.TechniqueGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView TechniqueGrid;
		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
	}
}