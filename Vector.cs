using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Vector
    {
        public float x = 0;
        public float y = 0;

        public Vector(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector() { }

        public float SquaredDistance()
        {
            return x * x + y * y;
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.x - b.x, a.y - b.y);
        }

        public static Vector operator *(Vector a, Vector b)
        {
            return new Vector(a.x * b.x, a.y * b.y);
        }

        public static Vector operator /(Vector a, Vector b)
        {
            return new Vector(a.x / b.x, a.y / b.y);
        }

        public static Vector operator *(Vector a, float b)
        {
            return new Vector(a.x * b, a.y * b);
        }

        public static Vector operator *(float a, Vector b)
        {
            return new Vector(a * b.x, a * b.y);
        }

        public static Vector operator /(Vector a, float b)
        {
            return new Vector(a.x / b, a.y / b);
        }
    }
}
