using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    public class Downsampler
    {
        private double _inputSampleRate;
        private float[] _firBuffer;
        private float _ratio;
        private float _invRatio;
        private float _lastSample;
        private float _sampleCounter;


        public Downsampler(double inputSampleRate)
        {
            _inputSampleRate = inputSampleRate;
            _firBuffer = new float[9];

            _sampleCounter = 0f;
            SetRatio(1f);
        }


        public void SupplyInput(float sample)
        {
            ShiftArrayLeft(_firBuffer, sample);
        }

        public bool Next()
        {
            bool returnVal = false;
            if (_ratio <= 1f)
            {
                if (_sampleCounter < 1f)
                {
                    float result = 0f;
                    for (int i = 0; i < _firBuffer.Length; i++)
                    {
                        result += (float)(Sinc((i - (_firBuffer.Length / 2)) - (_sampleCounter - (int)_sampleCounter), 1.0) * _firBuffer[i]);
                    }

                    _lastSample = result;

                    _sampleCounter += _invRatio;
                    returnVal = true;
                }
                _sampleCounter--;
            }
            else
            {
                throw new NotImplementedException("Raising the sampling rate is currently not supported");
            }

            return returnVal;
        }

        public float GetSample()
        {
            return _lastSample;
        }

        public void SetRatio(float ratio)
        {
            _ratio = ratio;
            _invRatio = 1f / _ratio;
        }


        private static double Sinc(double x, double scale)
        {
            if (scale == 0)
                scale = 1;
            if (x == 0)
                return 1;

            double phi = Math.PI / scale;
            return Math.Sin(phi * x) / (phi * x);
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
