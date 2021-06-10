using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

//CSCore API
using CSCore;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using CSCore.Codecs;

/********************************************************************************************************************************
*
* https://hello-stella.tistory.com/13 관련자료
* 
* FT
*푸리에 변환은 임의의 공간 위치 에서 정의된 함수를 연속적으로 변하는 파수를 갖는 주기함수들의 합으로 분해하여 표현하는 것
*시간 영역으로 표시된 파형을 진동수 영역으로 변환하여 여러 가지 진동수 성분과의 집합으로 표시하는 선형 변환
* 
* FFT
* 푸리에변환에 근거하여 근사공식을 이용한 이산푸리에변환을 계산할 때 연산횟수를 줄일 수 있도록 고안된 알고리즘
* O(n^2) - > O(nlogn)
* 
* STFT
*신호가 안정된 주기성을 갖는 짧은 시간 단위로 분할하여 푸리에 변환을 하는 일. 
*신호를 프레임 단위로 분할하고 푸리에 변환을 수행한다.
*시간에 따른 변화 알기 위함 
* 
*모노 사운드는 신호를 소리로 변환할 때 하나의 채널만 사용
*스테레오 사운드는 신호를 소리로 변환할 떄 하나 이상의 채널을 사용합니다.
* 
* 
********************************************************************************************************************************/


namespace PitchShifter
{
    public partial class MainForm : Form
    {
        //Variables
        private MMDeviceCollection mInputDevices;   //mic
        private MMDeviceCollection mOutputDevices;  //speaker
        private WasapiCapture mSoundIn;             //mic in
        private WasapiOut mSoundOut;                //mic - out
        private SampleDSP mDsp;                     //digital signal processing for micropocessor And used for real-time operating system calculations.
        private SimpleMixer mMixer;
        int i = 0;

        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(1200, 350);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Find sound capture devices and put combo
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            mInputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            MMDevice activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            foreach (MMDevice device in mInputDevices)
            {
                cmbInput.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID) cmbInput.SelectedIndex = cmbInput.Items.Count - 1;
            }

            //Find sound render devices and put combo
            activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            mOutputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);

            foreach (MMDevice device in mOutputDevices)
            {
                cmbOutput.Items.Add(device.FriendlyName);
                if (device.DeviceID == activeDevice.DeviceID) cmbOutput.SelectedIndex = cmbOutput.Items.Count - 1;
            }

        }

        //Start Audio Stream
        private bool StartFullDuplex()
        {
            try
            {
                //Init sound capture device with a latency of 5 ms.
                mSoundIn = new WasapiCapture(false, AudioClientShareMode.Exclusive, 5);
                mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                mSoundIn.Initialize();
                mSoundIn.Start();

                var source = new SoundInSource(mSoundIn) { FillWithZeros = true };

                //Init DSP for pitch shifting 
                mDsp = new SampleDSP(source.ToSampleSource().ToMono());
                mDsp.GainDB = trackGain.Value;
                SetPitchShiftValue();

                //Init mixer
                mMixer = new SimpleMixer(1, 16000) //mono, 16KHz
                {
                    FillWithZeros = false,
                    DivideResult = true //set to true for avoiding tick sounds because of exceeding -1 and 1
                };

                //Add sound source to the mixer
                mMixer.AddSource(mDsp.ChangeSampleRate(mMixer.WaveFormat.SampleRate));
                Console.WriteLine("pass: {0}", i++);

                //Init sound play device with a latency of 5 ms.
                mSoundOut = new WasapiOut(false, AudioClientShareMode.Exclusive, 5);
                mSoundOut.Device = mOutputDevices[cmbOutput.SelectedIndex];
                mSoundOut.Initialize(mMixer.ToWaveSource(16));
                Console.WriteLine("pass: {0}", i++);

                //Start
                mSoundOut.Play();
                Console.WriteLine("pass: {0}", i++);
                return true;
            }
            catch (Exception ex)
            {
                string msg = "Error in StartFullDuplex: \r\n" + ex.Message;
                MessageBox.Show(msg);
                Debug.WriteLine(msg);
            }
            return false;
        }

        //Stop audio stream
        private void StopFullDuplex()
        {
            if (mSoundOut != null) mSoundOut.Dispose();
            if (mSoundIn != null) mSoundIn.Dispose();
        }

        //Start & Stop
        private void btnStart_Click(object sender, EventArgs e)
        {
            StopFullDuplex();
            if (StartFullDuplex())
            {
                trackGain.Enabled = true;
                trackPitch.Enabled = true;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            StopFullDuplex();
            trackGain.Enabled = false;
            trackPitch.Enabled = false;
        }

        private void SetPitchShiftValue()
        {
            mDsp.PitchShift = (float)Math.Pow(2.0F, trackPitch.Value / 12.0F);  //반음 12개 변환
                                                                                //최대, 최소 반음: -12*log2(numel(Window)-OverlapLength) ≤ nsemitones ≤ -12*log2((numel(Window)-OverlapLength)/numel(Window))
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopFullDuplex();
        }

        private void trackGain_Scroll(object sender, EventArgs e)
        {
            mDsp.GainDB = trackGain.Value;
        }

        private void trackGain_ValueChanged(object sender, EventArgs e)
        {
            mDsp.GainDB = trackGain.Value;
        }

        private void trackPitch_Scroll(object sender, EventArgs e)
        {
            SetPitchShiftValue();
        }

        private void trackPitch_ValueChanged(object sender, EventArgs e)
        {
            SetPitchShiftValue();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            trackGain.Value = 0;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            trackPitch.Value = 0;
        }
    }
}
