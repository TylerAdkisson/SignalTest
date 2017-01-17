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
        private float[] _samples;
        private int _sampleIndex;
        private int _sampleCount;
        private bool _isAdaptAllowed;
        private Integrator _gainIntegrator;
        private float _gain;


        public float TargetAmplitude
        {
            get { return _targetAmp; }
            set { _targetAmp = value; }
        }

        public bool AdaptGain
        {
            get { return _isAdaptAllowed; }
            set
            {
                _isAdaptAllowed = value;
                if (!value)
                    _gainIntegrator.IntegratorGain = (1f / 44100f) * 10f;
                else
                    _gainIntegrator.IntegratorGain = (1f / 44100f) * 1500f;
            }
        }

        public float LastGain
        {
            get { return Math.Min(_gain, _maxGain); }
        }

        public float AverageAmplitude
        {
            get { return _state / _sampleCount; }
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
            _state = 0f;
            _samples = new float[200];
            _isAdaptAllowed = true;

            _gainIntegrator = new Integrator(1f, (1f / 44100f) * 1500f);
            _gainIntegrator.Preload(1.0f);
            _gain = 1.0f;
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
            //float avgVol = Math.Max(Math.Abs(value1), Math.Abs(value2));
            //avgVol = (Math.Abs(value1) + Math.Abs(value2)) / 2f;

            //_state = (_state * 0.9995f) + (avgVol * 0.0005f);




            //float avgAmplitude = _state / _sampleCount;

            //float gain = _gainIntegrator.Process(Math.Min(_targetAmp / avgAmplitude, _maxGain));

            //value1 *= Math.Min(gain, _maxGain);
            //value2 *= Math.Min(gain, _maxGain);

            //float postVol = Math.Max(Math.Abs(value1), Math.Abs(value2));

            //_stateShort = (_stateShort * 0.80f) + (postVol * 0.20f);


            //value1 = Math.Max(-1.0f, Math.Min(value1, 1.0f));
            //value2 = Math.Max(-1.0f, Math.Min(value2, 1.0f));



            // Apply gain
            value1 *= _gain;
            value2 *= _gain;

            // Compute average output volume
            //float outputVol = Math.Max(Math.Abs(value1), Math.Abs(value2));
            float outputVol = (Math.Abs(value1) + Math.Abs(value2)) / 2f;

            if (_sampleCount > 0)
                _state -= _samples[(_sampleIndex + 1) == _samples.Length ? 0 : (_sampleIndex + 1)];

            InsertSample(outputVol);

            _state += outputVol;

            outputVol = (float)/*Math.Sqrt*/(_state / _sampleCount);

            //_state = (_state * 0.9995f) + ((outputVol * outputVol) * 0.0005f);
            //outputVol = (float)Math.Sqrt(_state);

            // Adjust gain with error value
            float error = _targetAmp - outputVol;

            if (_isAdaptAllowed)
                _gain = _gainIntegrator.Process(error);
        }


        private void InsertSample(float value)
        {
            _samples[_sampleIndex] = value;
            _sampleIndex++;
            if (_sampleIndex == _samples.Length)
                _sampleIndex = 0;

            if (_sampleCount < _samples.Length)
                _sampleCount++;
        }
    }
}
