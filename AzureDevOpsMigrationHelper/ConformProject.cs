using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevOps_Migration_Helper
{
    public partial class ConformProject : Form
    {
        List<string> Projects;
        public string SelectedProject = "None";
        public ConformProject(List<string> _Projects)
        {
            Projects = _Projects;

            InitializeComponent();
        }

        private void ConformProject_Load(object sender, EventArgs e)
        {
            foreach (var item in Projects)
            {
                cbProjects.Items.Add(item);
            }
        }

        private void CbProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedProject = cbProjects.SelectedText;
        }

        private void BtnConform_Click(object sender, EventArgs e)
        {
            if (cbProjects.SelectedItem == null)
            {
                this.Close();
            }
            else
            {
                SelectedProject = cbProjects.SelectedItem.ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
