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
        private float _prevSample;
        private float _currentSample;
        private float _middleSample;


        public Gardner(float baudRate)
        {
        }


        public float Process(float sample)
        {
            // Shift symbol samples over
            _prevSample = _middleSample;
            _middleSample = _currentSample;
            _currentSample = sample;

            if ((_flipFlop ^= true))
            {
                // Every other sample, calculate error, but only if there is a transition
                // using Jonti's (modified gardner) method for M-QAM and M-PAM timing recovery
                // This method also works for QPSK and BPSK, and 2-PAM as well
                if (Math.Sign(_prevSample) != Math.Sign(_currentSample))
                {
                    float error = 0.5f * (_currentSample + _prevSample) - _middleSample;
                    if (_currentSample > _prevSample)
                    {
                        error = -error;
                    }

                    return error;
                }
            }

            return 0;
        }
    }
}
