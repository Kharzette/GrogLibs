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
			this.MaterialList = new System.Windows.Forms.ListView();
			this.VariableList = new System.Windows.Forms.DataGridView();
			this.FileGroup = new System.Windows.Forms.GroupBox();
			this.MergeMaterialLib = new System.Windows.Forms.Button();
			this.NewMaterial = new System.Windows.Forms.Button();
			this.LoadButton = new System.Windows.Forms.Button();
			this.SaveButton = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.GuessShaderParameterVisibility = new System.Windows.Forms.Button();
			this.UnHideAll = new System.Windows.Forms.Button();
			this.IgnoreParameter = new System.Windows.Forms.Button();
			this.HideSelected = new System.Windows.Forms.Button();
			this.GuessTextures = new System.Windows.Forms.Button();
			this.TexSizeUp = new System.Windows.Forms.Button();
			this.TexSizeDown = new System.Windows.Forms.Button();
			this.MeshPartGroup = new System.Windows.Forms.GroupBox();
			this.WeldWeight = new System.Windows.Forms.Button();
			this.StripElements = new System.Windows.Forms.Button();
			this.Match = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.TexCoordSet = new System.Windows.Forms.NumericUpDown();
			this.GenBiNormalTangent = new System.Windows.Forms.Button();
			this.ApplyMaterial = new System.Windows.Forms.Button();
			this.MeshPartList = new System.Windows.Forms.ListView();
			((System.ComponentModel.ISupportInitialize)(this.VariableList)).BeginInit();
			this.FileGroup.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.MeshPartGroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).BeginInit();
			this.SuspendLayout();
			// 
			// MaterialList
			// 
			this.MaterialList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MaterialList.FullRowSelect = true;
			this.MaterialList.GridLines = true;
			this.MaterialList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.MaterialList.HideSelection = false;
			this.MaterialList.LabelEdit = true;
			this.MaterialList.Location = new System.Drawing.Point(12, 414);
			this.MaterialList.MultiSelect = false;
			this.MaterialList.Name = "MaterialList";
			this.MaterialList.Size = new System.Drawing.Size(418, 159);
			this.MaterialList.TabIndex = 0;
			this.MaterialList.UseCompatibleStateImageBehavior = false;
			this.MaterialList.View = System.Windows.Forms.View.Details;
			this.MaterialList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnMaterialRename);
			this.MaterialList.SelectedIndexChanged += new System.EventHandler(this.OnMaterialSelectionChanged);
			this.MaterialList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnMatListKeyUp);
			this.MaterialList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMatListClick);
			// 
			// VariableList
			// 
			this.VariableList.AllowUserToAddRows = false;
			this.VariableList.AllowUserToDeleteRows = false;
			this.VariableList.AllowUserToResizeRows = false;
			this.VariableList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.VariableList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.VariableList.Location = new System.Drawing.Point(12, 12);
			this.VariableList.Name = "VariableList";
			this.VariableList.Size = new System.Drawing.Size(324, 165);
			this.VariableList.TabIndex = 3;
			// 
			// FileGroup
			// 
			this.FileGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.FileGroup.Controls.Add(this.MergeMaterialLib);
			this.FileGroup.Controls.Add(this.NewMaterial);
			this.FileGroup.Controls.Add(this.LoadButton);
			this.FileGroup.Controls.Add(this.SaveButton);
			this.FileGroup.Location = new System.Drawing.Point(436, 414);
			this.FileGroup.Name = "FileGroup";
			this.FileGroup.Size = new System.Drawing.Size(77, 159);
			this.FileGroup.TabIndex = 18;
			this.FileGroup.TabStop = false;
			this.FileGroup.Text = "File IO";
			// 
			// MergeMaterialLib
			// 
			this.MergeMaterialLib.Location = new System.Drawing.Point(6, 124);
			this.MergeMaterialLib.Name = "MergeMaterialLib";
			this.MergeMaterialLib.Size = new System.Drawing.Size(55, 29);
			this.MergeMaterialLib.TabIndex = 10;
			this.MergeMaterialLib.Text = "Merge";
			this.MergeMaterialLib.UseVisualStyleBackColor = true;
			this.MergeMaterialLib.Click += new System.EventHandler(this.OnMergeMatLib);
			// 
			// NewMaterial
			// 
			this.NewMaterial.Location = new System.Drawing.Point(6, 19);
			this.NewMaterial.Name = "NewMaterial";
			this.NewMaterial.Size = new System.Drawing.Size(66, 29);
			this.NewMaterial.TabIndex = 3;
			this.NewMaterial.Text = "New Mat";
			this.NewMaterial.UseVisualStyleBackColor = true;
			this.NewMaterial.Click += new System.EventHandler(this.OnNewMaterial);
			// 
			// LoadButton
			// 
			this.LoadButton.Location = new System.Drawing.Point(6, 54);
			this.LoadButton.Name = "LoadButton";
			this.LoadButton.Size = new System.Drawing.Size(48, 29);
			this.LoadButton.TabIndex = 9;
			this.LoadButton.Text = "Load";
			this.LoadButton.UseVisualStyleBackColor = true;
			this.LoadButton.Click += new System.EventHandler(this.OnLoadMaterialLib);
			// 
			// SaveButton
			// 
			this.SaveButton.Location = new System.Drawing.Point(6, 89);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(46, 29);
			this.SaveButton.TabIndex = 8;
			this.SaveButton.Text = "Save";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.OnSaveMaterialLib);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.GuessShaderParameterVisibility);
			this.groupBox1.Controls.Add(this.UnHideAll);
			this.groupBox1.Controls.Add(this.IgnoreParameter);
			this.groupBox1.Controls.Add(this.HideSelected);
			this.groupBox1.Controls.Add(this.GuessTextures);
			this.groupBox1.Controls.Add(this.TexSizeUp);
			this.groupBox1.Controls.Add(this.TexSizeDown);
			this.groupBox1.Location = new System.Drawing.Point(342, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(171, 165);
			this.groupBox1.TabIndex = 19;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Shader Shtuff";
			// 
			// GuessShaderParameterVisibility
			// 
			this.GuessShaderParameterVisibility.Location = new System.Drawing.Point(86, 124);
			this.GuessShaderParameterVisibility.Name = "GuessShaderParameterVisibility";
			this.GuessShaderParameterVisibility.Size = new System.Drawing.Size(79, 29);
			this.GuessShaderParameterVisibility.TabIndex = 20;
			this.GuessShaderParameterVisibility.Text = "Guess Vars";
			this.GuessShaderParameterVisibility.UseVisualStyleBackColor = true;
			this.GuessShaderParameterVisibility.Click += new System.EventHandler(this.OnGuessVisibility);
			// 
			// UnHideAll
			// 
			this.UnHideAll.Location = new System.Drawing.Point(86, 89);
			this.UnHideAll.Name = "UnHideAll";
			this.UnHideAll.Size = new System.Drawing.Size(79, 29);
			this.UnHideAll.TabIndex = 19;
			this.UnHideAll.Text = "Reset Vars";
			this.UnHideAll.UseVisualStyleBackColor = true;
			this.UnHideAll.Click += new System.EventHandler(this.OnResetVisibility);
			// 
			// IgnoreParameter
			// 
			this.IgnoreParameter.Location = new System.Drawing.Point(86, 54);
			this.IgnoreParameter.Name = "IgnoreParameter";
			this.IgnoreParameter.Size = new System.Drawing.Size(79, 29);
			this.IgnoreParameter.TabIndex = 18;
			this.IgnoreParameter.Text = "Ignore Var";
			this.IgnoreParameter.UseVisualStyleBackColor = true;
			this.IgnoreParameter.Click += new System.EventHandler(this.OnIgnoreVariables);
			// 
			// HideSelected
			// 
			this.HideSelected.Location = new System.Drawing.Point(86, 19);
			this.HideSelected.Name = "HideSelected";
			this.HideSelected.Size = new System.Drawing.Size(79, 29);
			this.HideSelected.TabIndex = 17;
			this.HideSelected.Text = "Hide Var";
			this.HideSelected.UseVisualStyleBackColor = true;
			this.HideSelected.Click += new System.EventHandler(this.OnHideVariables);
			// 
			// GuessTextures
			// 
			this.GuessTextures.Location = new System.Drawing.Point(6, 19);
			this.GuessTextures.Name = "GuessTextures";
			this.GuessTextures.Size = new System.Drawing.Size(74, 29);
			this.GuessTextures.TabIndex = 13;
			this.GuessTextures.Text = "Guess Tex";
			this.GuessTextures.UseVisualStyleBackColor = true;
			// 
			// TexSizeUp
			// 
			this.TexSizeUp.Location = new System.Drawing.Point(6, 54);
			this.TexSizeUp.Name = "TexSizeUp";
			this.TexSizeUp.Size = new System.Drawing.Size(74, 29);
			this.TexSizeUp.TabIndex = 14;
			this.TexSizeUp.Text = "TexUp";
			this.TexSizeUp.UseVisualStyleBackColor = true;
			// 
			// TexSizeDown
			// 
			this.TexSizeDown.Location = new System.Drawing.Point(6, 89);
			this.TexSizeDown.Name = "TexSizeDown";
			this.TexSizeDown.Size = new System.Drawing.Size(74, 29);
			this.TexSizeDown.TabIndex = 15;
			this.TexSizeDown.Text = "TexDown";
			this.TexSizeDown.UseVisualStyleBackColor = true;
			// 
			// MeshPartGroup
			// 
			this.MeshPartGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MeshPartGroup.Controls.Add(this.WeldWeight);
			this.MeshPartGroup.Controls.Add(this.StripElements);
			this.MeshPartGroup.Controls.Add(this.Match);
			this.MeshPartGroup.Controls.Add(this.label1);
			this.MeshPartGroup.Controls.Add(this.TexCoordSet);
			this.MeshPartGroup.Controls.Add(this.GenBiNormalTangent);
			this.MeshPartGroup.Controls.Add(this.ApplyMaterial);
			this.MeshPartGroup.Location = new System.Drawing.Point(12, 355);
			this.MeshPartGroup.Name = "MeshPartGroup";
			this.MeshPartGroup.Size = new System.Drawing.Size(466, 53);
			this.MeshPartGroup.TabIndex = 22;
			this.MeshPartGroup.TabStop = false;
			this.MeshPartGroup.Text = "Mesh Part";
			// 
			// WeldWeight
			// 
			this.WeldWeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.WeldWeight.Location = new System.Drawing.Point(372, 19);
			this.WeldWeight.Name = "WeldWeight";
			this.WeldWeight.Size = new System.Drawing.Size(88, 28);
			this.WeldWeight.TabIndex = 25;
			this.WeldWeight.Text = "Weld Weights";
			this.WeldWeight.UseVisualStyleBackColor = true;
			// 
			// StripElements
			// 
			this.StripElements.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.StripElements.Location = new System.Drawing.Point(275, 19);
			this.StripElements.Name = "StripElements";
			this.StripElements.Size = new System.Drawing.Size(91, 28);
			this.StripElements.TabIndex = 24;
			this.StripElements.Text = "Strip Elements";
			this.StripElements.UseVisualStyleBackColor = true;
			this.StripElements.Click += new System.EventHandler(this.OnStripElements);
			// 
			// Match
			// 
			this.Match.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Match.Location = new System.Drawing.Point(197, 19);
			this.Match.Name = "Match";
			this.Match.Size = new System.Drawing.Size(72, 28);
			this.Match.TabIndex = 23;
			this.Match.Text = "Match Mat";
			this.Match.UseVisualStyleBackColor = true;
			this.Match.Click += new System.EventHandler(this.OnMatchAndVisible);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(159, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(23, 13);
			this.label1.TabIndex = 22;
			this.label1.Text = "Set";
			// 
			// TexCoordSet
			// 
			this.TexCoordSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TexCoordSet.Location = new System.Drawing.Point(149, 24);
			this.TexCoordSet.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
			this.TexCoordSet.Name = "TexCoordSet";
			this.TexCoordSet.Size = new System.Drawing.Size(42, 20);
			this.TexCoordSet.TabIndex = 22;
			// 
			// GenBiNormalTangent
			// 
			this.GenBiNormalTangent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.GenBiNormalTangent.Location = new System.Drawing.Point(86, 19);
			this.GenBiNormalTangent.Name = "GenBiNormalTangent";
			this.GenBiNormalTangent.Size = new System.Drawing.Size(57, 28);
			this.GenBiNormalTangent.TabIndex = 21;
			this.GenBiNormalTangent.Text = "Gen Tan";
			this.GenBiNormalTangent.UseVisualStyleBackColor = true;
			// 
			// ApplyMaterial
			// 
			this.ApplyMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyMaterial.Location = new System.Drawing.Point(6, 19);
			this.ApplyMaterial.Name = "ApplyMaterial";
			this.ApplyMaterial.Size = new System.Drawing.Size(74, 28);
			this.ApplyMaterial.TabIndex = 20;
			this.ApplyMaterial.Text = "Apply Mat";
			this.ApplyMaterial.UseVisualStyleBackColor = true;
			this.ApplyMaterial.Click += new System.EventHandler(this.OnApplyMaterial);
			// 
			// MeshPartList
			// 
			this.MeshPartList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MeshPartList.GridLines = true;
			this.MeshPartList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.MeshPartList.HideSelection = false;
			this.MeshPartList.LabelEdit = true;
			this.MeshPartList.Location = new System.Drawing.Point(12, 183);
			this.MeshPartList.Name = "MeshPartList";
			this.MeshPartList.Size = new System.Drawing.Size(501, 166);
			this.MeshPartList.TabIndex = 23;
			this.MeshPartList.UseCompatibleStateImageBehavior = false;
			this.MeshPartList.View = System.Windows.Forms.View.Details;
			this.MeshPartList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnMeshPartListKeyUp);
			this.MeshPartList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMeshPartMouseUp);
			// 
			// MaterialForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(525, 585);
			this.ControlBox = false;
			this.Controls.Add(this.MeshPartList);
			this.Controls.Add(this.MeshPartGroup);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.FileGroup);
			this.Controls.Add(this.VariableList);
			this.Controls.Add(this.MaterialList);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MaterialForm";
			this.ShowInTaskbar = false;
			this.Text = "MaterialForm";
			this.SizeChanged += new System.EventHandler(this.OnFormSizeChanged);
			((System.ComponentModel.ISupportInitialize)(this.VariableList)).EndInit();
			this.FileGroup.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.MeshPartGroup.ResumeLayout(false);
			this.MeshPartGroup.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView MaterialList;
		private System.Windows.Forms.DataGridView VariableList;
		private System.Windows.Forms.GroupBox FileGroup;
		private System.Windows.Forms.Button MergeMaterialLib;
		private System.Windows.Forms.Button NewMaterial;
		private System.Windows.Forms.Button LoadButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button GuessShaderParameterVisibility;
		private System.Windows.Forms.Button UnHideAll;
		private System.Windows.Forms.Button IgnoreParameter;
		private System.Windows.Forms.Button HideSelected;
		private System.Windows.Forms.Button GuessTextures;
		private System.Windows.Forms.Button TexSizeUp;
		private System.Windows.Forms.Button TexSizeDown;
		private System.Windows.Forms.GroupBox MeshPartGroup;
		private System.Windows.Forms.Button WeldWeight;
		private System.Windows.Forms.Button StripElements;
		private System.Windows.Forms.Button Match;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown TexCoordSet;
		private System.Windows.Forms.Button GenBiNormalTangent;
		private System.Windows.Forms.Button ApplyMaterial;
		private System.Windows.Forms.ListView MeshPartList;
	}
}