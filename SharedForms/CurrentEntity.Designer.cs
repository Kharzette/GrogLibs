namespace SharedForms
{
	partial class CurrentEntity
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
			this.EntityID = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.AddComponent = new System.Windows.Forms.Button();
			this.ComponentChoices = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.ComponentList = new System.Windows.Forms.ListView();
			this.WishYouWouldHide = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ComponentProperties = new TabbyPropertyGrid();
			this.SuspendLayout();
			// 
			// EntityID
			// 
			this.EntityID.Location = new System.Drawing.Point(12, 12);
			this.EntityID.Name = "EntityID";
			this.EntityID.ReadOnly = true;
			this.EntityID.Size = new System.Drawing.Size(100, 20);
			this.EntityID.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(118, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(47, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Entity ID";
			// 
			// AddComponent
			// 
			this.AddComponent.Location = new System.Drawing.Point(12, 38);
			this.AddComponent.Name = "AddComponent";
			this.AddComponent.Size = new System.Drawing.Size(112, 23);
			this.AddComponent.TabIndex = 3;
			this.AddComponent.Text = "Add Component";
			this.AddComponent.UseVisualStyleBackColor = true;
			this.AddComponent.Click += new System.EventHandler(this.OnAddComponent);
			// 
			// ComponentChoices
			// 
			this.ComponentChoices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ComponentChoices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ComponentChoices.FormattingEnabled = true;
			this.ComponentChoices.Location = new System.Drawing.Point(130, 40);
			this.ComponentChoices.Name = "ComponentChoices";
			this.ComponentChoices.Size = new System.Drawing.Size(299, 21);
			this.ComponentChoices.TabIndex = 4;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 73);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "Components";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 205);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(111, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Component Properties";
			// 
			// ComponentList
			// 
			this.ComponentList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ComponentList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.WishYouWouldHide});
			this.ComponentList.FullRowSelect = true;
			this.ComponentList.GridLines = true;
			this.ComponentList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.ComponentList.HideSelection = false;
			this.ComponentList.Location = new System.Drawing.Point(15, 91);
			this.ComponentList.MultiSelect = false;
			this.ComponentList.Name = "ComponentList";
			this.ComponentList.Size = new System.Drawing.Size(414, 97);
			this.ComponentList.TabIndex = 9;
			this.ComponentList.UseCompatibleStateImageBehavior = false;
			this.ComponentList.View = System.Windows.Forms.View.Details;
			this.ComponentList.SelectedIndexChanged += new System.EventHandler(this.OnComponentListSelectedIndexChanged);
			this.ComponentList.SizeChanged += new System.EventHandler(this.OnSizeChanged);
			this.ComponentList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnCListKeyUp);
			// 
			// WishYouWouldHide
			// 
			this.WishYouWouldHide.Text = "Component Type";
			this.WishYouWouldHide.Width = 363;
			// 
			// ComponentProperties
			// 
			this.ComponentProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ComponentProperties.Location = new System.Drawing.Point(12, 221);
			this.ComponentProperties.Name = "ComponentProperties";
			this.ComponentProperties.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
			this.ComponentProperties.Size = new System.Drawing.Size(417, 195);
			this.ComponentProperties.TabIndex = 10;
			this.ComponentProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.OnPropertyValueChanged);
			// 
			// CurrentEntity
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(441, 428);
			this.ControlBox = false;
			this.Controls.Add(this.ComponentProperties);
			this.Controls.Add(this.ComponentList);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.ComponentChoices);
			this.Controls.Add(this.AddComponent);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.EntityID);
			this.Name = "CurrentEntity";
			this.Text = "CurrentEntity";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox EntityID;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button AddComponent;
		private System.Windows.Forms.ComboBox ComponentChoices;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListView ComponentList;
		private System.Windows.Forms.ColumnHeader WishYouWouldHide;
		private TabbyPropertyGrid ComponentProperties;
	}
}