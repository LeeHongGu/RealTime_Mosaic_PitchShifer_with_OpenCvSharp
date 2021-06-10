using System;
using CSCore;

namespace PitchShifter
{
    class SampleDSP : ISampleSource
    {
        ISampleSource mSource;

        public SampleDSP(ISampleSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            mSource = source;
            PitchShift = 1;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            //change gain
            float gainAmplification = (float)(Math.Pow(10.0, GainDB / 20.0));
            int samples = mSource.Read(buffer, offset, count);

            if (gainAmplification != 1.0f)
            {
                for (int i = offset; i < offset + samples; i++)
                {
                    buffer[i] = Math.Max(Math.Min(buffer[i] * gainAmplification, 1), -1);
                }
            }

            //pitchshift value change
            if (PitchShift != 1.0f)
            {
                PitchShifter.PitchShift(PitchShift, offset, count, 2048, 4, mSource.WaveFormat.SampleRate, buffer);

            }
            return samples;
        }

        public float GainDB { get; set; }

        public float PitchShift { get; set; }

        public bool CanSeek
        {
            get { return mSource.CanSeek; }
        }

        public WaveFormat WaveFormat
        {
            get { return mSource.WaveFormat; }
        }

        public long Position
        {
            get
            {
                return mSource.Position;
            }
            set
            {
                mSource.Position = value;
            }
        }

        public long Length
        {
            get { return mSource.Length; }
        }

        public void Dispose()
        {
            if (mSource != null) mSource.Dispose();
        }
    }
}
