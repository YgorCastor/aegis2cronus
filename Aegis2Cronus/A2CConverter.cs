using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Aegis2Cronus.Aegis;

namespace Aegis2Cronus
{
    partial class A2CConverter : Form
    {
        public A2CConverter()
        {
            InitializeComponent();
            Text += " - " + "10/08/2012 23:47";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            var s = new Scanner(textBox1.Text);
            var p = new Parser(s);
            var c = new CodeGen(p);
            Error.Errors = new List<string>();
            try
            {
                textBox2.Text = c.Gen();
            }
            catch
            {
                MessageBox.Show("Algum erro ocorreu durante a conversão!");
                textBox2.Text = "";
            }

            foreach (string er in Error.Errors)
                listBox1.Items.Add(er);
        }
    }
}