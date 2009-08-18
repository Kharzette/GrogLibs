namespace ColladaConvert
{
	partial class MaterialForm
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
			this.MeshPartGrid = new System.Windows.Forms.DataGridView();
			this.ApplyTexture = new System.Windows.Forms.Button();
			this.ShaderGrid = new System.Windows.Forms.DataGridView();
			this.MaterialGrid = new System.Windows.Forms.DataGridView();
			this.ApplyMaterial = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ShaderGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// TextureGrid
			// 
			this.TextureGrid.AllowUserToAddRows = false;
			this.TextureGrid.AllowUserToDeleteRows = false;
			this.TextureGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TextureGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.TextureGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.TextureGrid.Location = new System.Drawing.Point(12, 211);
			this.TextureGrid.MultiSelect = false;
			this.TextureGrid.Name = "TextureGrid";
			this.TextureGrid.ReadOnly = true;
			this.TextureGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.TextureGrid.Size = new System.Drawing.Size(237, 160);
			this.TextureGrid.TabIndex = 0;
			this.TextureGrid.SelectionChanged += new System.EventHandler(this.TextureGrid_SelectionChanged);
			// 
			// MeshPartGrid
			// 
			this.MeshPartGrid.AllowUserToAddRows = false;
			this.MeshPartGrid.AllowUserToDeleteRows = false;
			this.MeshPartGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MeshPartGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MeshPartGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MeshPartGrid.Location = new System.Drawing.Point(12, 12);
			this.MeshPartGrid.Name = "MeshPartGrid";
			this.MeshPartGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MeshPartGrid.Size = new System.Drawing.Size(495, 94);
			this.MeshPartGrid.TabIndex = 1;
			this.MeshPartGrid.SelectionChanged += new System.EventHandler(this.MeshPartGrid_SelectionChanged);
			// 
			// ApplyTexture
			// 
			this.ApplyTexture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyTexture.Location = new System.Drawing.Point(12, 377);
			this.ApplyTexture.Name = "ApplyTexture";
			this.ApplyTexture.Size = new System.Drawing.Size(143, 29);
			this.ApplyTexture.TabIndex = 3;
			this.ApplyTexture.Text = "Apply Tex/Shd to Mat";
			this.ApplyTexture.UseVisualStyleBackColor = true;
			this.ApplyTexture.Click += new System.EventHandler(this.ApplyTexture_Click);
			// 
			// ShaderGrid
			// 
			this.ShaderGrid.AllowUserToAddRows = false;
			this.ShaderGrid.AllowUserToDeleteRows = false;
			this.ShaderGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ShaderGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.ShaderGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ShaderGrid.Location = new System.Drawing.Point(255, 211);
			this.ShaderGrid.MultiSelect = false;
			this.ShaderGrid.Name = "ShaderGrid";
			this.ShaderGrid.ReadOnly = true;
			this.ShaderGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ShaderGrid.Size = new System.Drawing.Size(252, 160);
			this.ShaderGrid.TabIndex = 4;
			this.ShaderGrid.SelectionChanged += new System.EventHandler(this.ShaderGrid_SelectionChanged);
			// 
			// MaterialGrid
			// 
			this.MaterialGrid.AllowUserToAddRows = false;
			this.MaterialGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MaterialGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MaterialGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MaterialGrid.Location = new System.Drawing.Point(12, 112);
			this.MaterialGrid.MultiSelect = false;
			this.MaterialGrid.Name = "MaterialGrid";
			this.MaterialGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MaterialGrid.Size = new System.Drawing.Size(495, 93);
			this.MaterialGrid.TabIndex = 5;
			this.MaterialGrid.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.MaterialGrid_CellValidated);
			this.MaterialGrid.SelectionChanged += new System.EventHandler(this.MaterialGrid_SelectionChanged);
			// 
			// ApplyMaterial
			// 
			this.ApplyMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyMaterial.Location = new System.Drawing.Point(161, 377);
			this.ApplyMaterial.Name = "ApplyMaterial";
			this.ApplyMaterial.Size = new System.Drawing.Size(166, 29);
			this.ApplyMaterial.TabIndex = 6;
			this.ApplyMaterial.Text = "Apply Material To MeshPart";
			this.ApplyMaterial.UseVisualStyleBackColor = true;
			this.ApplyMaterial.Click += new System.EventHandler(this.ApplyMaterial_Click);
			// 
			// MaterialForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(519, 418);
			this.Controls.Add(this.ApplyMaterial);
			this.Controls.Add(this.MaterialGrid);
			this.Controls.Add(this.ShaderGrid);
			this.Controls.Add(this.ApplyTexture);
			this.Controls.Add(this.MeshPartGrid);
			this.Controls.Add(this.TextureGrid);
			this.Name = "MaterialForm";
			this.Text = "MaterialForm";
			((System.ComponentModel.ISupportInitialize)(this.TextureGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ShaderGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView TextureGrid;
		private System.Windows.Forms.DataGridView MeshPartGrid;
		private System.Windows.Forms.Button ApplyTexture;
		private System.Windows.Forms.DataGridView ShaderGrid;
		private System.Windows.Forms.DataGridView MaterialGrid;
		private System.Windows.Forms.Button ApplyMaterial;
	}
}