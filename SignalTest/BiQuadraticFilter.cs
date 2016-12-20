using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalTest
{
    public class BiQuadraticFilter
    {
        public enum Type
        {
            BANDPASS, LOWPASS, HIGHPASS, NOTCH, PEAK, LOWSHELF, HIGHSHELF
        };

        double a0, a1, a2, b0, b1, b2;
        double x1, x2, y, y1, y2;
        double gain_abs;
        Type type;
        double center_freq, sample_rate, Q, gainDB;

        public BiQuadraticFilter()
        {
        }

        public BiQuadraticFilter(Type type, double center_freq, double sample_rate, double Q, double gainDB)
        {
            configure(type, center_freq, sample_rate, Q, gainDB);
        }

        // constructor without gain setting
        public BiQuadraticFilter(Type type, double center_freq, double sample_rate, double Q)
        {
            configure(type, center_freq, sample_rate, Q, 0);
        }

        public void reset()
        {
            x1 = x2 = y1 = y2 = 0;
        }

        public double frequency()
        {
            return center_freq;
        }

        public void configure(Type type, double center_freq, double sample_rate, double Q, double gainDB)
        {
            reset();
            Q = (Q == 0) ? 1e-9 : Q;
            this.type = type;
            this.sample_rate = sample_rate;
            this.Q = Q;
            this.gainDB = gainDB;
            reconfigure(center_freq);
        }

        public void configure(Type type, double center_freq, double sample_rate, double Q)
        {
            configure(type, center_freq, sample_rate, Q, 0);
        }

        // allow parameter change while running
        public void reconfigure(double cf)
        {
            center_freq = cf;
            // only used for peaking and shelving filters
            gain_abs = Math.Pow(10, gainDB / 40);
            double omega = 2 * Math.PI * cf / sample_rate;
            double sn = Math.Sin(omega);
            double cs = Math.Cos(omega);
            double alpha = sn / (2 * Q);
            double beta = Math.Sqrt(gain_abs + gain_abs);
            switch (type)
            {
                case Type.BANDPASS:
                    b0 = alpha;
                    b1 = 0;
                    b2 = -alpha;
                    a0 = 1 + alpha;
                    a1 = -2 * cs;
                    a2 = 1 - alpha;
                    break;
                case Type.LOWPASS:
                    b0 = (1 - cs) / 2;
                    b1 = 1 - cs;
                    b2 = (1 - cs) / 2;
                    a0 = 1 + alpha;
                    a1 = -2 * cs;
                    a2 = 1 - alpha;
                    break;
                case Type.HIGHPASS:
                    b0 = (1 + cs) / 2;
                    b1 = -(1 + cs);
                    b2 = (1 + cs) / 2;
                    a0 = 1 + alpha;
                    a1 = -2 * cs;
                    a2 = 1 - alpha;
                    break;
                case Type.NOTCH:
                    b0 = 1;
                    b1 = -2 * cs;
                    b2 = 1;
                    a0 = 1 + alpha;
                    a1 = -2 * cs;
                    a2 = 1 - alpha;
                    break;
                case Type.PEAK:
                    b0 = 1 + (alpha * gain_abs);
                    b1 = -2 * cs;
                    b2 = 1 - (alpha * gain_abs);
                    a0 = 1 + (alpha / gain_abs);
                    a1 = -2 * cs;
                    a2 = 1 - (alpha / gain_abs);
                    break;
                case Type.LOWSHELF:
                    b0 = gain_abs * ((gain_abs + 1) - (gain_abs - 1) * cs + beta * sn);
                    b1 = 2 * gain_abs * ((gain_abs - 1) - (gain_abs + 1) * cs);
                    b2 = gain_abs * ((gain_abs + 1) - (gain_abs - 1) * cs - beta * sn);
                    a0 = (gain_abs + 1) + (gain_abs - 1) * cs + beta * sn;
                    a1 = -2 * ((gain_abs - 1) + (gain_abs + 1) * cs);
                    a2 = (gain_abs + 1) + (gain_abs - 1) * cs - beta * sn;
                    break;
                case Type.HIGHSHELF:
                    b0 = gain_abs * ((gain_abs + 1) + (gain_abs - 1) * cs + beta * sn);
                    b1 = -2 * gain_abs * ((gain_abs - 1) + (gain_abs + 1) * cs);
                    b2 = gain_abs * ((gain_abs + 1) + (gain_abs - 1) * cs - beta * sn);
                    a0 = (gain_abs + 1) - (gain_abs - 1) * cs + beta * sn;
                    a1 = 2 * ((gain_abs - 1) - (gain_abs + 1) * cs);
                    a2 = (gain_abs + 1) - (gain_abs - 1) * cs - beta * sn;
                    break;
            }
            // prescale flter constants
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;
        }

        // provide a static amplitude result for testing
        public double result(double f)
        {
            double phi = Math.Pow((Math.Sin(2.0 * Math.PI * f / (2.0 * sample_rate))), 2.0);
            return (Math.Pow(b0 + b1 + b2, 2.0) - 4.0 * (b0 * b1 + 4.0 * b0 * b2 + b1 * b2) * phi + 16.0 * b0 * b2 * phi * phi) / (Math.Pow(1.0 + a1 + a2, 2.0) - 4.0 * (a1 + 4.0 * a2 + a1 * a2) * phi + 16.0 * a2 * phi * phi);
        }

        // provide a static decibel result for testing
        public double log_result(double f)
        {
            double r;
            try
            {
                r = 10 * Math.Log10(result(f));
            }
            catch (Exception e)
            {
                r = -100;
            }
            if (Double.IsInfinity(r) || Double.IsNaN(r))
            {
                r = -100;
            }
            return r;
        }

        // return the constant set for this filter
        public double[] constants()
        {
            return new double[] { b0, b1, b2, a1, a2 };
        }

        // perform one filtering step
        public double filter(double x)
        {
            y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1;
            x1 = x;
            y2 = y1;
            y1 = y;
            return (y);
        }
    }
}
