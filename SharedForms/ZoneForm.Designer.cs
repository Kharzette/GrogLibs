namespace SharedForms
{
	partial class ZoneForm
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
			this.GroupFileIO = new System.Windows.Forms.GroupBox();
			this.LoadDebug = new System.Windows.Forms.Button();
			this.SaveEmissives = new System.Windows.Forms.Button();
			this.VisGBSP = new System.Windows.Forms.Button();
			this.SaveZone = new System.Windows.Forms.Button();
			this.GenerateMaterials = new System.Windows.Forms.Button();
			this.LoadGBSP = new System.Windows.Forms.Button();
			this.label10 = new System.Windows.Forms.Label();
			this.AtlasSize = new System.Windows.Forms.NumericUpDown();
			this.SaveDebug = new System.Windows.Forms.CheckBox();
			this.mTips = new System.Windows.Forms.ToolTip(this.components);
			this.GroupFileIO.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AtlasSize)).BeginInit();
			this.SuspendLayout();
			// 
			// GroupFileIO
			// 
			this.GroupFileIO.Controls.Add(this.LoadDebug);
			this.GroupFileIO.Controls.Add(this.SaveEmissives);
			this.GroupFileIO.Controls.Add(this.VisGBSP);
			this.GroupFileIO.Controls.Add(this.SaveZone);
			this.GroupFileIO.Controls.Add(this.GenerateMaterials);
			this.GroupFileIO.Controls.Add(this.LoadGBSP);
			this.GroupFileIO.Location = new System.Drawing.Point(12, 12);
			this.GroupFileIO.Name = "GroupFileIO";
			this.GroupFileIO.Size = new System.Drawing.Size(189, 109);
			this.GroupFileIO.TabIndex = 26;
			this.GroupFileIO.TabStop = false;
			this.GroupFileIO.Text = "File IO";
			// 
			// LoadDebug
			// 
			this.LoadDebug.Location = new System.Drawing.Point(87, 77);
			this.LoadDebug.Name = "LoadDebug";
			this.LoadDebug.Size = new System.Drawing.Size(75, 23);
			this.LoadDebug.TabIndex = 27;
			this.LoadDebug.Text = "Load Debug";
			this.mTips.SetToolTip(this.LoadDebug, "Load debug files for drawing portals or other misc data");
			this.LoadDebug.UseVisualStyleBackColor = true;
			this.LoadDebug.Click += new System.EventHandler(this.OnLoadDebug);
			// 
			// SaveEmissives
			// 
			this.SaveEmissives.Location = new System.Drawing.Point(87, 19);
			this.SaveEmissives.Name = "SaveEmissives";
			this.SaveEmissives.Size = new System.Drawing.Size(96, 23);
			this.SaveEmissives.TabIndex = 26;
			this.SaveEmissives.Text = "Save Emissives";
			this.mTips.SetToolTip(this.SaveEmissives, "Saves the emissives set up on the material form");
			this.SaveEmissives.UseVisualStyleBackColor = true;
			this.SaveEmissives.Click += new System.EventHandler(this.OnSaveEmissives);
			// 
			// VisGBSP
			// 
			this.VisGBSP.Location = new System.Drawing.Point(6, 77);
			this.VisGBSP.Name = "VisGBSP";
			this.VisGBSP.Size = new System.Drawing.Size(75, 23);
			this.VisGBSP.TabIndex = 25;
			this.VisGBSP.Text = "Material Vis";
			this.mTips.SetToolTip(this.VisGBSP, "Compute which materials are visible from any given cluster");
			this.VisGBSP.UseVisualStyleBackColor = true;
			this.VisGBSP.Click += new System.EventHandler(this.OnMaterialVis);
			// 
			// SaveZone
			// 
			this.SaveZone.Enabled = false;
			this.SaveZone.Location = new System.Drawing.Point(87, 48);
			this.SaveZone.Name = "SaveZone";
			this.SaveZone.Size = new System.Drawing.Size(75, 23);
			this.SaveZone.TabIndex = 21;
			this.SaveZone.Text = "Save Zone";
			this.mTips.SetToolTip(this.SaveZone, "Save the zone, also creates a ZoneDraw file");
			this.SaveZone.UseVisualStyleBackColor = true;
			this.SaveZone.Click += new System.EventHandler(this.OnSaveZone);
			// 
			// GenerateMaterials
			// 
			this.GenerateMaterials.Location = new System.Drawing.Point(6, 19);
			this.GenerateMaterials.Name = "GenerateMaterials";
			this.GenerateMaterials.Size = new System.Drawing.Size(75, 23);
			this.GenerateMaterials.TabIndex = 24;
			this.GenerateMaterials.Text = "Gen Mats";
			this.mTips.SetToolTip(this.GenerateMaterials, "Generate materials, useful for making emissives for lighting");
			this.GenerateMaterials.UseVisualStyleBackColor = true;
			this.GenerateMaterials.Click += new System.EventHandler(this.OnGenerateMaterials);
			// 
			// LoadGBSP
			// 
			this.LoadGBSP.Location = new System.Drawing.Point(6, 48);
			this.LoadGBSP.Name = "LoadGBSP";
			this.LoadGBSP.Size = new System.Drawing.Size(75, 23);
			this.LoadGBSP.TabIndex = 19;
			this.LoadGBSP.Text = "Zone GBSP";
			this.mTips.SetToolTip(this.LoadGBSP, "Grind a gbsp file into Zone and ZoneDraw information");
			this.LoadGBSP.UseVisualStyleBackColor = true;
			this.LoadGBSP.Click += new System.EventHandler(this.OnZone);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(266, 14);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(99, 13);
			this.label10.TabIndex = 41;
			this.label10.Text = "Lightmap Atlas Size";
			// 
			// AtlasSize
			// 
			this.AtlasSize.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.AtlasSize.Location = new System.Drawing.Point(207, 12);
			this.AtlasSize.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
			this.AtlasSize.Minimum = new decimal(new int[] {
            256,
            0,
            0,
            0});
			this.AtlasSize.Name = "AtlasSize";
			this.AtlasSize.Size = new System.Drawing.Size(53, 20);
			this.AtlasSize.TabIndex = 42;
			this.mTips.SetToolTip(this.AtlasSize, "Size of the atlas that contains the lightmaps.  Smaller the better, but you may r" +
        "un out of space.");
			this.AtlasSize.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			// 
			// SaveDebug
			// 
			this.SaveDebug.AutoSize = true;
			this.SaveDebug.Checked = true;
			this.SaveDebug.CheckState = System.Windows.Forms.CheckState.Checked;
			this.SaveDebug.Location = new System.Drawing.Point(207, 38);
			this.SaveDebug.Name = "SaveDebug";
			this.SaveDebug.Size = new System.Drawing.Size(107, 17);
			this.SaveDebug.TabIndex = 43;
			this.SaveDebug.Text = "Save Debug Info";
			this.mTips.SetToolTip(this.SaveDebug, "Save the Zone with leaf face and other data a final game has no need of");
			this.SaveDebug.UseVisualStyleBackColor = true;
			// 
			// ZoneForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(389, 129);
			this.ControlBox = false;
			this.Controls.Add(this.SaveDebug);
			this.Controls.Add(this.AtlasSize);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.GroupFileIO);
			this.Name = "ZoneForm";
			this.Text = "ZoneForm";
			this.GroupFileIO.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.AtlasSize)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox GroupFileIO;
		private System.Windows.Forms.Button LoadDebug;
		private System.Windows.Forms.Button SaveEmissives;
		private System.Windows.Forms.Button VisGBSP;
		private System.Windows.Forms.Button SaveZone;
		private System.Windows.Forms.Button GenerateMaterials;
		private System.Windows.Forms.Button LoadGBSP;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.NumericUpDown AtlasSize;
		private System.Windows.Forms.CheckBox SaveDebug;
		private System.Windows.Forms.ToolTip mTips;
	}
}