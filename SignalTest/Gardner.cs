using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Gardner
    {
        private bool _flipFlop;
        private float _prevSampleI;
        private float _prevSampleQ;
        private float _currentSampleI;
        private float _currentSampleQ;
        private float _middleSampleI;
        private float _middleSampleQ;


        public Gardner()
        {
        }


        public float Process(float sample)
        {
            return Process(sample, 0);
        }

        public float Process(float sampleI, float sampleQ)
        {
            // Shift symbol samples over
            _prevSampleI = _middleSampleI;
            _prevSampleQ = _middleSampleQ;
            _middleSampleI = _currentSampleI;
            _middleSampleQ = _currentSampleQ;
            _currentSampleI = sampleI;
            _currentSampleQ = sampleQ;

            if ((_flipFlop ^= true))
            {
                // Every other sample, calculate error, but only if there is a transition
                // using Jonti's (modified gardner) method for M-QAM and M-PAM timing recovery
                // This method also works for QPSK and BPSK, and 2-PAM as well
                float error = 0f;

                // Calculate for I
                if (Math.Sign(_prevSampleI) != Math.Sign(_currentSampleI))
                {
                    float localError = 0.5f * (_currentSampleI + _prevSampleI) - _middleSampleI;
                    if (_currentSampleI > _prevSampleI)
                    {
                        localError = -localError;
                    }

                    error += localError;
                }

                // Calculate for Q
                if (sampleQ != 0f && Math.Sign(_prevSampleQ) != Math.Sign(_currentSampleQ))
                {
                    float localError = 0.5f * (_currentSampleQ + _prevSampleQ) - _middleSampleQ;
                    if (_currentSampleQ > _prevSampleQ)
                    {
                        localError = -localError;
                    }

                    error += localError;
                }
                return error;
            }

            return 0;
        }
    }
}
