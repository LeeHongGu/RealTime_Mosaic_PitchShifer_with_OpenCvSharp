using System;
using System.Collections.Generic;
using System.Text;


/****************************************************************************
*
* 단시간 푸리에 변환을 사용하여 지속 시간을 유지하면서 피치 이동을 수행하는 루틴.
* 
* 루틴은 0.5 사이의 pitchShift 요소 값을 사용
* (한 옥타브 아래로) 및 2. (한 옥타브 위로). 정확히 1의 값은 변경x
* 
* Pitch 
* numSampsToProcess는  피치 시프트되고  
* indata [0 ...numSampsToProcess-1]랑
* outdata [0 ...numSampsToProcess-1]. 두 버퍼는 동일 할 수 있음 
* (데이터 내부). fftFrameSize는에 사용되는 FFT 프레임 크기를 정의..
* 
* Processing
* 일반적인 값은 1024, 2048 및 4096. 
* 모든 값이 <= 일 수 있음.
* MAX_FRAME_LENGTH이지만 2의 거듭 제곱. 
* osamp -> STFT, 인접 STFT 간의 겹침을 결정하는 오버 샘플링 인자
* 
* Frame. 
* 적당한 스케일링 비율의 경우 4 이상 
* 32의 값은 최상의 품질을 위해 권장됨. 
* sampleRate는 신호의 샘플 속도
* 
* 단위 Hz, 즉. 44.1kHz 오디오의 경우 44100. 루틴에 전달 된 데이터
* indata []는 출력 범위 인 [-1.0, 1.0) 범위에 있어야함
* 데이터의 경우 그에 따라 데이터의 크기를 조정 (16 비트 부호있는 정수의 경우 32768로 나누고 곱해야 함)
*
*****************************************************************************/


namespace PitchShifter
{
    public class PitchShifter
    {
        #region Private Static Memeber Variables
        private static int MAX_FRAME_LENGTH = 16000;
        private static float[] gInFIFO = new float[MAX_FRAME_LENGTH];
        private static float[] gOutFIFO = new float[MAX_FRAME_LENGTH];

        private static float[] gFFTworksp = new float[2 * MAX_FRAME_LENGTH];

        private static float[] gLastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private static float[] gSumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];

        private static float[] gOutputAccum = new float[2 * MAX_FRAME_LENGTH];

        private static float[] gAnaFreq = new float[MAX_FRAME_LENGTH];
        private static float[] gAnaMagn = new float[MAX_FRAME_LENGTH];
        private static float[] gSynFreq = new float[MAX_FRAME_LENGTH];
        private static float[] gSynMagn = new float[MAX_FRAME_LENGTH];
        private static long gRover, gInit;
        #endregion

        #region Public Static  Methods
        public static void PitchShift(float pitchShift, long sampleCount, float sampleRate, float[] indata)
        {
            PitchShift(pitchShift, 0, sampleCount, (long)2048, (long)4, sampleRate, indata);
        }

        /// <summary>
        /// algorithm by (https://dafx.labri.fr/main/papers/p007.pdf)
        /// summerize Korean translated - Pitch Shift Algorithm.txt
        /// </summary>
        public static void PitchShift(float pitchShift, long offset, long sampleCount, long fftFrameSize, long osamp, float sampleRate, float[] indata)
        {
            double magn, phase, tmp, window, real, imag;
            double freqPerBin, expct;
            long i, k, qpd, index, inFifoLatency, stepSize, fftFrameSize2;


            float[] outdata = indata;

            //set variables 
            fftFrameSize2 = fftFrameSize / 2;
            stepSize = fftFrameSize / osamp;
            freqPerBin = sampleRate / (double)fftFrameSize;
            expct = 2.0 * Math.PI * (double)stepSize / (double)fftFrameSize;
            inFifoLatency = fftFrameSize - stepSize;


            if (gRover == 0) gRover = inFifoLatency;

            //main processing loop
            for (i = offset; i < sampleCount; i++)
            {

                //not yet collected enough data just read in
                gInFIFO[gRover] = indata[i];
                outdata[i] = gOutFIFO[gRover - inFifoLatency];
                gRover++;

                //now enough data for processing
                if (gRover >= fftFrameSize)
                {
                    gRover = inFifoLatency;

                    //do windowing and re,im
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gFFTworksp[2 * k] = (float)(gInFIFO[k] * window);
                        gFFTworksp[2 * k + 1] = 0.0F;
                    }

                    /* ***************** ANALYSIS ******************* */
                    //do transform
                    ShortTimeFourierTransform(gFFTworksp, fftFrameSize, -1);

                    //analysis step
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        //FFT buffer
                        real = gFFTworksp[2 * k];
                        imag = gFFTworksp[2 * k + 1];

                        //compute magnitude and phase
                        magn = 2.0 * Math.Sqrt(real * real + imag * imag);
                        phase = Math.Atan2(imag, real);

                        //compute phase difference
                        tmp = phase - gLastPhase[k];
                        gLastPhase[k] = (float)phase;

                        //subtract expected phase difference
                        tmp -= (double)k * expct;

                        //map delta phase into +/- Pi interval
                        qpd = (long)(tmp / Math.PI);
                        if (qpd >= 0) qpd += qpd & 1;
                        else qpd -= qpd & 1;
                        tmp -= Math.PI * (double)qpd;

                        //get deviation from bin frequency from the +/- Pi interval
                        tmp = osamp * tmp / (2.0 * Math.PI);

                        // compute the k-th partials' true frequency
                        tmp = (double)k * freqPerBin + tmp * freqPerBin;

                        //store magnitude and true frequency in analysis arrays
                        gAnaMagn[k] = (float)magn;
                        gAnaFreq[k] = (float)tmp;

                    }

                    /* ***************** PROCESSING ******************* */

                    //actual pitch shifting 
                    for (int zero = 0; zero < fftFrameSize; zero++)
                    {
                        gSynMagn[zero] = 0;
                        gSynFreq[zero] = 0;
                    }

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        index = (long)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            gSynMagn[index] += gAnaMagn[k];
                            gSynFreq[index] = gAnaFreq[k] * pitchShift;
                        }
                    }

                    /* ***************** SYNTHESIS ******************* */
                    //the synthesis step
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        //get magnitude and true frequency from synthesis arrays
                        magn = gSynMagn[k];
                        tmp = gSynFreq[k];

                        //subtract bin mid frequency
                        tmp -= (double)k * freqPerBin;

                        //get bin deviation from freq deviation
                        tmp /= freqPerBin;

                        //take osamp into account(sampling?)
                        tmp = 2.0 * Math.PI * tmp / osamp;

                        //add the overlap phase advance back in
                        tmp += (double)k * expct;

                        //accumulate delta phase to get bin phase
                        gSumPhase[k] += (float)tmp;
                        phase = gSumPhase[k];

                        //get real and imag part and re-interleave
                        gFFTworksp[2 * k] = (float)(magn * Math.Cos(phase));
                        gFFTworksp[2 * k + 1] = (float)(magn * Math.Sin(phase));
                    }

                    //zero negative frequencies
                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++) gFFTworksp[k] = 0.0F;

                    //do inverse transform
                    ShortTimeFourierTransform(gFFTworksp, fftFrameSize, 1);

                    //do windowing and add to output accumulator
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[2 * k] / (fftFrameSize2 * osamp));
                    }
                    for (k = 0; k < stepSize; k++) gOutFIFO[k] = gOutputAccum[k];

                    ///shift accumulator////
                    //memmove(gOutputAccum, gOutputAccum + stepSize, fftFrameSize * sizeof(float));
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        gOutputAccum[k] = gOutputAccum[k + stepSize];
                    }

                    //move input FIFO
                    for (k = 0; k < inFifoLatency; k++) gInFIFO[k] = gInFIFO[k + stepSize];
                }
            }
        }
        #endregion

        #region Private Static Methods
        //STFT
        //논문과 해외 블로그 article 기반으로 작성,,
        public static void ShortTimeFourierTransform(float[] fftBuffer, long fftFrameSize, long sign)
        {
            float wr, wi, arg, temp;
            float tr, ti, ur, ui;
            long i, bitm, j, le, le2, k;

            for (i = 2; i < 2 * fftFrameSize - 2; i += 2)
            {
                for (bitm = 2, j = 0; bitm < 2 * fftFrameSize; bitm <<= 1)
                {
                    if ((i & bitm) != 0) j++;
                    j <<= 1;
                }
                if (i < j)
                {
                    temp = fftBuffer[i];
                    fftBuffer[i] = fftBuffer[j];
                    fftBuffer[j] = temp;
                    temp = fftBuffer[i + 1];
                    fftBuffer[i + 1] = fftBuffer[j + 1];
                    fftBuffer[j + 1] = temp;
                }
            }
            long max = (long)(Math.Log(fftFrameSize) / Math.Log(2.0) + .5);
            for (k = 0, le = 2; k < max; k++)
            {
                le <<= 1;
                le2 = le >> 1;
                ur = 1.0F;
                ui = 0.0F;
                arg = (float)Math.PI / (le2 >> 1);
                wr = (float)Math.Cos(arg);
                wi = (float)(sign * Math.Sin(arg));
                for (j = 0; j < le2; j += 2)
                {

                    for (i = j; i < 2 * fftFrameSize; i += le)
                    {
                        tr = fftBuffer[i + le2] * ur - fftBuffer[i + le2 + 1] * ui;
                        ti = fftBuffer[i + le2] * ui + fftBuffer[i + le2 + 1] * ur;
                        fftBuffer[i + le2] = fftBuffer[i] - tr;
                        fftBuffer[i + le2 + 1] = fftBuffer[i + 1] - ti;
                        fftBuffer[i] += tr;
                        fftBuffer[i + 1] += ti;

                    }
                    tr = ur * wr - ui * wi;
                    ui = ur * wi + ui * wr;
                    ur = tr;
                }
            }
        }
        #endregion
    }
}
