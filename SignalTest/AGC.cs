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
            get { return _gain; }
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

            _gainIntegrator = new Integrator(1f, (1f / 44100f) * 1000f);
            _gainIntegrator.SetValue(1.0f);
            _gain = 1.0f;
        }


        public float Process(float value)
        {
            // Apply gain
            value *= _gain;

            ProcessInternal(Math.Abs(value));

            return value;
        }

        public void ProcessDual(ref float value1, ref float value2)
        {
            // Apply gain
            value1 *= _gain;
            value2 *= _gain;

            // Compute average output volume
            //float outputVol = Math.Max(Math.Abs(value1), Math.Abs(value2));
            float outputVol = (Math.Abs(value1) + Math.Abs(value2)) / 2f;

            ProcessInternal(outputVol);
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

        private void ProcessInternal(float value)
        {
            // Add value to running average
            if (_sampleCount > 0)
                _state -= _samples[(_sampleIndex + 1) == _samples.Length ? 0 : (_sampleIndex + 1)];

            InsertSample(value);

            _state += value;

            value = (float)/*Math.Sqrt*/(_state / _sampleCount);

            //_state = (_state * 0.9995f) + ((outputVol * outputVol) * 0.0005f);
            //outputVol = (float)Math.Sqrt(_state);

            // Adjust gain with error value
            float error = _targetAmp - value;

            if (_isAdaptAllowed)
            {
                _gain = _gainIntegrator.Process(error);
                _gain = Math.Max(Math.Min(_gain, _maxGain), 0);
            }
        }
    }
}
