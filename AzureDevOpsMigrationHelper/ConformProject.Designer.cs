namespace DevOps_Migration_Helper
{
    partial class ConformProject
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
            this.btnConform = new System.Windows.Forms.Button();
            this.cbProjects = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnConform
            // 
            this.btnConform.Location = new System.Drawing.Point(253, 26);
            this.btnConform.Name = "btnConform";
            this.btnConform.Size = new System.Drawing.Size(75, 23);
            this.btnConform.TabIndex = 0;
            this.btnConform.Text = "Conform";
            this.btnConform.UseVisualStyleBackColor = true;
            this.btnConform.Click += new System.EventHandler(this.BtnConform_Click);
            // 
            // cbProjects
            // 
            this.cbProjects.FormattingEnabled = true;
            this.cbProjects.Location = new System.Drawing.Point(83, 28);
            this.cbProjects.Name = "cbProjects";
            this.cbProjects.Size = new System.Drawing.Size(164, 21);
            this.cbProjects.TabIndex = 1;
            this.cbProjects.SelectedIndexChanged += new System.EventHandler(this.CbProjects_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Project:";
            // 
            // ConformProject
            // 
            this.AcceptButton = this.btnConform;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 70);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbProjects);
            this.Controls.Add(this.btnConform);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(378, 109);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(378, 109);
            this.Name = "ConformProject";
            this.Text = "ConformProject";
            this.Load += new System.EventHandler(this.ConformProject_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnConform;
        private System.Windows.Forms.ComboBox cbProjects;
        private System.Windows.Forms.Label label1;
    }
}