using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class AGC
    {
        private float _state;
        private float _targetAmp;
        private float _maxGain;
        //private float[] _samples;
        //private int _sampleIndex;
        //private int _sampleCount;


        public float TargetAmplitude
        {
            get { return _targetAmp; }
            set { _targetAmp = value; }
        }

        public float LastGain
        {
            get { return Math.Min(_targetAmp / _state, _maxGain); }
        }

        public float AverageAmplitude
        {
            get { return _state; }
        }

        public float MaximumGain
        {
            get { return _maxGain; }
            set { _maxGain = value; }
        }


        public AGC(float targetAmplitude)
            : this(targetAmplitude, 5f)
        {
        }

        public AGC(float targetAmplitude, float maxGain)
        {
            _targetAmp = targetAmplitude;
            _maxGain = maxGain;
        }


        public float Process(float value)
        {
            if (_state == 0.0f)
                _state = Math.Abs(value);
            _state = (_state * 0.9995f) + (Math.Abs(value) * 0.0005f);

            return value * Math.Min(_targetAmp / _state, _maxGain);

            //InsertSample(value);

            //float sum = 0f;
            //for (int i = 0; i < _sampleCount; i++)
            //{
            //    sum += _samples[i] * _samples[i];
            //}

            //float rms = (float)Math.Sqrt(sum / _sampleCount);

            //return value * (_targetAmp / rms);
        }

        public void ProcessDual(ref float value1, ref float value2)
        {
            float avgVol = Math.Max(Math.Abs(value1), Math.Abs(value2));
            if (_state == 0.0f)
                _state = avgVol;

            _state = (_state * 0.9995f) + (avgVol * 0.0005f);

            value1 *= Math.Min(_targetAmp / _state, _maxGain);
            value2 *= Math.Min(_targetAmp / _state, _maxGain);

            //value1 = Math.Max(-1.0f, Math.Min(value1, 1.0f));
            //value2 = Math.Max(-1.0f, Math.Min(value2, 1.0f));
        }


        //private void InsertSample(float value)
        //{
        //    _samples[_sampleIndex] = value;
        //    _sampleIndex++;
        //    if (_sampleIndex == _samples.Length)
        //        _sampleIndex = 0;

        //    if (_sampleCount < _samples.Length)
        //        _sampleCount++;
        //}
    }
}
