namespace SharedForms
{
	partial class VisForm
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
			this.RoughVis = new System.Windows.Forms.CheckBox();
			this.Distributed = new System.Windows.Forms.CheckBox();
			this.ResumeVis = new System.Windows.Forms.Button();
			this.Stop = new System.Windows.Forms.Button();
			this.ReloadBuildFarm = new System.Windows.Forms.Button();
			this.QueryVisFarm = new System.Windows.Forms.Button();
			this.VisGBSP = new System.Windows.Forms.Button();
			this.SortPortals = new System.Windows.Forms.CheckBox();
			this.GroupFileIO = new System.Windows.Forms.GroupBox();
			this.Pause = new System.Windows.Forms.Button();
			this.GroupFileIO.SuspendLayout();
			this.SuspendLayout();
			// 
			// RoughVis
			// 
			this.RoughVis.AutoSize = true;
			this.RoughVis.Location = new System.Drawing.Point(94, 69);
			this.RoughVis.Name = "RoughVis";
			this.RoughVis.Size = new System.Drawing.Size(58, 17);
			this.RoughVis.TabIndex = 45;
			this.RoughVis.Text = "Rough";
			this.RoughVis.UseVisualStyleBackColor = true;
			// 
			// Distributed
			// 
			this.Distributed.AutoSize = true;
			this.Distributed.Location = new System.Drawing.Point(12, 69);
			this.Distributed.Name = "Distributed";
			this.Distributed.Size = new System.Drawing.Size(76, 17);
			this.Distributed.TabIndex = 44;
			this.Distributed.Text = "Distributed";
			this.Distributed.UseVisualStyleBackColor = true;
			this.Distributed.CheckedChanged += new System.EventHandler(this.OnDistChecked);
			// 
			// ResumeVis
			// 
			this.ResumeVis.Enabled = false;
			this.ResumeVis.Location = new System.Drawing.Point(102, 19);
			this.ResumeVis.Name = "ResumeVis";
			this.ResumeVis.Size = new System.Drawing.Size(81, 23);
			this.ResumeVis.TabIndex = 42;
			this.ResumeVis.Text = "Resume Vis";
			this.ResumeVis.UseVisualStyleBackColor = true;
			this.ResumeVis.Click += new System.EventHandler(this.OnResumeVis);
			// 
			// Stop
			// 
			this.Stop.Location = new System.Drawing.Point(12, 92);
			this.Stop.Name = "Stop";
			this.Stop.Size = new System.Drawing.Size(44, 23);
			this.Stop.TabIndex = 41;
			this.Stop.Text = "Stop";
			this.Stop.UseVisualStyleBackColor = true;
			this.Stop.Click += new System.EventHandler(this.OnStopVis);
			// 
			// ReloadBuildFarm
			// 
			this.ReloadBuildFarm.Location = new System.Drawing.Point(113, 121);
			this.ReloadBuildFarm.Name = "ReloadBuildFarm";
			this.ReloadBuildFarm.Size = new System.Drawing.Size(87, 23);
			this.ReloadBuildFarm.TabIndex = 40;
			this.ReloadBuildFarm.Text = "ReLoad Farm";
			this.ReloadBuildFarm.UseVisualStyleBackColor = true;
			this.ReloadBuildFarm.Click += new System.EventHandler(this.OnReLoadVisFarm);
			// 
			// QueryVisFarm
			// 
			this.QueryVisFarm.Location = new System.Drawing.Point(12, 121);
			this.QueryVisFarm.Name = "QueryVisFarm";
			this.QueryVisFarm.Size = new System.Drawing.Size(95, 23);
			this.QueryVisFarm.TabIndex = 39;
			this.QueryVisFarm.Text = "Query VisFarm";
			this.QueryVisFarm.UseVisualStyleBackColor = true;
			this.QueryVisFarm.Click += new System.EventHandler(this.OnQueryVisFarm);
			// 
			// VisGBSP
			// 
			this.VisGBSP.Location = new System.Drawing.Point(6, 19);
			this.VisGBSP.Name = "VisGBSP";
			this.VisGBSP.Size = new System.Drawing.Size(90, 23);
			this.VisGBSP.TabIndex = 38;
			this.VisGBSP.Text = "Vis GBSP File";
			this.VisGBSP.UseVisualStyleBackColor = true;
			this.VisGBSP.Click += new System.EventHandler(this.OnVisGBSP);
			// 
			// SortPortals
			// 
			this.SortPortals.AutoSize = true;
			this.SortPortals.Checked = true;
			this.SortPortals.CheckState = System.Windows.Forms.CheckState.Checked;
			this.SortPortals.Location = new System.Drawing.Point(158, 69);
			this.SortPortals.Name = "SortPortals";
			this.SortPortals.Size = new System.Drawing.Size(80, 17);
			this.SortPortals.TabIndex = 46;
			this.SortPortals.Text = "Sort Portals";
			this.SortPortals.UseVisualStyleBackColor = true;
			// 
			// GroupFileIO
			// 
			this.GroupFileIO.Controls.Add(this.VisGBSP);
			this.GroupFileIO.Controls.Add(this.ResumeVis);
			this.GroupFileIO.Location = new System.Drawing.Point(12, 12);
			this.GroupFileIO.Name = "GroupFileIO";
			this.GroupFileIO.Size = new System.Drawing.Size(196, 51);
			this.GroupFileIO.TabIndex = 47;
			this.GroupFileIO.TabStop = false;
			this.GroupFileIO.Text = "File IO";
			// 
			// Pause
			// 
			this.Pause.Location = new System.Drawing.Point(62, 92);
			this.Pause.Name = "Pause";
			this.Pause.Size = new System.Drawing.Size(75, 23);
			this.Pause.TabIndex = 48;
			this.Pause.Text = "Pause";
			this.Pause.UseVisualStyleBackColor = true;
			// 
			// VisForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(240, 159);
			this.ControlBox = false;
			this.Controls.Add(this.Pause);
			this.Controls.Add(this.GroupFileIO);
			this.Controls.Add(this.QueryVisFarm);
			this.Controls.Add(this.Stop);
			this.Controls.Add(this.SortPortals);
			this.Controls.Add(this.ReloadBuildFarm);
			this.Controls.Add(this.RoughVis);
			this.Controls.Add(this.Distributed);
			this.Name = "VisForm";
			this.Text = "VisForm";
			this.GroupFileIO.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox RoughVis;
		private System.Windows.Forms.CheckBox Distributed;
		private System.Windows.Forms.Button ResumeVis;
		private System.Windows.Forms.Button Stop;
		private System.Windows.Forms.Button ReloadBuildFarm;
		private System.Windows.Forms.Button QueryVisFarm;
		private System.Windows.Forms.Button VisGBSP;
		private System.Windows.Forms.CheckBox SortPortals;
		private System.Windows.Forms.GroupBox GroupFileIO;
		private System.Windows.Forms.Button Pause;
	}
}