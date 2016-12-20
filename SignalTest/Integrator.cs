﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Integrator
    {
        private float _iState;
        private float _iGain;
        private float _pGain;
        private float _lastValue;


        public float IntegratorGain
        {
            get { return _iGain; }
            set { _iGain = value; }
        }

        public float ProportionalGain
        {
            get { return _pGain; }
            set { _pGain = value; }
        }


        public Integrator(float pGain, float iGain)
        {
            _iGain = iGain;
            _pGain = pGain;
        }


        public float Process(float sample)
        {
            _iState += sample;
            float pTerm = sample * _pGain;

            return _lastValue = (pTerm + (_iState * _iGain));
        }

        public float GetValue()
        {
            return _lastValue;
        }

        public float GetIntegratorValue()
        {
            return _iState * _iGain;
        }

        public void SetValue(float value)
        {
            _iState = (value / _iGain);
        }
    }
}