﻿namespace SharedForms
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
			this.components = new System.ComponentModel.Container();
			this.NewMaterial = new System.Windows.Forms.Button();
			this.MaterialGrid = new System.Windows.Forms.DataGridView();
			this.MaterialProperties = new System.Windows.Forms.DataGridView();
			this.SaveButton = new System.Windows.Forms.Button();
			this.LoadButton = new System.Windows.Forms.Button();
			this.RefreshShaders = new System.Windows.Forms.Button();
			this.GuessTextures = new System.Windows.Forms.Button();
			this.TexSizeUp = new System.Windows.Forms.Button();
			this.TexSizeDown = new System.Windows.Forms.Button();
			this.GetEmissive = new System.Windows.Forms.Button();
			this.FileGroup = new System.Windows.Forms.GroupBox();
			this.MergeMaterialLib = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.MeshPartGrid = new System.Windows.Forms.DataGridView();
			this.ApplyMaterial = new System.Windows.Forms.Button();
			this.MeshPartGroup = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.TexCoordSet = new System.Windows.Forms.NumericUpDown();
			this.GenBiNormalTangent = new System.Windows.Forms.Button();
			this.mTips = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialProperties)).BeginInit();
			this.FileGroup.SuspendLayout();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).BeginInit();
			this.MeshPartGroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).BeginInit();
			this.SuspendLayout();
			// 
			// NewMaterial
			// 
			this.NewMaterial.Location = new System.Drawing.Point(6, 19);
			this.NewMaterial.Name = "NewMaterial";
			this.NewMaterial.Size = new System.Drawing.Size(94, 29);
			this.NewMaterial.TabIndex = 3;
			this.NewMaterial.Text = "New Material";
			this.mTips.SetToolTip(this.NewMaterial, "Create a new material");
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
			this.MaterialGrid.Size = new System.Drawing.Size(1480, 120);
			this.MaterialGrid.TabIndex = 5;
			this.MaterialGrid.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnCellClick);
			this.MaterialGrid.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellValidated);
			this.MaterialGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			this.MaterialGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.OnNukeMaterial);
			// 
			// MaterialProperties
			// 
			this.MaterialProperties.AllowUserToAddRows = false;
			this.MaterialProperties.AllowUserToDeleteRows = false;
			this.MaterialProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MaterialProperties.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MaterialProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MaterialProperties.Location = new System.Drawing.Point(12, 12);
			this.MaterialProperties.Name = "MaterialProperties";
			this.MaterialProperties.Size = new System.Drawing.Size(1480, 196);
			this.MaterialProperties.TabIndex = 7;
			this.MaterialProperties.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnPropCellClick);
			this.MaterialProperties.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnPropValueValidated);
			// 
			// SaveButton
			// 
			this.SaveButton.Location = new System.Drawing.Point(160, 19);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(46, 29);
			this.SaveButton.TabIndex = 8;
			this.SaveButton.Text = "Save";
			this.mTips.SetToolTip(this.SaveButton, "Save a material library");
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.OnSave);
			// 
			// LoadButton
			// 
			this.LoadButton.Location = new System.Drawing.Point(106, 19);
			this.LoadButton.Name = "LoadButton";
			this.LoadButton.Size = new System.Drawing.Size(48, 29);
			this.LoadButton.TabIndex = 9;
			this.LoadButton.Text = "Load";
			this.mTips.SetToolTip(this.LoadButton, "Load a material library");
			this.LoadButton.UseVisualStyleBackColor = true;
			this.LoadButton.Click += new System.EventHandler(this.OnLoad);
			// 
			// RefreshShaders
			// 
			this.RefreshShaders.Location = new System.Drawing.Point(6, 19);
			this.RefreshShaders.Name = "RefreshShaders";
			this.RefreshShaders.Size = new System.Drawing.Size(60, 29);
			this.RefreshShaders.TabIndex = 12;
			this.RefreshShaders.Text = "Refresh";
			this.mTips.SetToolTip(this.RefreshShaders, "Refresh shader values");
			this.RefreshShaders.UseVisualStyleBackColor = true;
			this.RefreshShaders.Click += new System.EventHandler(this.OnRefreshShaders);
			// 
			// GuessTextures
			// 
			this.GuessTextures.Location = new System.Drawing.Point(72, 19);
			this.GuessTextures.Name = "GuessTextures";
			this.GuessTextures.Size = new System.Drawing.Size(94, 29);
			this.GuessTextures.TabIndex = 13;
			this.GuessTextures.Text = "Guess Textures";
			this.mTips.SetToolTip(this.GuessTextures, "Attempts to match materials up to textures loaded in the content pipeline");
			this.GuessTextures.UseVisualStyleBackColor = true;
			this.GuessTextures.Click += new System.EventHandler(this.OnGuessTextures);
			// 
			// TexSizeUp
			// 
			this.TexSizeUp.Location = new System.Drawing.Point(172, 19);
			this.TexSizeUp.Name = "TexSizeUp";
			this.TexSizeUp.Size = new System.Drawing.Size(75, 29);
			this.TexSizeUp.TabIndex = 14;
			this.TexSizeUp.Text = "TexSize Up";
			this.mTips.SetToolTip(this.TexSizeUp, "Increase material tex size, hold shift for all materials");
			this.TexSizeUp.UseVisualStyleBackColor = true;
			this.TexSizeUp.Click += new System.EventHandler(this.OnTexSizeUp);
			// 
			// TexSizeDown
			// 
			this.TexSizeDown.Location = new System.Drawing.Point(253, 19);
			this.TexSizeDown.Name = "TexSizeDown";
			this.TexSizeDown.Size = new System.Drawing.Size(89, 29);
			this.TexSizeDown.TabIndex = 15;
			this.TexSizeDown.Text = "TexSize Down";
			this.mTips.SetToolTip(this.TexSizeDown, "Decrease material tex size, hold shift for all materials");
			this.TexSizeDown.UseVisualStyleBackColor = true;
			this.TexSizeDown.Click += new System.EventHandler(this.OnTexSizeDown);
			// 
			// GetEmissive
			// 
			this.GetEmissive.Location = new System.Drawing.Point(348, 19);
			this.GetEmissive.Name = "GetEmissive";
			this.GetEmissive.Size = new System.Drawing.Size(89, 29);
			this.GetEmissive.TabIndex = 16;
			this.GetEmissive.Text = "Get Emissive";
			this.mTips.SetToolTip(this.GetEmissive, "Average textures down to a single color for light emission (if the material does)" +
        "");
			this.GetEmissive.UseVisualStyleBackColor = true;
			this.GetEmissive.Click += new System.EventHandler(this.OnGetEmissive);
			// 
			// FileGroup
			// 
			this.FileGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.FileGroup.Controls.Add(this.MergeMaterialLib);
			this.FileGroup.Controls.Add(this.NewMaterial);
			this.FileGroup.Controls.Add(this.LoadButton);
			this.FileGroup.Controls.Add(this.SaveButton);
			this.FileGroup.Location = new System.Drawing.Point(12, 492);
			this.FileGroup.Name = "FileGroup";
			this.FileGroup.Size = new System.Drawing.Size(277, 54);
			this.FileGroup.TabIndex = 17;
			this.FileGroup.TabStop = false;
			this.FileGroup.Text = "File IO";
			// 
			// MergeMaterialLib
			// 
			this.MergeMaterialLib.Location = new System.Drawing.Point(212, 19);
			this.MergeMaterialLib.Name = "MergeMaterialLib";
			this.MergeMaterialLib.Size = new System.Drawing.Size(57, 29);
			this.MergeMaterialLib.TabIndex = 10;
			this.MergeMaterialLib.Text = "Merge";
			this.mTips.SetToolTip(this.MergeMaterialLib, "Merge another material lib into the existing material data");
			this.MergeMaterialLib.UseVisualStyleBackColor = true;
			this.MergeMaterialLib.Click += new System.EventHandler(this.OnMergeMaterialLib);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.RefreshShaders);
			this.groupBox1.Controls.Add(this.GuessTextures);
			this.groupBox1.Controls.Add(this.GetEmissive);
			this.groupBox1.Controls.Add(this.TexSizeUp);
			this.groupBox1.Controls.Add(this.TexSizeDown);
			this.groupBox1.Location = new System.Drawing.Point(295, 492);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(445, 54);
			this.groupBox1.TabIndex = 18;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Shader Shtuff";
			// 
			// MeshPartGrid
			// 
			this.MeshPartGrid.AllowUserToAddRows = false;
			this.MeshPartGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.MeshPartGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.MeshPartGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MeshPartGrid.Location = new System.Drawing.Point(12, 214);
			this.MeshPartGrid.Name = "MeshPartGrid";
			this.MeshPartGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MeshPartGrid.Size = new System.Drawing.Size(1480, 146);
			this.MeshPartGrid.TabIndex = 19;
			this.MeshPartGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.OnMeshPartNuking);
			// 
			// ApplyMaterial
			// 
			this.ApplyMaterial.Location = new System.Drawing.Point(6, 19);
			this.ApplyMaterial.Name = "ApplyMaterial";
			this.ApplyMaterial.Size = new System.Drawing.Size(95, 29);
			this.ApplyMaterial.TabIndex = 20;
			this.ApplyMaterial.Text = "Apply Material";
			this.mTips.SetToolTip(this.ApplyMaterial, "Apply selected material to the selected mesh part");
			this.ApplyMaterial.UseVisualStyleBackColor = true;
			this.ApplyMaterial.Click += new System.EventHandler(this.OnApplyMaterial);
			// 
			// MeshPartGroup
			// 
			this.MeshPartGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MeshPartGroup.Controls.Add(this.label1);
			this.MeshPartGroup.Controls.Add(this.TexCoordSet);
			this.MeshPartGroup.Controls.Add(this.GenBiNormalTangent);
			this.MeshPartGroup.Controls.Add(this.ApplyMaterial);
			this.MeshPartGroup.Location = new System.Drawing.Point(746, 492);
			this.MeshPartGroup.Name = "MeshPartGroup";
			this.MeshPartGroup.Size = new System.Drawing.Size(299, 54);
			this.MeshPartGroup.TabIndex = 21;
			this.MeshPartGroup.TabStop = false;
			this.MeshPartGroup.Text = "Mesh Part";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(232, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(23, 13);
			this.label1.TabIndex = 22;
			this.label1.Text = "Set";
			// 
			// TexCoordSet
			// 
			this.TexCoordSet.Location = new System.Drawing.Point(223, 28);
			this.TexCoordSet.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
			this.TexCoordSet.Name = "TexCoordSet";
			this.TexCoordSet.Size = new System.Drawing.Size(42, 20);
			this.TexCoordSet.TabIndex = 22;
			this.mTips.SetToolTip(this.TexCoordSet, "Texture coordinate set to use when generating bitangents");
			// 
			// GenBiNormalTangent
			// 
			this.GenBiNormalTangent.Location = new System.Drawing.Point(107, 19);
			this.GenBiNormalTangent.Name = "GenBiNormalTangent";
			this.GenBiNormalTangent.Size = new System.Drawing.Size(110, 29);
			this.GenBiNormalTangent.TabIndex = 21;
			this.GenBiNormalTangent.Text = "Gen Tangents";
			this.mTips.SetToolTip(this.GenBiNormalTangent, "Generate bitangents for normal mapping for the selected mesh part");
			this.GenBiNormalTangent.UseVisualStyleBackColor = true;
			this.GenBiNormalTangent.Click += new System.EventHandler(this.OnGenBiNormalTangent);
			// 
			// MaterialForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1504, 554);
			this.ControlBox = false;
			this.Controls.Add(this.MeshPartGroup);
			this.Controls.Add(this.MeshPartGrid);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.FileGroup);
			this.Controls.Add(this.MaterialProperties);
			this.Controls.Add(this.MaterialGrid);
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.Name = "MaterialForm";
			this.ShowIcon = false;
			this.Text = "MaterialForm";
			this.SizeChanged += new System.EventHandler(this.OnSizeChanged);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
			((System.ComponentModel.ISupportInitialize)(this.MaterialGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaterialProperties)).EndInit();
			this.FileGroup.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.MeshPartGrid)).EndInit();
			this.MeshPartGroup.ResumeLayout(false);
			this.MeshPartGroup.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button NewMaterial;
		private System.Windows.Forms.DataGridView MaterialGrid;
		private System.Windows.Forms.DataGridView MaterialProperties;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.Button LoadButton;
		private System.Windows.Forms.Button RefreshShaders;
		private System.Windows.Forms.Button GuessTextures;
		private System.Windows.Forms.Button TexSizeUp;
		private System.Windows.Forms.Button TexSizeDown;
		private System.Windows.Forms.Button GetEmissive;
		private System.Windows.Forms.GroupBox FileGroup;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.DataGridView MeshPartGrid;
		private System.Windows.Forms.Button ApplyMaterial;
		private System.Windows.Forms.GroupBox MeshPartGroup;
		private System.Windows.Forms.Button GenBiNormalTangent;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown TexCoordSet;
		private System.Windows.Forms.ToolTip mTips;
		private System.Windows.Forms.Button MergeMaterialLib;
	}
}