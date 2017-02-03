using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class ChannelEqualizer
    {
        private float[] _firBufferI;
        private float[] _firBufferQ;
        private float[] _coeffBufferI;
        private float[] _coeffBufferQ;
        private float _lastResultI;
        private float _lastResultQ;


        public float AdaptRate { get; set; }


        public ChannelEqualizer(int taps)
        {
            _firBufferI = new float[taps];
            _firBufferQ = new float[taps];
            _coeffBufferI = new float[taps];
            _coeffBufferQ = new float[taps];

            AdaptRate = 1f;

            // Start equalizer with a no-op filter (1 + 0i)
            _coeffBufferI[0] = 1.0f;
            _coeffBufferQ[0] = 0f;
        }


        public void AddData(float sampleI, float sampleQ)
        {
            // TODO: Change to circular buffer
            ShiftArrayRight(_firBufferI, sampleI);
            ShiftArrayRight(_firBufferQ, sampleQ);
        }

        public void Process(out float resultI, out float resultQ)
        {
            // Run filter
            resultI = 0f;
            resultQ = 0f;
            for (int i = 0; i < _firBufferI.Length; i++)
            {
                float tempI, tempQ;
                ComplexMultiply(_firBufferI[i], _firBufferQ[i], _coeffBufferI[i], _coeffBufferQ[i], out tempI, out tempQ);
                resultI += tempI;
                resultQ += tempQ;
            }

            _lastResultI = resultI;
            _lastResultQ = resultQ;
        }

        public void Update(float decisionSampleI, float decisionSampleQ)
        {
            if (_firBufferI[_firBufferI.Length - 1] == 0f)
                return;

            // Update filter coefficients
            float errorI = decisionSampleI - _lastResultI;
            float errorQ = decisionSampleQ - _lastResultQ;
            float normalization = 1.1755e-38f;

            // Normalize based on the power of the input samples
            // This prevents the amplitude of the input from adversely affecting the stability
            //   of the filter update
            for (int i = 0; i < _coeffBufferI.Length; i++)
                normalization += (_firBufferI[i] * _firBufferI[i]) + (_firBufferQ[i] * _firBufferQ[i]);

            for (int i = 0; i < _coeffBufferI.Length; i++)
            {
                float tempI, tempQ;
                // _firBufferQ is inverted to swap the multiply operation's signs from +, - to -, +
                ComplexMultiply(_firBufferI[i], -_firBufferQ[i], errorI, errorQ, out tempI, out tempQ);
                tempI *= AdaptRate;
                tempQ *= AdaptRate;

                tempI /= normalization;
                tempQ /= normalization;

                _coeffBufferI[i] += tempI;
                _coeffBufferQ[i] += tempQ;

                //_coeffBufferI[i] += _firBufferI[i] * errorI;
                //_coeffBufferQ[i] += _firBufferQ[i] * errorQ;

                //float tempI = AdaptRate * ((errorI * _firBufferI[i]) + (errorQ * _firBufferQ[i]));
                //float tempQ = AdaptRate * ((errorQ * _firBufferI[i]) - (errorI * _firBufferQ[i]));
                //tempI /= normalization;
                //tempQ /= normalization;

                //_coeffBufferI[i] += tempI;
                //_coeffBufferQ[i] += tempQ;
            }
        }

        public void DumpCoeff()
        {
            Console.Write("I: ");
            for (int i = 0; i < _coeffBufferI.Length; i++)
            {
                Console.Write("{0,7:F4} ", _coeffBufferI[i]);
            }
            Console.WriteLine();
            Console.Write("Q: ");
            for (int i = 0; i < _coeffBufferI.Length; i++)
            {
                Console.Write("{0,7:F4} ", _coeffBufferQ[i]);
            }
            Console.WriteLine();
        }

        public void SetCoefficients(float[] coeffI, float[] coeffQ)
        {
            Array.Copy(coeffI, _coeffBufferI, _coeffBufferI.Length);
            Array.Copy(coeffQ, _coeffBufferQ, _coeffBufferQ.Length);
        }


        private static void ShiftArrayLeft<T>(T[] array, T newValue)
        {
            for (int i = 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }
            array[array.Length - 1] = newValue;
        }

        private static void ShiftArrayRight<T>(T[] array, T newValue)
        {
            for (int i = array.Length-2; i >= 0; i--)
            {
                array[i + 1] = array[i];
            }
            array[0] = newValue;
        }

        private static void ComplexMultiply(float aR, float aI, float bR, float bI, out float resultR, out float resultI)
        {
            resultR = (aR * bR) - (aI * bI);
            resultI = (aR * bI) + (aI * bR);
        }

        private void BuildImpulseResponse(int lengthSymbols, float alpha)
        {
            //_coeffBufferI = new float[lengthSymbols * 2];
            //_firBufferI = new float[_coeffBufferI.Length];
            float samplesPerSymbol = 2;

            float maxValue = 0f;
            for (int i = 0; i < _coeffBufferI.Length; i++)
            {
                float time = (i - _coeffBufferI.Length / 2) / samplesPerSymbol;
                _coeffBufferI[i] = RRCStep(time, alpha);
                _coeffBufferQ[i] = _coeffBufferI[i];
                //_coeffBufferI[i] *= (float)WelchWindow(i, _impulseResponse.Length);
                maxValue = Math.Max(maxValue, _coeffBufferI[i]);
            }

            // Normalize to a maximum of 1
            float scale = 1f / maxValue;
            for (int i = 0; i < _coeffBufferI.Length; i++)
            {
                _coeffBufferI[i] *= scale;
                _coeffBufferQ[i] *= scale;
            }
        }

        private static float RRCStep(float symbolTime, float alpha)
        {
            symbolTime += 0.0000001f;

            //double symbolIndex = time / symbolLengthSeconds;

            double part1 = (Math.Sin(Math.PI * symbolTime * (1 - alpha)) + 4 * alpha * symbolTime * Math.Cos(Math.PI * symbolTime * (1 + alpha)));
            double part2 = Math.PI * symbolTime * (1 - Math.Pow(4 * alpha * symbolTime, 2));

            double response = part1 / part2;

            return (float)response;
        }
    }
}
