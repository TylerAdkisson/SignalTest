using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    public class Vco
    {
        private struct State
        {
            public double Phase;
            public double Multiplier;
            public double PhaseOffset;
        }

        private const double TwoPi = 2 * Math.PI;
        private int _sampleRate;
        private double _span;
        private double _baseFq;
        private double _fq;
        private State[] _states;


        public Vco(int sampleRate, double frequencyHz) : this(sampleRate, frequencyHz, 0)
        {
        }

        public Vco(int sampleRate, double frequencyHz, double spanHz)
        {
            _sampleRate = sampleRate;
            _baseFq = _fq = TwoPi * frequencyHz / sampleRate;
            _span = TwoPi * spanHz / sampleRate;

            _states = new State[1];
            _states[0] = new State()
            {
                Multiplier = 1.0,
                Phase = 0.0
            };
        }


        public void Reset()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Phase = 0;
            }

            _fq = _baseFq;
        }

        public void Next()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                //_states[i].Phase -= _states[i].PhaseOffset;
                _states[i].Phase += _fq * _states[i].Multiplier;
                //_states[i].Phase += _states[i].PhaseOffset;

                if (_states[i].Phase > TwoPi)
                    _states[i].Phase -= TwoPi;
            }
        }

        public double Sin(int phaseIndex = 0)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            return Math.Sin(_states[phaseIndex].Phase);
        }

        public double Cos(int phaseIndex = 0)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            return Math.Cos(_states[phaseIndex].Phase);
        }

        public double Square(int phaseIndex = 0)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            return _states[phaseIndex].Phase > Math.PI ? 1.0 : -1.0;
        }

        public double SquareCos(int phaseIndex = 0)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            double phase = _states[phaseIndex].Phase;
            double halfPi = Math.PI / 2.0;
            return (phase > Math.PI + halfPi || phase < Math.PI - halfPi) ? 1.0 : -1.0;
        }


        public void Tune(double frequencyPercent)
        {
            if (_span == 0)
                return;

            _fq = _baseFq + (_span * frequencyPercent);
            ContainSpan();
        }

        public void SetCenterFrequency(double frequencyHz)
        {
            _baseFq = _fq = TwoPi * frequencyHz / _sampleRate;
        }

        public void SetSpanWidth(double widthHz)
        {
            _span = TwoPi * widthHz / _sampleRate;
        }

        public double GetSpanWidth()
        {
            return (_span * _sampleRate) / TwoPi;
        }

        public int AddMultiplier(double multiplier)
        {
            State state = new State();
            state.Multiplier = multiplier;
            state.Phase = 0.0;

            // Resize state array
            State[] oldStates = _states;
            _states = new State[_states.Length + 1];
            Array.Copy(oldStates, _states, oldStates.Length);
            _states[_states.Length - 1] = state;

            return _states.Length - 1;
        }

        public void ChangeMultiplier(int phaseIndex, double multiplier)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            _states[phaseIndex].Multiplier = multiplier;
        }

        public double GetFrequency(int phaseIndex = 0)
        {
            //(fq * sampleRate) / (Math.PI * 2)
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            return (_fq * _states[phaseIndex].Multiplier * _sampleRate) / TwoPi;
        }

        public void SetPhaseOffset(int phaseIndex, double offsetRadians)
        {
            if (phaseIndex >= _states.Length || phaseIndex < 0)
                throw new IndexOutOfRangeException("The specified phase index is out of range");

            _states[phaseIndex].PhaseOffset = offsetRadians;
            _states[phaseIndex].Phase += offsetRadians;
        }

        private void ContainSpan()
        {
            if (_fq > _baseFq + _span)
                _fq = _baseFq + _span;
            else if (_fq < _baseFq - _span)
                _fq = _baseFq - _span;
        }
    }
}
