using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Costas
    {
        private int _sampleRate;
        private Vco _vco;
        private BiQuadraticFilter _iArmFilter;
        private BiQuadraticFilter _qArmFilter;
        private BiQuadraticFilter _iNotchFilter;
        private BiQuadraticFilter _qNotchFilter;
        private Integrator _piPhase;
        private float _iSample;
        private float _qSample;
        private float _errSample;
        private float _lockSample;
        private BiQuadraticFilter _lockFilter;
        

        public double CarrierFrequency
        {
            get { return _vco.GetFrequency(); }
            set {
                _vco.SetCenterFrequency(value);
                _iNotchFilter.reconfigure(value * 2);
                _qNotchFilter.reconfigure(value * 2);
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
            get { return _iArmFilter.frequency(); }
            set
            {
                _iArmFilter.reconfigure(value);
                _qArmFilter.reconfigure(value);
            }
        }

        public float PreportionalGain
        {
            get { return _piPhase.ProportionalGain; }
            set { _piPhase.ProportionalGain = value; }
        }

        public bool IsLocked
        {
            get { return Math.Abs(_lockSample) <= 0.05f; }
        }


        public Costas(int sampleRate, float carrierFrequency)
        {
            _sampleRate = sampleRate;
            _vco = new Vco(sampleRate, carrierFrequency, 50);
            _lockFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 50, sampleRate, 0.707);


            _iArmFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 63, sampleRate, 0.707);
            _qArmFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 63, sampleRate, 0.707);

            // Notch filters at 2x carrier frequency
            _iNotchFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, carrierFrequency * 2, sampleRate, 1.0);
            _qNotchFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, carrierFrequency * 2, sampleRate, 1.0);

            _piPhase = new Integrator(0.707f, (1f / sampleRate) * 10f);
        }


        public void Process(float sample)
        {
            // Increment VCO
            _vco.Next();

            // Multiply with oscillator
            float signalI = (float)_vco.Cos() * sample;
            float signalQ = -(float)_vco.Sin() * sample;

            // TODO: Do we need to clip?  Should be handled by EQ someday

            // Take our demodulated data out before the arm filters
            _iSample = signalI;
            _qSample = signalQ;

            // Notch out 2x carrier
            signalI = (float)_iNotchFilter.filter(signalI);
            signalQ = (float)_qNotchFilter.filter(signalQ);

            // Low-pass to get phase error
            float phaseI = (float)_iArmFilter.filter(signalI);
            float phaseQ = (float)_qArmFilter.filter(signalQ);


            // To recover the carrier of a QPSK signal, we must multiply the phase
            //   error of the other arm with the sign of our arm, then subtract I and Q
            //   errors to get the carrier phase error
            float phaseMixI = Math.Sign(phaseI) * phaseQ;
            float phaseMixQ = Math.Sign(phaseQ) * phaseI;
            float phaseError = phaseMixI - phaseMixQ;
            _errSample = phaseError;

            // Low-pass the non-integrated error signal to determine if the loop is locked
            _lockSample = (float)_lockFilter.filter(phaseError);

            phaseError = _piPhase.Process(phaseError);

            // Tune the VCO to correct the phase and frequency errors
            _vco.Tune(phaseError);
        }

        public float Inphase()
        {
            return _iSample;
        }

        public float Quadrature()
        {
            return _qSample;
        }

        public float InphaseCarrier()
        {
            return (float)_vco.Cos();
        }

        public float QuadratureCarrier()
        {
            return -(float)_vco.Sin();
        }

        public float Error()
        {
            return _errSample;
        }

        public float ErrorIntegral()
        {
            return _piPhase.GetValue();
        }

        public float ErrorIntegral2()
        {
            return _piPhase.GetIntegratorValue();
        }

        public float LockSignal()
        {
            return _lockSample;
        }

        public void Reset()
        {
            // Reset all filters to remove past data
            _iArmFilter.reset();
            _qArmFilter.reset();
            _lockFilter.reset();

            _iSample = _qSample = 0f;
            _lockSample = 0f;
            _errSample = 0f;

            _vco.Reset();
        }
    }
}
