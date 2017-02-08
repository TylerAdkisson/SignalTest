using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class PseudoRandom
    {
        private uint _state;


        public PseudoRandom()
            : this((uint)DateTime.Now.Ticks)
        {
        }

        public PseudoRandom(uint seed)
        {
            _state = seed;
        }


        public uint Next()
        {
            uint output = _state;
            output ^= output << 13;
            output ^= output >> 17;
            output ^= output << 5;
            _state = output;
            return output;
        }

        public uint Next(int min, int max)
        {
            uint val = Next();
            val %= (uint)max - (uint)min;
            val += (uint)min;

            return val;
        }
    }
}
