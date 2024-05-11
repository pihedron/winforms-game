using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Block
    {
        public Vector pos;
        public Vector dim;
        public bool isClose = false;

        public Block(Vector pos, Vector dim)
        {
            this.pos = pos;
            this.dim = dim;
        }
    }
}
