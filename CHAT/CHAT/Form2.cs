using System;
using System.Windows.Forms;

namespace CHAT
{
    public partial class Form2 : Form
    {
        public Form1? ChatFormReference { get; set; }

        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChatFormReference?.ApplySettings(checkBox1.Checked, textBox1.Text.Trim());
            MessageBox.Show("Settings applied.");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked) checkBox1.Checked = false;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}