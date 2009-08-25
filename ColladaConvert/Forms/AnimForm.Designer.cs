namespace ColladaConvert
{
	partial class AnimForm
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
			this.LoadAnim = new System.Windows.Forms.Button();
			this.LoadModel = new System.Windows.Forms.Button();
			this.AnimGrid = new System.Windows.Forms.DataGridView();
			this.TimeScale = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.AnimGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TimeScale)).BeginInit();
			this.SuspendLayout();
			// 
			// LoadAnim
			// 
			this.LoadAnim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadAnim.Location = new System.Drawing.Point(12, 310);
			this.LoadAnim.Name = "LoadAnim";
			this.LoadAnim.Size = new System.Drawing.Size(67, 25);
			this.LoadAnim.TabIndex = 0;
			this.LoadAnim.Text = "Load Anim";
			this.LoadAnim.UseVisualStyleBackColor = true;
			this.LoadAnim.Click += new System.EventHandler(this.LoadAnim_Click);
			// 
			// LoadModel
			// 
			this.LoadModel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadModel.Location = new System.Drawing.Point(85, 310);
			this.LoadModel.Name = "LoadModel";
			this.LoadModel.Size = new System.Drawing.Size(73, 25);
			this.LoadModel.TabIndex = 1;
			this.LoadModel.Text = "Load Model";
			this.LoadModel.UseVisualStyleBackColor = true;
			this.LoadModel.Click += new System.EventHandler(this.LoadModel_Click);
			// 
			// AnimGrid
			// 
			this.AnimGrid.AllowUserToAddRows = false;
			this.AnimGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.AnimGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.AnimGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.AnimGrid.Location = new System.Drawing.Point(11, 13);
			this.AnimGrid.Name = "AnimGrid";
			this.AnimGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.AnimGrid.Size = new System.Drawing.Size(355, 205);
			this.AnimGrid.TabIndex = 2;
			this.AnimGrid.SelectionChanged += new System.EventHandler(this.AnimGrid_SelectionChanged);
			// 
			// TimeScale
			// 
			this.TimeScale.DecimalPlaces = 2;
			this.TimeScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
			this.TimeScale.Location = new System.Drawing.Point(12, 224);
			this.TimeScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
			this.TimeScale.Name = "TimeScale";
			this.TimeScale.Size = new System.Drawing.Size(51, 20);
			this.TimeScale.TabIndex = 3;
			this.TimeScale.Value = new decimal(new int[] {
            10,
            0,
            0,
            65536});
			this.TimeScale.ValueChanged += new System.EventHandler(this.TimeScale_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(69, 228);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Time Scale";
			// 
			// AnimForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(377, 347);
			this.ControlBox = false;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.TimeScale);
			this.Controls.Add(this.AnimGrid);
			this.Controls.Add(this.LoadModel);
			this.Controls.Add(this.LoadAnim);
			this.MaximizeBox = false;
			this.Name = "AnimForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Animation";
			((System.ComponentModel.ISupportInitialize)(this.AnimGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TimeScale)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button LoadAnim;
		private System.Windows.Forms.Button LoadModel;
		private System.Windows.Forms.DataGridView AnimGrid;
		private System.Windows.Forms.NumericUpDown TimeScale;
		private System.Windows.Forms.Label label1;
	}
}