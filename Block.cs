using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Block : Box
    {
        public bool isClose = false;
        public bool isDangerous = false;

        public Block(Vector pos, Vector dim) : base(pos, dim) { }

        public Block(Vector pos, Vector dim, bool isDangerous) : base(pos, dim)
        {
            this.isDangerous = isDangerous;
        }
    }
}
