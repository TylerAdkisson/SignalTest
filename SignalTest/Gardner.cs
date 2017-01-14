using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Gardner
    {
        private Integrator _piError;
        private bool _flipFlop;
        private float _prevSample;
        private float _currentSample;
        private float _middleSample;


        public Gardner(float baudRate)
        {
            _piError = new Integrator(0.03f/*0.125f/4*//*0.063f/2*/, 1f / (baudRate * 1f));
        }


        //private float _lastErr;
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

                    float preError = error;
                    error = _piError.Process(error);
                    //Console.WriteLine("E {0,7:F4} {1,8:F4}", error, preError);
                    //_lastErr = error;
                }
            }
            //_flipFlop ^= true;

            //return _lastErr;
            return _piError.GetValue();
        }
    }
}
