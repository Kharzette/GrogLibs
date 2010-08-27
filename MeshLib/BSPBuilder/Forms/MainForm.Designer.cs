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
			this.BevelCorners = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.HullSize = new System.Windows.Forms.NumericUpDown();
			this.LabelNumPortals = new System.Windows.Forms.Label();
			this.NumPortals = new System.Windows.Forms.TextBox();
			this.EntityIndex = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.HullSize)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.EntityIndex)).BeginInit();
			this.SuspendLayout();
			// 
			// OpenVMF
			// 
			this.OpenVMF.Location = new System.Drawing.Point(12, 12);
			this.OpenVMF.Name = "OpenVMF";
			this.OpenVMF.Size = new System.Drawing.Size(75, 23);
			this.OpenVMF.TabIndex = 0;
			this.OpenVMF.Text = "Open VMF";
			this.OpenVMF.UseVisualStyleBackColor = true;
			this.OpenVMF.Click += new System.EventHandler(this.OnOpenVMF);
			// 
			// OpenMap
			// 
			this.OpenMap.Location = new System.Drawing.Point(12, 41);
			this.OpenMap.Name = "OpenMap";
			this.OpenMap.Size = new System.Drawing.Size(75, 23);
			this.OpenMap.TabIndex = 1;
			this.OpenMap.Text = "Open Map";
			this.OpenMap.UseVisualStyleBackColor = true;
			// 
			// BevelCorners
			// 
			this.BevelCorners.AutoSize = true;
			this.BevelCorners.Checked = true;
			this.BevelCorners.CheckState = System.Windows.Forms.CheckState.Checked;
			this.BevelCorners.Location = new System.Drawing.Point(93, 38);
			this.BevelCorners.Name = "BevelCorners";
			this.BevelCorners.Size = new System.Drawing.Size(123, 17);
			this.BevelCorners.TabIndex = 11;
			this.BevelCorners.Text = "Bevel Sharp Corners";
			this.BevelCorners.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(149, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 10;
			this.label1.Text = "Hull Size";
			// 
			// HullSize
			// 
			this.HullSize.Location = new System.Drawing.Point(93, 12);
			this.HullSize.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.HullSize.Name = "HullSize";
			this.HullSize.Size = new System.Drawing.Size(50, 20);
			this.HullSize.TabIndex = 9;
			this.HullSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// LabelNumPortals
			// 
			this.LabelNumPortals.AutoSize = true;
			this.LabelNumPortals.Location = new System.Drawing.Point(94, 73);
			this.LabelNumPortals.Name = "LabelNumPortals";
			this.LabelNumPortals.Size = new System.Drawing.Size(64, 13);
			this.LabelNumPortals.TabIndex = 12;
			this.LabelNumPortals.Text = "Num Portals";
			// 
			// NumPortals
			// 
			this.NumPortals.Location = new System.Drawing.Point(12, 70);
			this.NumPortals.Name = "NumPortals";
			this.NumPortals.ReadOnly = true;
			this.NumPortals.Size = new System.Drawing.Size(76, 20);
			this.NumPortals.TabIndex = 13;
			// 
			// EntityIndex
			// 
			this.EntityIndex.Location = new System.Drawing.Point(13, 97);
			this.EntityIndex.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.EntityIndex.Name = "EntityIndex";
			this.EntityIndex.Size = new System.Drawing.Size(75, 20);
			this.EntityIndex.TabIndex = 14;
			this.EntityIndex.ValueChanged += new System.EventHandler(this.OnEntityIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(94, 99);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(61, 13);
			this.label2.TabIndex = 15;
			this.label2.Text = "Entity Draw";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(238, 124);
			this.ControlBox = false;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.EntityIndex);
			this.Controls.Add(this.NumPortals);
			this.Controls.Add(this.LabelNumPortals);
			this.Controls.Add(this.BevelCorners);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.HullSize);
			this.Controls.Add(this.OpenMap);
			this.Controls.Add(this.OpenVMF);
			this.Name = "MainForm";
			this.ShowInTaskbar = false;
			this.Text = "MainForm";
			((System.ComponentModel.ISupportInitialize)(this.HullSize)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.EntityIndex)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OpenVMF;
		private System.Windows.Forms.Button OpenMap;
		private System.Windows.Forms.CheckBox BevelCorners;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown HullSize;
		private System.Windows.Forms.Label LabelNumPortals;
		private System.Windows.Forms.TextBox NumPortals;
		private System.Windows.Forms.NumericUpDown EntityIndex;
		private System.Windows.Forms.Label label2;
	}
}