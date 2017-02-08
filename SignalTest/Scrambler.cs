using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Scrambler
    {
        private bool _isDescrambler;
        private int _state;


        public Scrambler(bool descramble)
            : this(descramble, 0x7AF8AD)
        {
        }

        public Scrambler(bool descramble, int state)
        {
            _isDescrambler = descramble;
            _state = state;
        }


        public int Process(int bit)
        {
            // Multiplicative (de)scrambler
            // Uses the ITU V.34 polynomial x^23 + x^18 + 1
            // The shifts are 1 less to place the bits in the first bit slot
            int output = bit ^ (((_state >> 22) ^ (_state >> 17)) & 1);
            _state = (_state << 1) & 0x7FFFFF;

            if (_isDescrambler)
                _state |= bit;
            else
                _state |= output;

            return output;
        }
    }
}
