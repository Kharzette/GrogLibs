﻿namespace ColladaConvert
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
			this.MeshPartGrid = new System.Windows.Forms.DataGridView();
			this.NewMaterial = new System.Windows.Forms.Button();
			this.MaterialGrid = new System.Windows.Forms.DataGridView();
			this.ApplyMaterial = new System.Windows.Forms.Button();
			this.MaterialProperties = new System.Windows.Forms.DataGridView();
			this.SaveButton = new System.Windows.Forms.Button();
			this.LoadButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialProperties)).BeginInit();
			this.SuspendLayout();
			// 
			// MeshPartGrid
			// 
			this.MeshPartGrid.AllowUserToAddRows = false;
			this.MeshPartGrid.AllowUserToDeleteRows = false;
			this.MeshPartGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.MeshPartGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MeshPartGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MeshPartGrid.Location = new System.Drawing.Point(12, 214);
			this.MeshPartGrid.Name = "MeshPartGrid";
			this.MeshPartGrid.ReadOnly = true;
			this.MeshPartGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MeshPartGrid.Size = new System.Drawing.Size(495, 146);
			this.MeshPartGrid.TabIndex = 1;
			this.MeshPartGrid.SelectionChanged += new System.EventHandler(this.MeshPartGrid_SelectionChanged);
			// 
			// NewMaterial
			// 
			this.NewMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.NewMaterial.Location = new System.Drawing.Point(12, 532);
			this.NewMaterial.Name = "NewMaterial";
			this.NewMaterial.Size = new System.Drawing.Size(94, 29);
			this.NewMaterial.TabIndex = 3;
			this.NewMaterial.Text = "New Material";
			this.NewMaterial.UseVisualStyleBackColor = true;
			this.NewMaterial.Click += new System.EventHandler(this.OnNewMaterial);
			// 
			// MaterialGrid
			// 
			this.MaterialGrid.AllowUserToAddRows = false;
			this.MaterialGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MaterialGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MaterialGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MaterialGrid.Location = new System.Drawing.Point(12, 366);
			this.MaterialGrid.MultiSelect = false;
			this.MaterialGrid.Name = "MaterialGrid";
			this.MaterialGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MaterialGrid.Size = new System.Drawing.Size(495, 160);
			this.MaterialGrid.TabIndex = 5;
			this.MaterialGrid.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnCellClick);
			this.MaterialGrid.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellValidated);
			this.MaterialGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			// 
			// ApplyMaterial
			// 
			this.ApplyMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyMaterial.Location = new System.Drawing.Point(353, 532);
			this.ApplyMaterial.Name = "ApplyMaterial";
			this.ApplyMaterial.Size = new System.Drawing.Size(154, 29);
			this.ApplyMaterial.TabIndex = 6;
			this.ApplyMaterial.Text = "Apply Material To MeshPart";
			this.ApplyMaterial.UseVisualStyleBackColor = true;
			this.ApplyMaterial.Click += new System.EventHandler(this.OnApplyMaterial);
			// 
			// MaterialProperties
			// 
			this.MaterialProperties.AllowUserToAddRows = false;
			this.MaterialProperties.AllowUserToDeleteRows = false;
			this.MaterialProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MaterialProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MaterialProperties.Location = new System.Drawing.Point(12, 12);
			this.MaterialProperties.Name = "MaterialProperties";
			this.MaterialProperties.Size = new System.Drawing.Size(495, 196);
			this.MaterialProperties.TabIndex = 7;
			this.MaterialProperties.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnPropValueValidated);
			this.MaterialProperties.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnPropCellClick);
			// 
			// Save
			// 
			this.SaveButton.Location = new System.Drawing.Point(112, 532);
			this.SaveButton.Name = "Save";
			this.SaveButton.Size = new System.Drawing.Size(46, 29);
			this.SaveButton.TabIndex = 8;
			this.SaveButton.Text = "Save";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.OnSave);
			// 
			// Load
			// 
			this.LoadButton.Location = new System.Drawing.Point(164, 532);
			this.LoadButton.Name = "Load";
			this.LoadButton.Size = new System.Drawing.Size(48, 29);
			this.LoadButton.TabIndex = 9;
			this.LoadButton.Text = "Load";
			this.LoadButton.UseVisualStyleBackColor = true;
			this.LoadButton.Click += new System.EventHandler(this.OnLoad);
			// 
			// MaterialForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(519, 573);
			this.ControlBox = false;
			this.Controls.Add(this.LoadButton);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.MaterialProperties);
			this.Controls.Add(this.ApplyMaterial);
			this.Controls.Add(this.MaterialGrid);
			this.Controls.Add(this.NewMaterial);
			this.Controls.Add(this.MeshPartGrid);
			this.MaximizeBox = false;
			this.Name = "MaterialForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "MaterialForm";
			this.SizeChanged += new System.EventHandler(this.OnSizeChanged);
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialProperties)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView MeshPartGrid;
		private System.Windows.Forms.Button NewMaterial;
		private System.Windows.Forms.DataGridView MaterialGrid;
		private System.Windows.Forms.Button ApplyMaterial;
		private System.Windows.Forms.DataGridView MaterialProperties;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.Button LoadButton;
	}
}