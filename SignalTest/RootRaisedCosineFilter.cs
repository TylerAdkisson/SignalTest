using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class RootRaisedCosineFilter
    {
        private float[] _impulseResponse;
        private float[] _buffer;
        private int _sampleRate;
        private int _lengthSymbols;
        private float _baudRate;
        private float _alpha;
        private float _dcGain;
        private bool _clip;


        public int SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = value; BuildImpulseResponse(); }
        }

        public int LengthSymbols
        {
            get { return _lengthSymbols; }
            set { _lengthSymbols = value; BuildImpulseResponse(); }
        }

        public float BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; BuildImpulseResponse(); }
        }

        public float Alpha
        {
            get { return _alpha; }
            set { _alpha = value; BuildImpulseResponse(); }
        }

        public float DCGain
        {
            get { return _dcGain; }
        }

        public bool ClipPeaks
        {
            get { return _clip; }
            set { _clip = value; }
        }


        public RootRaisedCosineFilter(int sampleRate, int lengthSymbols, float baudRate, float alpha)
        {
            _sampleRate = sampleRate;
            _lengthSymbols = lengthSymbols;
            _baudRate = baudRate;
            _alpha = alpha;
            _clip = false;

            BuildImpulseResponse();
        }


        public float Process(float sample)
        {
            // TODO: Convert this to a circle buffer instead of element shifts
            ShiftArrayLeft(_buffer, sample);
            float result = 0;
            for (int i = 0; i < _impulseResponse.Length; i++)
            {
                result += _buffer[i] * _impulseResponse[i];
            }

            if (_clip)
            {
                if (result > 1f)
                    result = 1f;
                else if (result < -1f)
                    result = -1f;
            }

            return result;
        }


        private void BuildImpulseResponse()
        {
            // Impulse response needs to be large enough to hold an integer amount (or close
            //   enough) of symbols, at the desired sample rate
            _impulseResponse = new float[(int)(_sampleRate * _lengthSymbols / _baudRate)];
            _buffer = new float[_impulseResponse.Length];
            float samplesPerSymbol = _sampleRate / _baudRate;

            float maxValue = 0f;
            for (int i = 0; i < _impulseResponse.Length; i++)
            {
                float time = (i - _impulseResponse.Length / 2) / samplesPerSymbol;
                _impulseResponse[i] = RRCStep(time, _alpha);
                maxValue = Math.Max(maxValue, _impulseResponse[i]);
            }

            // Normalize to a maximum of 1
            float scale = 1f / maxValue;
            _dcGain = 0f;
            for (int i = 0; i < _impulseResponse.Length; i++)
            {
                _impulseResponse[i] *= scale;
                _dcGain += _impulseResponse[i];
            }
        }

        private static float RRCStep(float symbolTime, float alpha)
        {
            symbolTime += 0.000000001f;

            //double symbolIndex = time / symbolLengthSeconds;

            double part1 = (Math.Sin(Math.PI * symbolTime * (1 - alpha)) + 4 * alpha * symbolTime * Math.Cos(Math.PI * symbolTime * (1 + alpha)));
            double part2 = Math.PI * symbolTime * (1 - Math.Pow(4 * alpha * symbolTime, 2));

            double response = part1 / part2;

            return (float)response;
        }

        private static void ShiftArrayLeft<T>(T[] array, T newValue)
        {
            for (int i = 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }
            array[array.Length - 1] = newValue;
        }
    }
}
