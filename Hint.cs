using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Hint
    {
        public Vector pos;
        public Vector target;
        public string message;
        public const int radius = Game.size * 4;

        public Hint(Vector pos, Vector target, string message)
        {
            this.pos = pos;
            this.target = target;
            this.message = message;
        }
    }
}
