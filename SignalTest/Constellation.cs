using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest
{
    class Constellation
    {
        public struct Point
        {
            public double I;
            public double Q;
            public int Value;

            public Point(double i, double q)
                : this(i, q, 0)
            {
            }

            public Point(double i, double q, int value)
            {
                I = i;
                Q = q;
                Value = value;
            }
        }


        private Dictionary<int, Point> _bitMap;

        public int PointCount { get { return Points == null ? 0 : Points.Length; } }
        public Point[] Points { get; private set; }


        public Constellation()
        {
        }


        public static Constellation CreateSquare(int pointsPerAxis)
        {
            double diffBetweenPoint = 2.0 / (pointsPerAxis - 1);
            int totalPoints = pointsPerAxis * pointsPerAxis;
            Point[] constPts = new Point[totalPoints];
            int constIndex = 0;
            for (int i = 0; i < pointsPerAxis; i++)
            {
                double valI = (i * diffBetweenPoint) - 1.0;
                for (int q = 0; q < pointsPerAxis; q++, constIndex++)
                {
                    double valQ = (q * diffBetweenPoint) - 1.0;
                    constPts[constIndex] = new Constellation.Point(valI, valQ);
                }
            }

            Constellation constellation = new Constellation();
            constellation.SetPoints(constPts);
            return constellation;
        }

        public void SetPoints(Point[] points)
        {
            Points = new Point[points.Length];
            Array.Copy(points, Points, points.Length);
        }

        public Point FindNearestPoint(double i, double q)
        {
            Point nearest = default(Point);
            double nearestDist = double.MaxValue;
            //double nearestI = 0;
            //double nearestQ = 0;

            //// Find closest I
            //for (int p = 0; p < Points.Length; p++)
            //{
            //    double dist = Math.Abs(Points[p].I - i);
            //    if (dist < nearestDist)
            //    {
            //        nearestDist = dist;
            //        nearestI = Points[p].I;
            //    }
            //}

            //nearestDist = double.MaxValue;
            //// Find closest Q
            //for (int p = 0; p < Points.Length; p++)
            //{
            //    double dist = Math.Abs(Points[p].Q - q);
            //    if (dist < nearestDist)
            //    {
            //        nearestDist = dist;
            //        nearestQ = Points[p].Q;
            //    }
            //}

            //return new Point(nearestI, nearestQ);

            for (int p = 0; p < Points.Length; p++)
            {
                Point pt = Points[p];
                double a = i - pt.I;
                double b = q - pt.Q;

                // We skip the square root here, as we are just doing a distance comparison
                double dist = (a * a) + (b * b);

                if (dist < nearestDist)
                {
                    nearest = pt;
                    nearestDist = dist;
                }
            }

            return nearest;
        }

        public void RotateDegrees(double degrees)
        {
            double radians = degrees * Math.PI / 180;
            double rotReal = Math.Cos(radians);
            double rotImag = Math.Sin(radians);

            // Rotate all points around origin (0, 0)
            for (int i = 0; i < Points.Length; i++)
            {
                ComplexMultiply(Points[i].I, Points[i].Q, rotReal, rotImag, out Points[i].I, out Points[i].Q);
            }
        }

        public void Scale(double magnitude)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                ComplexMultiply(Points[i].I, Points[i].Q, magnitude, 0, out Points[i].I, out Points[i].Q);
            }
        }

        public bool MapValue(int value, out Point point)
        {
            bool result = _bitMap.TryGetValue(value, out point);

            return result;
        }

        public void PrepareGeneration()
        {
            _bitMap = new Dictionary<int, Point>();
            for (int i = 0; i < Points.Length; i++)
            {
                _bitMap[Points[i].Value] = Points[i];
            }
        }

        public static int BinaryToGray(int binary)
        {
            return binary ^ (binary >> 1);
        }


        private static void ComplexMultiply(double aR, double aI, double bR, double bI, out double resultR, out double resultI)
        {
            resultR = (aR * bR) - (aI * bI);
            resultI = (aR * bI) + (aI * bR);
        }
    }
}
