using System;
using System.Collections.Generic;
using CSCore;

namespace PitchShifter
{
    public class SimpleMixer : ISampleSource
    {
        //Variables
        private readonly WaveFormat mWaveFormat;
        private readonly List<ISampleSource> mSampleSources = new List<ISampleSource>();
        private readonly object mLockObj = new object();
        private float[] mMixerBuffer;

        public bool FillWithZeros { get; set; }

        public bool DivideResult { get; set; }

        public SimpleMixer(int channelCount, int sampleRate)
        {
            if (channelCount < 1)
                throw new ArgumentOutOfRangeException("channelCount");

            if (sampleRate < 1)
                throw new ArgumentOutOfRangeException("sampleRate");

            //set wave format, to bset value 32
            mWaveFormat = new WaveFormat(sampleRate, 32, channelCount, AudioEncoding.IeeeFloat);
            FillWithZeros = false;
        }

        public void AddSource(ISampleSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (source.WaveFormat.Channels != WaveFormat.Channels || source.WaveFormat.SampleRate != WaveFormat.SampleRate)
                throw new ArgumentException("Invalid format.", "source");

            lock (mLockObj)
            {
                if (!Contains(source))
                    mSampleSources.Add(source);
            }
        }

        public void RemoveSource(ISampleSource source)
        {
            //don't throw null ex -> protect dead lock
            lock (mLockObj)
            {
                if (Contains(source))
                    mSampleSources.Remove(source);
            }
        }

        public bool Contains(ISampleSource source)
        {
            if (source == null)
                return false;

            return mSampleSources.Contains(source);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int numberOfStoredSamples = 0;

            if (count > 0 && mSampleSources.Count > 0)
            {
                lock (mLockObj)
                {
                    mMixerBuffer = mMixerBuffer.CheckBuffer(count);
                    List<int> numberOfReadSamples = new List<int>();

                    for (int m = mSampleSources.Count - 1; m >= 0; m--)
                    {
                        var sampleSource = mSampleSources[m];
                        int read = sampleSource.Read(mMixerBuffer, 0, count);

                        for (int i = offset, n = 0; n < read; i++, n++)
                        {
                            if (numberOfStoredSamples <= i)
                                buffer[i] = mMixerBuffer[n];
                            else
                                buffer[i] += mMixerBuffer[n];
                        }

                        if (read > numberOfStoredSamples)
                            numberOfStoredSamples = read;

                        if (read > 0)
                            numberOfReadSamples.Add(read);
                        else
                        {
                            //raise event here
                            RemoveSource(sampleSource); //remove the input->the event gets only raised once.
                        }
                    }

                    if (DivideResult)
                    {
                        numberOfReadSamples.Sort();

                        int currentOffset = offset;
                        int remainingSources = numberOfReadSamples.Count;

                        foreach (var readSamples in numberOfReadSamples)
                        {
                            if (remainingSources == 0)
                                break;

                            while (currentOffset < offset + readSamples)
                            {
                                buffer[currentOffset] /= remainingSources;
                                buffer[currentOffset] = Math.Max(-1, Math.Min(1, buffer[currentOffset]));
                                currentOffset++;
                            }
                            remainingSources--;
                        }
                    }
                }
            }

            if (FillWithZeros && numberOfStoredSamples != count)
            {
                Array.Clear(
                    buffer,
                    Math.Max(offset + numberOfStoredSamples - 1, 0),
                    count - numberOfStoredSamples);

                return count;
            }

            return numberOfStoredSamples;
        }

        public bool CanSeek { get { return false; } }

        public WaveFormat WaveFormat
        {
            get { return mWaveFormat; }
        }

        public long Position
        {
            get { return 0; }
            set
            {
                throw new NotSupportedException();
            }
        }

        public long Length
        {
            get { return 0; }
        }

        public void Dispose()
        {
            lock (mLockObj)
            {
                foreach (var sampleSource in mSampleSources.ToArray())
                {
                    sampleSource.Dispose();
                    mSampleSources.Remove(sampleSource);
                }
            }
        }
    }
}
