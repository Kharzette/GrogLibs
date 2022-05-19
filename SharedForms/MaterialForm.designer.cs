namespace SharedForms
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
            this.components = new System.ComponentModel.Container();
            this.MaterialList = new System.Windows.Forms.ListView();
            this.FileGroup = new System.Windows.Forms.GroupBox();
            this.MergeMaterialLib = new System.Windows.Forms.Button();
            this.NewMaterial = new System.Windows.Forms.Button();
            this.LoadButton = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SolidColor = new System.Windows.Forms.Button();
            this.GuessTextures = new System.Windows.Forms.Button();
            this.MeshPartGroup = new System.Windows.Forms.GroupBox();
            this.WeldWeight = new System.Windows.Forms.Button();
            this.StripElements = new System.Windows.Forms.Button();
            this.Match = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.TexCoordSet = new System.Windows.Forms.NumericUpDown();
            this.GenBiNormalTangent = new System.Windows.Forms.Button();
            this.ApplyMaterial = new System.Windows.Forms.Button();
            this.MeshPartList = new System.Windows.Forms.ListView();
            this.MatFormToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SpecPower = new System.Windows.Forms.NumericUpDown();
            this.Texture0Pic = new System.Windows.Forms.PictureBox();
            this.Texture1Pic = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.FileGroup.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.MeshPartGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpecPower)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Texture0Pic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Texture1Pic)).BeginInit();
            this.SuspendLayout();
            // 
            // MaterialList
            // 
            this.MaterialList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MaterialList.FullRowSelect = true;
            this.MaterialList.GridLines = true;
            this.MaterialList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.MaterialList.LabelEdit = true;
            this.MaterialList.Location = new System.Drawing.Point(14, 478);
            this.MaterialList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaterialList.MultiSelect = false;
            this.MaterialList.Name = "MaterialList";
            this.MaterialList.Size = new System.Drawing.Size(487, 183);
            this.MaterialList.TabIndex = 0;
            this.MaterialList.UseCompatibleStateImageBehavior = false;
            this.MaterialList.View = System.Windows.Forms.View.Details;
            this.MaterialList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnMaterialRename);
            this.MaterialList.SelectedIndexChanged += new System.EventHandler(this.OnMaterialSelectionChanged);
            this.MaterialList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnMatListKeyUp);
            this.MaterialList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMatListClick);
            // 
            // FileGroup
            // 
            this.FileGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.FileGroup.Controls.Add(this.MergeMaterialLib);
            this.FileGroup.Controls.Add(this.NewMaterial);
            this.FileGroup.Controls.Add(this.LoadButton);
            this.FileGroup.Controls.Add(this.SaveButton);
            this.FileGroup.Location = new System.Drawing.Point(509, 478);
            this.FileGroup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.FileGroup.Name = "FileGroup";
            this.FileGroup.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.FileGroup.Size = new System.Drawing.Size(90, 183);
            this.FileGroup.TabIndex = 18;
            this.FileGroup.TabStop = false;
            this.FileGroup.Text = "File IO";
            // 
            // MergeMaterialLib
            // 
            this.MergeMaterialLib.Location = new System.Drawing.Point(7, 143);
            this.MergeMaterialLib.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MergeMaterialLib.Name = "MergeMaterialLib";
            this.MergeMaterialLib.Size = new System.Drawing.Size(64, 33);
            this.MergeMaterialLib.TabIndex = 10;
            this.MergeMaterialLib.Text = "Merge";
            this.MergeMaterialLib.UseVisualStyleBackColor = true;
            this.MergeMaterialLib.Click += new System.EventHandler(this.OnMergeMatLib);
            // 
            // NewMaterial
            // 
            this.NewMaterial.Location = new System.Drawing.Point(7, 22);
            this.NewMaterial.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.NewMaterial.Name = "NewMaterial";
            this.NewMaterial.Size = new System.Drawing.Size(77, 33);
            this.NewMaterial.TabIndex = 3;
            this.NewMaterial.Text = "New Mat";
            this.NewMaterial.UseVisualStyleBackColor = true;
            this.NewMaterial.Click += new System.EventHandler(this.OnNewMaterial);
            // 
            // LoadButton
            // 
            this.LoadButton.Location = new System.Drawing.Point(7, 62);
            this.LoadButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(56, 33);
            this.LoadButton.TabIndex = 9;
            this.LoadButton.Text = "Load";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.OnLoadMaterialLib);
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(7, 103);
            this.SaveButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(54, 33);
            this.SaveButton.TabIndex = 8;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.OnSaveMaterialLib);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.Texture1Pic);
            this.groupBox1.Controls.Add(this.Texture0Pic);
            this.groupBox1.Controls.Add(this.SpecPower);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.SolidColor);
            this.groupBox1.Controls.Add(this.GuessTextures);
            this.groupBox1.Location = new System.Drawing.Point(13, 12);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(586, 253);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Shader Shtuff";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(88, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 15);
            this.label2.TabIndex = 15;
            this.label2.Text = "Solid Color";
            // 
            // SolidColor
            // 
            this.SolidColor.Location = new System.Drawing.Point(7, 22);
            this.SolidColor.Name = "SolidColor";
            this.SolidColor.Size = new System.Drawing.Size(75, 23);
            this.SolidColor.TabIndex = 14;
            this.SolidColor.UseVisualStyleBackColor = true;
            this.SolidColor.Click += new System.EventHandler(this.OnSolidColor);
            // 
            // GuessTextures
            // 
            this.GuessTextures.Location = new System.Drawing.Point(492, 214);
            this.GuessTextures.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.GuessTextures.Name = "GuessTextures";
            this.GuessTextures.Size = new System.Drawing.Size(86, 33);
            this.GuessTextures.TabIndex = 13;
            this.GuessTextures.Text = "Guess Tex";
            this.MatFormToolTips.SetToolTip(this.GuessTextures, "Try to guess a texture value from the material name");
            this.GuessTextures.UseVisualStyleBackColor = true;
            this.GuessTextures.Click += new System.EventHandler(this.OnGuessTextures);
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
            this.MeshPartGroup.Location = new System.Drawing.Point(13, 411);
            this.MeshPartGroup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MeshPartGroup.Name = "MeshPartGroup";
            this.MeshPartGroup.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MeshPartGroup.Size = new System.Drawing.Size(496, 61);
            this.MeshPartGroup.TabIndex = 22;
            this.MeshPartGroup.TabStop = false;
            this.MeshPartGroup.Text = "Mesh Parts";
            // 
            // WeldWeight
            // 
            this.WeldWeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.WeldWeight.Location = new System.Drawing.Point(434, 12);
            this.WeldWeight.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.WeldWeight.Name = "WeldWeight";
            this.WeldWeight.Size = new System.Drawing.Size(54, 43);
            this.WeldWeight.TabIndex = 25;
            this.MatFormToolTips.SetToolTip(this.WeldWeight, "Attempts to stitch together the selected mesh parts by welding weights and vertic" +
        "es along boundaries");
            this.WeldWeight.UseVisualStyleBackColor = true;
            this.WeldWeight.Click += new System.EventHandler(this.OnFrankenstein);
            // 
            // StripElements
            // 
            this.StripElements.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.StripElements.Location = new System.Drawing.Point(321, 22);
            this.StripElements.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.StripElements.Name = "StripElements";
            this.StripElements.Size = new System.Drawing.Size(106, 32);
            this.StripElements.TabIndex = 24;
            this.StripElements.Text = "Strip Elements";
            this.MatFormToolTips.SetToolTip(this.StripElements, "Brings up the vertex format allowing deletion of elements");
            this.StripElements.UseVisualStyleBackColor = true;
            this.StripElements.Click += new System.EventHandler(this.OnStripElements);
            // 
            // Match
            // 
            this.Match.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Match.Location = new System.Drawing.Point(230, 22);
            this.Match.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Match.Name = "Match";
            this.Match.Size = new System.Drawing.Size(84, 32);
            this.Match.TabIndex = 23;
            this.Match.Text = "Match Mat";
            this.MatFormToolTips.SetToolTip(this.Match, "Attempt to guess which materials to assign to mesh parts based on the names.  Usu" +
        "ally doesn\'t work very well.");
            this.Match.UseVisualStyleBackColor = true;
            this.Match.Click += new System.EventHandler(this.OnMatchAndVisible);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(186, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 15);
            this.label1.TabIndex = 22;
            this.label1.Text = "Set";
            // 
            // TexCoordSet
            // 
            this.TexCoordSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.TexCoordSet.Location = new System.Drawing.Point(174, 28);
            this.TexCoordSet.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.TexCoordSet.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.TexCoordSet.Name = "TexCoordSet";
            this.TexCoordSet.Size = new System.Drawing.Size(49, 23);
            this.TexCoordSet.TabIndex = 22;
            this.MatFormToolTips.SetToolTip(this.TexCoordSet, "Texture coordinate set to use when generating tangents");
            // 
            // GenBiNormalTangent
            // 
            this.GenBiNormalTangent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.GenBiNormalTangent.Location = new System.Drawing.Point(100, 22);
            this.GenBiNormalTangent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.GenBiNormalTangent.Name = "GenBiNormalTangent";
            this.GenBiNormalTangent.Size = new System.Drawing.Size(66, 32);
            this.GenBiNormalTangent.TabIndex = 21;
            this.GenBiNormalTangent.Text = "Gen Tan";
            this.MatFormToolTips.SetToolTip(this.GenBiNormalTangent, "Generate tangents for normal mapping on the selected mesh parts");
            this.GenBiNormalTangent.UseVisualStyleBackColor = true;
            this.GenBiNormalTangent.Click += new System.EventHandler(this.OnGenTangents);
            // 
            // ApplyMaterial
            // 
            this.ApplyMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ApplyMaterial.Location = new System.Drawing.Point(7, 22);
            this.ApplyMaterial.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ApplyMaterial.Name = "ApplyMaterial";
            this.ApplyMaterial.Size = new System.Drawing.Size(86, 32);
            this.ApplyMaterial.TabIndex = 20;
            this.ApplyMaterial.Text = "Apply Mat";
            this.MatFormToolTips.SetToolTip(this.ApplyMaterial, "Applies the selected material to the selected mesh parts");
            this.ApplyMaterial.UseVisualStyleBackColor = true;
            this.ApplyMaterial.Click += new System.EventHandler(this.OnApplyMaterial);
            // 
            // MeshPartList
            // 
            this.MeshPartList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MeshPartList.GridLines = true;
            this.MeshPartList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.MeshPartList.LabelEdit = true;
            this.MeshPartList.Location = new System.Drawing.Point(13, 271);
            this.MeshPartList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MeshPartList.Name = "MeshPartList";
            this.MeshPartList.Size = new System.Drawing.Size(586, 134);
            this.MeshPartList.TabIndex = 23;
            this.MeshPartList.UseCompatibleStateImageBehavior = false;
            this.MeshPartList.View = System.Windows.Forms.View.Details;
            this.MeshPartList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnMeshPartRename);
            this.MeshPartList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnMeshPartListKeyUp);
            this.MeshPartList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMeshPartMouseUp);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(7, 51);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 16;
            this.button1.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(88, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 15);
            this.label3.TabIndex = 17;
            this.label3.Text = "Spec Color";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(89, 83);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 15);
            this.label4.TabIndex = 19;
            this.label4.Text = "Spec Power";
            // 
            // SpecPower
            // 
            this.SpecPower.Location = new System.Drawing.Point(8, 80);
            this.SpecPower.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.SpecPower.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.SpecPower.Name = "SpecPower";
            this.SpecPower.Size = new System.Drawing.Size(75, 23);
            this.SpecPower.TabIndex = 20;
            this.SpecPower.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // Texture0Pic
            // 
            this.Texture0Pic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Texture0Pic.Location = new System.Drawing.Point(515, 22);
            this.Texture0Pic.Name = "Texture0Pic";
            this.Texture0Pic.Size = new System.Drawing.Size(64, 64);
            this.Texture0Pic.TabIndex = 21;
            this.Texture0Pic.TabStop = false;
            this.Texture0Pic.Click += new System.EventHandler(this.OnTexture0Click);
            // 
            // Texture1Pic
            // 
            this.Texture1Pic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Texture1Pic.Location = new System.Drawing.Point(515, 92);
            this.Texture1Pic.Name = "Texture1Pic";
            this.Texture1Pic.Size = new System.Drawing.Size(64, 64);
            this.Texture1Pic.TabIndex = 22;
            this.Texture1Pic.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(458, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 15);
            this.label5.TabIndex = 23;
            this.label5.Text = "Texture0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(458, 92);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 15);
            this.label6.TabIndex = 24;
            this.label6.Text = "Texture1";
            // 
            // MaterialForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(612, 675);
            this.ControlBox = false;
            this.Controls.Add(this.MeshPartList);
            this.Controls.Add(this.MeshPartGroup);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.FileGroup);
            this.Controls.Add(this.MaterialList);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MaterialForm";
            this.ShowInTaskbar = false;
            this.Text = "MaterialForm";
            this.SizeChanged += new System.EventHandler(this.OnFormSizeChanged);
            this.FileGroup.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.MeshPartGroup.ResumeLayout(false);
            this.MeshPartGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TexCoordSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpecPower)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Texture0Pic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Texture1Pic)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView MaterialList;
		private System.Windows.Forms.GroupBox FileGroup;
		private System.Windows.Forms.Button MergeMaterialLib;
		private System.Windows.Forms.Button NewMaterial;
		private System.Windows.Forms.Button LoadButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button GuessTextures;
		private System.Windows.Forms.GroupBox MeshPartGroup;
		private System.Windows.Forms.Button WeldWeight;
		private System.Windows.Forms.Button StripElements;
		private System.Windows.Forms.Button Match;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown TexCoordSet;
		private System.Windows.Forms.Button GenBiNormalTangent;
		private System.Windows.Forms.Button ApplyMaterial;
		private System.Windows.Forms.ListView MeshPartList;
		private System.Windows.Forms.ToolTip MatFormToolTips;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button SolidColor;
        private System.Windows.Forms.NumericUpDown SpecPower;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox Texture1Pic;
        private System.Windows.Forms.PictureBox Texture0Pic;
    }
}