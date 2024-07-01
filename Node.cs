using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Node : ICloneable
    {
        public int[] children = Array.Empty<int>();
        public Vector pos;
        public bool isActivated = false;
        public string? gateName;

        public static Dictionary<string, (Color color, Func<bool, bool, bool> eval)> gates = new()
        {
            {
                "AND",
                (Color.Blue, (bool a, bool b) => a && b)
            },
            {
                "OR",
                (Color.Red, (bool a, bool b) => a || b)
            },
            {
                "XOR",
                (Color.Lime, (bool a, bool b) => a ^ b)
            },
            {
                "NAND",
                (Color.Cyan, (bool a, bool b) => !(a && b))
            },
            {
                "NOR",
                (Color.Magenta, (bool a, bool b) => !(a || b))
            },
            {
                "XNOR",
                (Color.Yellow, (bool a, bool b) => !(a ^ b))
            },
        };
        public static int size = 32;
        public Vector dim = new(size, size);

        public Node(Vector pos, int[] children)
        {
            this.pos = pos;
            this.children = children;
        }

        public Node(Vector pos, bool isActivated)
        {
            this.pos = pos;
            this.isActivated = isActivated;
        }

        public Node(Vector pos, string gateName, int[] children)
        {
            this.pos = pos;
            this.gateName = gateName;
            this.children = children;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
