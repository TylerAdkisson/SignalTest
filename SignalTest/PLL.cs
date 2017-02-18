using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class PLL
    {
        private int _sampleRate;
        private Vco _vco;
        private BiQuadraticFilter _armFilter;
        private BiQuadraticFilter _notchFilter;
        private Integrator _piPhase;
        private float _errSample;
        private float _lockSample;
        private BiQuadraticFilter _lockFilter;
        private bool _isTrackingEnabled;
        private float _errScale;
        private bool _useIntegrator;


        public double CarrierFrequency
        {
            get { return _vco.GetFrequency(); }
            set
            {
                _vco.SetCenterFrequency(value);
                _notchFilter.reconfigure(value * 2);
            }
        }

        public double SpanHz
        {
            get { return _vco.GetSpanWidth(); }
            set
            {
                _vco.SetSpanWidth(value);
                _lockFilter.reconfigure(value);
            }
        }

        public double ArmFilterHz
        {
            get { return _armFilter.frequency(); }
            set { _armFilter.reconfigure(value); }
        }

        public float ProportionalGain
        {
            get { return _piPhase.ProportionalGain; }
            set { _piPhase.ProportionalGain = value; }
        }

        public float IntegrationGain
        {
            get { return _piPhase.IntegratorGain; }
            set { _piPhase.IntegratorGain = value; }
        }

        public float ErrorScaler
        {
            get { return _errScale; }
            set { _errScale = value; }
        }

        public bool IsLocked
        {
            get { return Math.Abs(_lockSample) <= 0.05f; }
        }

        public bool IsTrackingEnabled
        {
            get { return _isTrackingEnabled; }
            set { _isTrackingEnabled = value; }
        }


        /// <summary>
        /// Creates a new Phase-Locked Loop
        /// </summary>
        /// <param name="sampleRate">The sampling rate of the signal stream</param>
        /// <param name="carrierFrequency">The center frequency of the PLL</param>
        /// <param name="spanHz">VCO bandwidth</param>
        /// <param name="useIntegrator">If true, an the phase error is integrated (type 2 PLL), otherwise is a type 1 PLL</param>
        public PLL(int sampleRate, float carrierFrequency, float spanHz, bool useIntegrator)
        {
            _sampleRate = sampleRate;
            _vco = new Vco(sampleRate, carrierFrequency, spanHz);
            _lockFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 50, sampleRate, 0.707);

            _armFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 63, sampleRate, 0.707);

            // Notch filter at 2x carrier frequency
            _notchFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, Math.Min(carrierFrequency * 2, (sampleRate / 2) - 10), sampleRate, 0.707);

            _piPhase = new Integrator(0.707f, (1f / sampleRate) * 10f);

            _isTrackingEnabled = true;
            _errScale = 1f;
            _useIntegrator = useIntegrator;
        }


        public void Process(float sample)
        {
            // Increment VCO
            _vco.Next();

            // Multiply with oscillator
            float signal = (float)_vco.Cos() * sample;
            signal *= _errScale;
            _errSample = signal;

            // Notch out 2x carrier
            signal = (float)_notchFilter.filter(signal);

            // Low-pass to get phase error
            float phaseError = (float)_armFilter.filter(signal);

            //phaseError *= _errScale;
            //_errSample = phaseError;


            // Low-pass the non-integrated error signal to determine if the loop is locked
            _lockSample = (float)_lockFilter.filter(phaseError);

            if (_useIntegrator)
                phaseError = _piPhase.Process(phaseError);

            // Tune the VCO to correct the phase and frequency errors
            if (_isTrackingEnabled)
                _vco.Tune(phaseError);
        }

        public float Carrier()
        {
            return (float)_vco.Cos();
        }

        public float Error()
        {
            return _errSample;
        }

        public float LockSignal()
        {
            return _lockSample;
        }

        public void Reset()
        {
            // Reset all filters to remove past data
            _armFilter.reset();
            _lockFilter.reset();

            _lockSample = 0f;
            _errSample = 0f;

            _vco.Reset();
        }
    }
}
