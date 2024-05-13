using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Block(Vector pos, Vector dim) : Box(pos, dim)
    {
        public bool isClose = false;
    }
}
