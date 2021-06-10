namespace PitchShifter
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnStart = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.trackPitch = new System.Windows.Forms.TrackBar();
            this.cmbInput = new System.Windows.Forms.ComboBox();
            this.cmbOutput = new System.Windows.Forms.ComboBox();
            this.lblMic = new System.Windows.Forms.Label();
            this.lblSpeaker = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.trackGain = new System.Windows.Forms.TrackBar();
            this.btnStop = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackPitch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackGain)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(85, 134);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(118, 21);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "Pitch";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // trackPitch
            // 
            this.trackPitch.Enabled = false;
            this.trackPitch.Location = new System.Drawing.Point(110, 60);
            this.trackPitch.Minimum = -10;
            this.trackPitch.Name = "trackPitch";
            this.trackPitch.Size = new System.Drawing.Size(350, 45);
            this.trackPitch.TabIndex = 5;
            this.trackPitch.Scroll += new System.EventHandler(this.trackPitch_Scroll);
            this.trackPitch.ValueChanged += new System.EventHandler(this.trackPitch_ValueChanged);
            // 
            // cmbInput
            // 
            this.cmbInput.FormattingEnabled = true;
            this.cmbInput.Location = new System.Drawing.Point(115, 10);
            this.cmbInput.Name = "cmbInput";
            this.cmbInput.Size = new System.Drawing.Size(338, 20);
            this.cmbInput.TabIndex = 7;
            // 
            // cmbOutput
            // 
            this.cmbOutput.FormattingEnabled = true;
            this.cmbOutput.Location = new System.Drawing.Point(115, 35);
            this.cmbOutput.Name = "cmbOutput";
            this.cmbOutput.Size = new System.Drawing.Size(338, 20);
            this.cmbOutput.TabIndex = 8;
            // 
            // lblMic
            // 
            this.lblMic.AutoSize = true;
            this.lblMic.Location = new System.Drawing.Point(14, 13);
            this.lblMic.Name = "lblMic";
            this.lblMic.Size = new System.Drawing.Size(72, 12);
            this.lblMic.TabIndex = 9;
            this.lblMic.Text = "Microphone";
            // 
            // lblSpeaker
            // 
            this.lblSpeaker.AutoSize = true;
            this.lblSpeaker.Location = new System.Drawing.Point(14, 38);
            this.lblSpeaker.Name = "lblSpeaker";
            this.lblSpeaker.Size = new System.Drawing.Size(51, 12);
            this.lblSpeaker.TabIndex = 10;
            this.lblSpeaker.Text = "Speaker";
            // 
            // timer1
            // 
            this.timer1.Interval = 40;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 98);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 12);
            this.label1.TabIndex = 12;
            this.label1.Text = "Gain";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // trackGain
            // 
            this.trackGain.Enabled = false;
            this.trackGain.Location = new System.Drawing.Point(108, 89);
            this.trackGain.Maximum = 30;
            this.trackGain.Name = "trackGain";
            this.trackGain.Size = new System.Drawing.Size(350, 45);
            this.trackGain.TabIndex = 11;
            this.trackGain.Scroll += new System.EventHandler(this.trackGain_Scroll);
            this.trackGain.ValueChanged += new System.EventHandler(this.trackGain_ValueChanged);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(259, 134);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(118, 21);
            this.btnStop.TabIndex = 13;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.button1_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(468, 167);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackGain);
            this.Controls.Add(this.lblSpeaker);
            this.Controls.Add(this.lblMic);
            this.Controls.Add(this.cmbOutput);
            this.Controls.Add(this.cmbInput);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.trackPitch);
            this.Controls.Add(this.btnStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Pitch Shifter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackPitch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackGain)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar trackPitch;
        private System.Windows.Forms.ComboBox cmbInput;
        private System.Windows.Forms.ComboBox cmbOutput;
        private System.Windows.Forms.Label lblMic;
        private System.Windows.Forms.Label lblSpeaker;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar trackGain;
        private System.Windows.Forms.Button btnStop;
    }
}

