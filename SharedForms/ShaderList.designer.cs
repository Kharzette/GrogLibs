namespace SharedForms
{
	partial class ShaderList
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
			this.ShaderGrid = new System.Windows.Forms.DataGridView();
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.ShaderGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// ShaderGrid
			// 
			this.ShaderGrid.AllowUserToAddRows = false;
			this.ShaderGrid.AllowUserToDeleteRows = false;
			this.ShaderGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ShaderGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.ShaderGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ShaderGrid.Location = new System.Drawing.Point(12, 12);
			this.ShaderGrid.MultiSelect = false;
			this.ShaderGrid.Name = "ShaderGrid";
			this.ShaderGrid.ReadOnly = true;
			this.ShaderGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ShaderGrid.Size = new System.Drawing.Size(392, 200);
			this.ShaderGrid.TabIndex = 5;
			this.ShaderGrid.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
			// 
			// Ok
			// 
			this.Ok.Location = new System.Drawing.Point(12, 224);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(91, 28);
			this.Ok.TabIndex = 6;
			this.Ok.Text = "Ok";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.OnOk);
			// 
			// Cancel
			// 
			this.Cancel.Location = new System.Drawing.Point(109, 224);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(82, 28);
			this.Cancel.TabIndex = 7;
			this.Cancel.Text = "Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.OnCancel);
			// 
			// ShaderList
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(416, 264);
			this.ControlBox = false;
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.Controls.Add(this.ShaderGrid);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Location", global::SharedForms.Settings.Default, "ShaderListPos", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.Location = global::SharedForms.Settings.Default.ShaderListPos;
			this.MaximizeBox = false;
			this.Name = "ShaderList";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "ShaderList";
			((System.ComponentModel.ISupportInitialize)(this.ShaderGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView ShaderGrid;
		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
	}
}