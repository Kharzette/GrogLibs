namespace SharedForms
{
	partial class EntityList
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
			System.Windows.Forms.ColumnHeader ID;
			System.Windows.Forms.ColumnHeader Name;
			this.CreateNew = new System.Windows.Forms.Button();
			this.Entities = new System.Windows.Forms.ListView();
			this.LoadEntities = new System.Windows.Forms.Button();
			this.SaveEntities = new System.Windows.Forms.Button();
			ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			Name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// ID
			// 
			ID.Text = "ID";
			// 
			// Name
			// 
			Name.Text = "Name";
			// 
			// CreateNew
			// 
			this.CreateNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.CreateNew.Location = new System.Drawing.Point(12, 229);
			this.CreateNew.Name = "CreateNew";
			this.CreateNew.Size = new System.Drawing.Size(96, 23);
			this.CreateNew.TabIndex = 1;
			this.CreateNew.Text = "Create New";
			this.CreateNew.UseVisualStyleBackColor = true;
			this.CreateNew.Click += new System.EventHandler(this.OnCreateNew);
			// 
			// Entities
			// 
			this.Entities.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Entities.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            ID,
            Name});
			this.Entities.FullRowSelect = true;
			this.Entities.GridLines = true;
			this.Entities.HideSelection = false;
			this.Entities.Location = new System.Drawing.Point(12, 12);
			this.Entities.Name = "Entities";
			this.Entities.Size = new System.Drawing.Size(260, 211);
			this.Entities.TabIndex = 2;
			this.Entities.UseCompatibleStateImageBehavior = false;
			this.Entities.View = System.Windows.Forms.View.Details;
			this.Entities.SelectedIndexChanged += new System.EventHandler(this.OnSelectionChanged);
			this.Entities.SizeChanged += new System.EventHandler(this.OnSizeChanged);
			this.Entities.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
			// 
			// LoadEntities
			// 
			this.LoadEntities.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadEntities.Location = new System.Drawing.Point(114, 229);
			this.LoadEntities.Name = "LoadEntities";
			this.LoadEntities.Size = new System.Drawing.Size(58, 23);
			this.LoadEntities.TabIndex = 3;
			this.LoadEntities.Text = "Load";
			this.LoadEntities.UseVisualStyleBackColor = true;
			this.LoadEntities.Click += new System.EventHandler(this.OnLoad);
			// 
			// SaveEntities
			// 
			this.SaveEntities.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.SaveEntities.Location = new System.Drawing.Point(178, 229);
			this.SaveEntities.Name = "SaveEntities";
			this.SaveEntities.Size = new System.Drawing.Size(61, 23);
			this.SaveEntities.TabIndex = 4;
			this.SaveEntities.Text = "Save";
			this.SaveEntities.UseVisualStyleBackColor = true;
			this.SaveEntities.Click += new System.EventHandler(this.OnSave);
			// 
			// EntityList
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 264);
			this.ControlBox = false;
			this.Controls.Add(this.SaveEntities);
			this.Controls.Add(this.LoadEntities);
			this.Controls.Add(this.Entities);
			this.Controls.Add(this.CreateNew);
			this.Name = "EntityList";
			this.Text = "Entities";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button CreateNew;
		private System.Windows.Forms.ListView Entities;
		private System.Windows.Forms.Button LoadEntities;
		private System.Windows.Forms.Button SaveEntities;
	}
}