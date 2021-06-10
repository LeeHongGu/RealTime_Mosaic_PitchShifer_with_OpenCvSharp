using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//Using other projects
using Face_Recognition;
using PitchShifter;

namespace RealTime_Mosaic_PitchShifer
{

    //First Form
    public partial class Form1 : Form
    {
        Face_Recognition.Form1 form1 = new Face_Recognition.Form1();
        PitchShifter.MainForm mf = new MainForm();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Before starting the program, " + "\ncheck that the camera, microphone, and speaker are working.",
                "Check Device", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                form1.Show();
                mf.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("기기를 확인하고 다시 실행해주세요.");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
