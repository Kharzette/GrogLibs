namespace BSPBuilder
{
	partial class MainForm
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
			this.OpenVMF = new System.Windows.Forms.Button();
			this.OpenMap = new System.Windows.Forms.Button();
			this.BevelBrushes = new System.Windows.Forms.CheckBox();
			this.LabelNumRawFaces = new System.Windows.Forms.Label();
			this.NumMapFaces = new System.Windows.Forms.TextBox();
			this.StatsGroupBox = new System.Windows.Forms.GroupBox();
			this.LabelNumPortals = new System.Windows.Forms.Label();
			this.NumPortals = new System.Windows.Forms.TextBox();
			this.LabelNumCollisionFaces = new System.Windows.Forms.Label();
			this.LabelNumDrawFaces = new System.Windows.Forms.Label();
			this.NumCollisionFaces = new System.Windows.Forms.TextBox();
			this.NumDrawFaces = new System.Windows.Forms.TextBox();
			this.SaveZone = new System.Windows.Forms.Button();
			this.ConsoleOut = new System.Windows.Forms.TextBox();
			this.DrawChoice = new System.Windows.Forms.ComboBox();
			this.MaxCPUCores = new System.Windows.Forms.NumericUpDown();
			this.LabelCPUCores = new System.Windows.Forms.Label();
			this.GroupFileIO = new System.Windows.Forms.GroupBox();
			this.OpenZone = new System.Windows.Forms.Button();
			this.SaveCollisionBrushes = new System.Windows.Forms.Button();
			this.SaveDrawBrushes = new System.Windows.Forms.Button();
			this.GroupBuildSettings = new System.Windows.Forms.GroupBox();
			this.GroupDrawSettings = new System.Windows.Forms.GroupBox();
			this.Progress1 = new System.Windows.Forms.ProgressBar();
			this.Progress2 = new System.Windows.Forms.ProgressBar();
			this.Progress3 = new System.Windows.Forms.ProgressBar();
			this.Progress4 = new System.Windows.Forms.ProgressBar();
			this.StatsGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MaxCPUCores)).BeginInit();
			this.GroupFileIO.SuspendLayout();
			this.GroupBuildSettings.SuspendLayout();
			this.GroupDrawSettings.SuspendLayout();
			this.SuspendLayout();
			// 
			// OpenVMF
			// 
			this.OpenVMF.Location = new System.Drawing.Point(6, 19);
			this.OpenVMF.Name = "OpenVMF";
			this.OpenVMF.Size = new System.Drawing.Size(75, 23);
			this.OpenVMF.TabIndex = 0;
			this.OpenVMF.Text = "Open VMF";
			this.OpenVMF.UseVisualStyleBackColor = true;
			this.OpenVMF.Click += new System.EventHandler(this.OnOpenVMF);
			// 
			// OpenMap
			// 
			this.OpenMap.Location = new System.Drawing.Point(6, 48);
			this.OpenMap.Name = "OpenMap";
			this.OpenMap.Size = new System.Drawing.Size(75, 23);
			this.OpenMap.TabIndex = 1;
			this.OpenMap.Text = "Open Map";
			this.OpenMap.UseVisualStyleBackColor = true;
			this.OpenMap.Click += new System.EventHandler(this.OnOpenMap);
			// 
			// BevelBrushes
			// 
			this.BevelBrushes.AutoSize = true;
			this.BevelBrushes.Checked = true;
			this.BevelBrushes.CheckState = System.Windows.Forms.CheckState.Checked;
			this.BevelBrushes.Enabled = false;
			this.BevelBrushes.Location = new System.Drawing.Point(6, 45);
			this.BevelBrushes.Name = "BevelBrushes";
			this.BevelBrushes.Size = new System.Drawing.Size(110, 17);
			this.BevelBrushes.TabIndex = 11;
			this.BevelBrushes.Text = "Add Bevel Planes";
			this.BevelBrushes.UseVisualStyleBackColor = true;
			// 
			// LabelNumRawFaces
			// 
			this.LabelNumRawFaces.AutoSize = true;
			this.LabelNumRawFaces.Location = new System.Drawing.Point(6, 22);
			this.LabelNumRawFaces.Name = "LabelNumRawFaces";
			this.LabelNumRawFaces.Size = new System.Drawing.Size(69, 13);
			this.LabelNumRawFaces.TabIndex = 12;
			this.LabelNumRawFaces.Text = "Map Brushes";
			// 
			// NumMapFaces
			// 
			this.NumMapFaces.Location = new System.Drawing.Point(98, 19);
			this.NumMapFaces.Name = "NumMapFaces";
			this.NumMapFaces.ReadOnly = true;
			this.NumMapFaces.Size = new System.Drawing.Size(76, 20);
			this.NumMapFaces.TabIndex = 13;
			// 
			// StatsGroupBox
			// 
			this.StatsGroupBox.Controls.Add(this.LabelNumPortals);
			this.StatsGroupBox.Controls.Add(this.NumPortals);
			this.StatsGroupBox.Controls.Add(this.LabelNumCollisionFaces);
			this.StatsGroupBox.Controls.Add(this.LabelNumDrawFaces);
			this.StatsGroupBox.Controls.Add(this.NumCollisionFaces);
			this.StatsGroupBox.Controls.Add(this.NumDrawFaces);
			this.StatsGroupBox.Controls.Add(this.LabelNumRawFaces);
			this.StatsGroupBox.Controls.Add(this.NumMapFaces);
			this.StatsGroupBox.Location = new System.Drawing.Point(12, 12);
			this.StatsGroupBox.Name = "StatsGroupBox";
			this.StatsGroupBox.Size = new System.Drawing.Size(321, 104);
			this.StatsGroupBox.TabIndex = 14;
			this.StatsGroupBox.TabStop = false;
			this.StatsGroupBox.Text = "Statistics";
			// 
			// LabelNumPortals
			// 
			this.LabelNumPortals.AutoSize = true;
			this.LabelNumPortals.Location = new System.Drawing.Point(194, 22);
			this.LabelNumPortals.Name = "LabelNumPortals";
			this.LabelNumPortals.Size = new System.Drawing.Size(39, 13);
			this.LabelNumPortals.TabIndex = 20;
			this.LabelNumPortals.Text = "Portals";
			// 
			// NumPortals
			// 
			this.NumPortals.Location = new System.Drawing.Point(239, 19);
			this.NumPortals.Name = "NumPortals";
			this.NumPortals.ReadOnly = true;
			this.NumPortals.Size = new System.Drawing.Size(76, 20);
			this.NumPortals.TabIndex = 19;
			// 
			// LabelNumCollisionFaces
			// 
			this.LabelNumCollisionFaces.AutoSize = true;
			this.LabelNumCollisionFaces.Location = new System.Drawing.Point(6, 74);
			this.LabelNumCollisionFaces.Name = "LabelNumCollisionFaces";
			this.LabelNumCollisionFaces.Size = new System.Drawing.Size(86, 13);
			this.LabelNumCollisionFaces.TabIndex = 18;
			this.LabelNumCollisionFaces.Text = "Collision Brushes";
			// 
			// LabelNumDrawFaces
			// 
			this.LabelNumDrawFaces.AutoSize = true;
			this.LabelNumDrawFaces.Location = new System.Drawing.Point(6, 48);
			this.LabelNumDrawFaces.Name = "LabelNumDrawFaces";
			this.LabelNumDrawFaces.Size = new System.Drawing.Size(73, 13);
			this.LabelNumDrawFaces.TabIndex = 17;
			this.LabelNumDrawFaces.Text = "Draw Brushes";
			// 
			// NumCollisionFaces
			// 
			this.NumCollisionFaces.Location = new System.Drawing.Point(98, 71);
			this.NumCollisionFaces.Name = "NumCollisionFaces";
			this.NumCollisionFaces.ReadOnly = true;
			this.NumCollisionFaces.Size = new System.Drawing.Size(76, 20);
			this.NumCollisionFaces.TabIndex = 15;
			// 
			// NumDrawFaces
			// 
			this.NumDrawFaces.Location = new System.Drawing.Point(98, 45);
			this.NumDrawFaces.Name = "NumDrawFaces";
			this.NumDrawFaces.ReadOnly = true;
			this.NumDrawFaces.Size = new System.Drawing.Size(76, 20);
			this.NumDrawFaces.TabIndex = 14;
			// 
			// SaveZone
			// 
			this.SaveZone.Location = new System.Drawing.Point(87, 19);
			this.SaveZone.Name = "SaveZone";
			this.SaveZone.Size = new System.Drawing.Size(75, 23);
			this.SaveZone.TabIndex = 15;
			this.SaveZone.Text = "Save Zone";
			this.SaveZone.UseVisualStyleBackColor = true;
			this.SaveZone.Click += new System.EventHandler(this.OnSaveZone);
			// 
			// ConsoleOut
			// 
			this.ConsoleOut.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ConsoleOut.Location = new System.Drawing.Point(12, 264);
			this.ConsoleOut.Multiline = true;
			this.ConsoleOut.Name = "ConsoleOut";
			this.ConsoleOut.ReadOnly = true;
			this.ConsoleOut.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.ConsoleOut.Size = new System.Drawing.Size(480, 153);
			this.ConsoleOut.TabIndex = 16;
			// 
			// DrawChoice
			// 
			this.DrawChoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DrawChoice.FormattingEnabled = true;
			this.DrawChoice.Items.AddRange(new object[] {
            "Draw Brushes",
            "Map Brushes",
            "Collision Brushes",
            "Trouble Brushes",
            "Draw Tree",
            "Collision Tree",
            "Portals",
            "Portal Tree"});
			this.DrawChoice.Location = new System.Drawing.Point(6, 19);
			this.DrawChoice.Name = "DrawChoice";
			this.DrawChoice.Size = new System.Drawing.Size(123, 21);
			this.DrawChoice.TabIndex = 17;
			this.DrawChoice.SelectedIndexChanged += new System.EventHandler(this.OnDrawChoiceChanged);
			// 
			// MaxCPUCores
			// 
			this.MaxCPUCores.Enabled = false;
			this.MaxCPUCores.Location = new System.Drawing.Point(6, 19);
			this.MaxCPUCores.Name = "MaxCPUCores";
			this.MaxCPUCores.Size = new System.Drawing.Size(41, 20);
			this.MaxCPUCores.TabIndex = 18;
			// 
			// LabelCPUCores
			// 
			this.LabelCPUCores.AutoSize = true;
			this.LabelCPUCores.Location = new System.Drawing.Point(53, 21);
			this.LabelCPUCores.Name = "LabelCPUCores";
			this.LabelCPUCores.Size = new System.Drawing.Size(79, 13);
			this.LabelCPUCores.TabIndex = 19;
			this.LabelCPUCores.Text = "Max Cpu Cores";
			// 
			// GroupFileIO
			// 
			this.GroupFileIO.Controls.Add(this.OpenZone);
			this.GroupFileIO.Controls.Add(this.SaveCollisionBrushes);
			this.GroupFileIO.Controls.Add(this.SaveDrawBrushes);
			this.GroupFileIO.Controls.Add(this.OpenVMF);
			this.GroupFileIO.Controls.Add(this.OpenMap);
			this.GroupFileIO.Controls.Add(this.SaveZone);
			this.GroupFileIO.Location = new System.Drawing.Point(12, 122);
			this.GroupFileIO.Name = "GroupFileIO";
			this.GroupFileIO.Size = new System.Drawing.Size(215, 129);
			this.GroupFileIO.TabIndex = 20;
			this.GroupFileIO.TabStop = false;
			this.GroupFileIO.Text = "File IO";
			// 
			// OpenZone
			// 
			this.OpenZone.Location = new System.Drawing.Point(6, 77);
			this.OpenZone.Name = "OpenZone";
			this.OpenZone.Size = new System.Drawing.Size(75, 23);
			this.OpenZone.TabIndex = 18;
			this.OpenZone.Text = "Open Zone";
			this.OpenZone.UseVisualStyleBackColor = true;
			this.OpenZone.Click += new System.EventHandler(this.OnOpenZone);
			// 
			// SaveCollisionBrushes
			// 
			this.SaveCollisionBrushes.Enabled = false;
			this.SaveCollisionBrushes.Location = new System.Drawing.Point(87, 77);
			this.SaveCollisionBrushes.Name = "SaveCollisionBrushes";
			this.SaveCollisionBrushes.Size = new System.Drawing.Size(115, 41);
			this.SaveCollisionBrushes.TabIndex = 17;
			this.SaveCollisionBrushes.Text = "Save Collision Brushes";
			this.SaveCollisionBrushes.UseVisualStyleBackColor = true;
			// 
			// SaveDrawBrushes
			// 
			this.SaveDrawBrushes.Enabled = false;
			this.SaveDrawBrushes.Location = new System.Drawing.Point(87, 48);
			this.SaveDrawBrushes.Name = "SaveDrawBrushes";
			this.SaveDrawBrushes.Size = new System.Drawing.Size(115, 23);
			this.SaveDrawBrushes.TabIndex = 16;
			this.SaveDrawBrushes.Text = "Save Draw Brushes";
			this.SaveDrawBrushes.UseVisualStyleBackColor = true;
			// 
			// GroupBuildSettings
			// 
			this.GroupBuildSettings.Controls.Add(this.MaxCPUCores);
			this.GroupBuildSettings.Controls.Add(this.LabelCPUCores);
			this.GroupBuildSettings.Controls.Add(this.BevelBrushes);
			this.GroupBuildSettings.Location = new System.Drawing.Point(352, 74);
			this.GroupBuildSettings.Name = "GroupBuildSettings";
			this.GroupBuildSettings.Size = new System.Drawing.Size(140, 72);
			this.GroupBuildSettings.TabIndex = 21;
			this.GroupBuildSettings.TabStop = false;
			this.GroupBuildSettings.Text = "Build Settings";
			// 
			// GroupDrawSettings
			// 
			this.GroupDrawSettings.Controls.Add(this.DrawChoice);
			this.GroupDrawSettings.Location = new System.Drawing.Point(339, 12);
			this.GroupDrawSettings.Name = "GroupDrawSettings";
			this.GroupDrawSettings.Size = new System.Drawing.Size(153, 56);
			this.GroupDrawSettings.TabIndex = 22;
			this.GroupDrawSettings.TabStop = false;
			this.GroupDrawSettings.Text = "Draw Settings";
			// 
			// Progress1
			// 
			this.Progress1.Location = new System.Drawing.Point(233, 154);
			this.Progress1.Name = "Progress1";
			this.Progress1.Size = new System.Drawing.Size(259, 19);
			this.Progress1.TabIndex = 23;
			// 
			// Progress2
			// 
			this.Progress2.Location = new System.Drawing.Point(233, 179);
			this.Progress2.Name = "Progress2";
			this.Progress2.Size = new System.Drawing.Size(259, 19);
			this.Progress2.TabIndex = 24;
			// 
			// Progress3
			// 
			this.Progress3.Location = new System.Drawing.Point(233, 204);
			this.Progress3.Name = "Progress3";
			this.Progress3.Size = new System.Drawing.Size(259, 19);
			this.Progress3.TabIndex = 25;
			// 
			// Progress4
			// 
			this.Progress4.Location = new System.Drawing.Point(233, 229);
			this.Progress4.Name = "Progress4";
			this.Progress4.Size = new System.Drawing.Size(259, 19);
			this.Progress4.TabIndex = 26;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(504, 429);
			this.ControlBox = false;
			this.Controls.Add(this.Progress4);
			this.Controls.Add(this.Progress3);
			this.Controls.Add(this.Progress2);
			this.Controls.Add(this.Progress1);
			this.Controls.Add(this.GroupDrawSettings);
			this.Controls.Add(this.GroupBuildSettings);
			this.Controls.Add(this.GroupFileIO);
			this.Controls.Add(this.ConsoleOut);
			this.Controls.Add(this.StatsGroupBox);
			this.Name = "MainForm";
			this.ShowInTaskbar = false;
			this.Text = "MainForm";
			this.StatsGroupBox.ResumeLayout(false);
			this.StatsGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MaxCPUCores)).EndInit();
			this.GroupFileIO.ResumeLayout(false);
			this.GroupBuildSettings.ResumeLayout(false);
			this.GroupBuildSettings.PerformLayout();
			this.GroupDrawSettings.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OpenVMF;
		private System.Windows.Forms.Button OpenMap;
		private System.Windows.Forms.CheckBox BevelBrushes;
		private System.Windows.Forms.Label LabelNumRawFaces;
		private System.Windows.Forms.TextBox NumMapFaces;
		private System.Windows.Forms.GroupBox StatsGroupBox;
		private System.Windows.Forms.Label LabelNumCollisionFaces;
		private System.Windows.Forms.Label LabelNumDrawFaces;
		private System.Windows.Forms.TextBox NumCollisionFaces;
		private System.Windows.Forms.TextBox NumDrawFaces;
		private System.Windows.Forms.Button SaveZone;
		private System.Windows.Forms.TextBox ConsoleOut;
		private System.Windows.Forms.ComboBox DrawChoice;
		private System.Windows.Forms.NumericUpDown MaxCPUCores;
		private System.Windows.Forms.Label LabelCPUCores;
		private System.Windows.Forms.GroupBox GroupFileIO;
		private System.Windows.Forms.Button SaveCollisionBrushes;
		private System.Windows.Forms.Button SaveDrawBrushes;
		private System.Windows.Forms.GroupBox GroupBuildSettings;
		private System.Windows.Forms.GroupBox GroupDrawSettings;
		private System.Windows.Forms.ProgressBar Progress1;
		private System.Windows.Forms.ProgressBar Progress2;
		private System.Windows.Forms.ProgressBar Progress3;
		private System.Windows.Forms.ProgressBar Progress4;
		private System.Windows.Forms.Button OpenZone;
		private System.Windows.Forms.Label LabelNumPortals;
		private System.Windows.Forms.TextBox NumPortals;
	}
}