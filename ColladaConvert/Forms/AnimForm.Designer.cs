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
			this.SaveAnimLib = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.SaveCharacter = new System.Windows.Forms.Button();
			this.LoadCharacter = new System.Windows.Forms.Button();
			this.ClearAll = new System.Windows.Forms.Button();
			this.Compress = new System.Windows.Forms.Button();
			this.MaxError = new System.Windows.Forms.NumericUpDown();
			this.LoadStaticModel = new System.Windows.Forms.Button();
			this.SaveStatic = new System.Windows.Forms.Button();
			this.LoadStatic = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.AnimGrid)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TimeScale)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxError)).BeginInit();
			this.SuspendLayout();
			// 
			// LoadAnim
			// 
			this.LoadAnim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadAnim.Location = new System.Drawing.Point(258, 262);
			this.LoadAnim.Name = "LoadAnim";
			this.LoadAnim.Size = new System.Drawing.Size(118, 25);
			this.LoadAnim.TabIndex = 0;
			this.LoadAnim.Text = "Load Anim DAE";
			this.LoadAnim.UseVisualStyleBackColor = true;
			this.LoadAnim.Click += new System.EventHandler(this.OnLoadAnim);
			// 
			// LoadModel
			// 
			this.LoadModel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadModel.Location = new System.Drawing.Point(12, 262);
			this.LoadModel.Name = "LoadModel";
			this.LoadModel.Size = new System.Drawing.Size(131, 25);
			this.LoadModel.TabIndex = 1;
			this.LoadModel.Text = "Load DAE Char Model";
			this.LoadModel.UseVisualStyleBackColor = true;
			this.LoadModel.Click += new System.EventHandler(this.OnLoadModel);
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
			this.AnimGrid.MultiSelect = false;
			this.AnimGrid.Name = "AnimGrid";
			this.AnimGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.AnimGrid.Size = new System.Drawing.Size(406, 212);
			this.AnimGrid.TabIndex = 2;
			this.AnimGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.OnRowNuking);
			this.AnimGrid.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellValidated);
			this.AnimGrid.SelectionChanged += new System.EventHandler(this.AnimGrid_SelectionChanged);
			// 
			// TimeScale
			// 
			this.TimeScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TimeScale.DecimalPlaces = 2;
			this.TimeScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
			this.TimeScale.Location = new System.Drawing.Point(11, 236);
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
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(68, 238);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Time Scale";
			// 
			// SaveAnimLib
			// 
			this.SaveAnimLib.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.SaveAnimLib.Location = new System.Drawing.Point(111, 291);
			this.SaveAnimLib.Name = "SaveAnimLib";
			this.SaveAnimLib.Size = new System.Drawing.Size(112, 25);
			this.SaveAnimLib.TabIndex = 5;
			this.SaveAnimLib.Text = "Save Anim Library";
			this.SaveAnimLib.UseVisualStyleBackColor = true;
			this.SaveAnimLib.Click += new System.EventHandler(this.OnSaveLibrary);
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button2.Location = new System.Drawing.Point(115, 322);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(108, 25);
			this.button2.TabIndex = 6;
			this.button2.Text = "Load Anim Library";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.OnLoadLibrary);
			// 
			// SaveCharacter
			// 
			this.SaveCharacter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.SaveCharacter.Location = new System.Drawing.Point(11, 291);
			this.SaveCharacter.Name = "SaveCharacter";
			this.SaveCharacter.Size = new System.Drawing.Size(94, 25);
			this.SaveCharacter.TabIndex = 7;
			this.SaveCharacter.Text = "Save Character";
			this.SaveCharacter.UseVisualStyleBackColor = true;
			this.SaveCharacter.Click += new System.EventHandler(this.OnSaveCharacter);
			// 
			// LoadCharacter
			// 
			this.LoadCharacter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadCharacter.Location = new System.Drawing.Point(12, 322);
			this.LoadCharacter.Name = "LoadCharacter";
			this.LoadCharacter.Size = new System.Drawing.Size(97, 25);
			this.LoadCharacter.TabIndex = 8;
			this.LoadCharacter.Text = "Load Character";
			this.LoadCharacter.UseVisualStyleBackColor = true;
			this.LoadCharacter.Click += new System.EventHandler(this.OnLoadCharacter);
			// 
			// ClearAll
			// 
			this.ClearAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearAll.Location = new System.Drawing.Point(346, 231);
			this.ClearAll.Name = "ClearAll";
			this.ClearAll.Size = new System.Drawing.Size(70, 24);
			this.ClearAll.TabIndex = 9;
			this.ClearAll.Text = "Clear All";
			this.ClearAll.UseVisualStyleBackColor = true;
			this.ClearAll.Click += new System.EventHandler(this.OnClearAll);
			// 
			// Compress
			// 
			this.Compress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Compress.Location = new System.Drawing.Point(271, 231);
			this.Compress.Name = "Compress";
			this.Compress.Size = new System.Drawing.Size(69, 25);
			this.Compress.TabIndex = 10;
			this.Compress.Text = "Compress";
			this.Compress.UseVisualStyleBackColor = true;
			this.Compress.Click += new System.EventHandler(this.OnCompress);
			// 
			// MaxError
			// 
			this.MaxError.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.MaxError.DecimalPlaces = 3;
			this.MaxError.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
			this.MaxError.Location = new System.Drawing.Point(214, 235);
			this.MaxError.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
			this.MaxError.Name = "MaxError";
			this.MaxError.Size = new System.Drawing.Size(51, 20);
			this.MaxError.TabIndex = 11;
			this.MaxError.Value = new decimal(new int[] {
            10,
            0,
            0,
            65536});
			// 
			// LoadStaticModel
			// 
			this.LoadStaticModel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadStaticModel.Location = new System.Drawing.Point(149, 262);
			this.LoadStaticModel.Name = "LoadStaticModel";
			this.LoadStaticModel.Size = new System.Drawing.Size(103, 25);
			this.LoadStaticModel.TabIndex = 12;
			this.LoadStaticModel.Text = "Load Static DAE";
			this.LoadStaticModel.UseVisualStyleBackColor = true;
			this.LoadStaticModel.Click += new System.EventHandler(this.OnLoadStaticModel);
			// 
			// SaveStatic
			// 
			this.SaveStatic.Location = new System.Drawing.Point(229, 291);
			this.SaveStatic.Name = "SaveStatic";
			this.SaveStatic.Size = new System.Drawing.Size(75, 25);
			this.SaveStatic.TabIndex = 13;
			this.SaveStatic.Text = "Save Static";
			this.SaveStatic.UseVisualStyleBackColor = true;
			this.SaveStatic.Click += new System.EventHandler(this.OnSaveStatic);
			// 
			// LoadStatic
			// 
			this.LoadStatic.Location = new System.Drawing.Point(229, 322);
			this.LoadStatic.Name = "LoadStatic";
			this.LoadStatic.Size = new System.Drawing.Size(75, 25);
			this.LoadStatic.TabIndex = 14;
			this.LoadStatic.Text = "Load Static";
			this.LoadStatic.UseVisualStyleBackColor = true;
			this.LoadStatic.Click += new System.EventHandler(this.OnLoadStatic);
			// 
			// AnimForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(428, 359);
			this.ControlBox = false;
			this.Controls.Add(this.LoadStatic);
			this.Controls.Add(this.SaveStatic);
			this.Controls.Add(this.LoadStaticModel);
			this.Controls.Add(this.MaxError);
			this.Controls.Add(this.Compress);
			this.Controls.Add(this.ClearAll);
			this.Controls.Add(this.LoadCharacter);
			this.Controls.Add(this.SaveCharacter);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.SaveAnimLib);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.TimeScale);
			this.Controls.Add(this.AnimGrid);
			this.Controls.Add(this.LoadModel);
			this.Controls.Add(this.LoadAnim);
			this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::ColladaConvert.Properties.Settings.Default, "AnimFormPos", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.Location = global::ColladaConvert.Properties.Settings.Default.AnimFormPos;
			this.MaximizeBox = false;
			this.Name = "AnimForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Animation";
			((System.ComponentModel.ISupportInitialize)(this.AnimGrid)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TimeScale)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxError)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button LoadAnim;
		private System.Windows.Forms.Button LoadModel;
		private System.Windows.Forms.DataGridView AnimGrid;
		private System.Windows.Forms.NumericUpDown TimeScale;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button SaveAnimLib;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button SaveCharacter;
		private System.Windows.Forms.Button LoadCharacter;
		private System.Windows.Forms.Button ClearAll;
		private System.Windows.Forms.Button Compress;
		private System.Windows.Forms.NumericUpDown MaxError;
		private System.Windows.Forms.Button LoadStaticModel;
		private System.Windows.Forms.Button SaveStatic;
		private System.Windows.Forms.Button LoadStatic;
	}
}