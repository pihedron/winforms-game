using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Box
    {
        public Vector pos;
        public Vector dim;

        public Box(Vector pos, Vector dim) { this.pos = pos; this.dim = dim; }

        public bool IsIntersecting(Box box)
        {
            return pos.x + dim.x / 2 > box.pos.x - box.dim.x / 2 && pos.x - dim.x / 2 < box.pos.x + box.dim.x / 2 && pos.y + dim.y / 2 > box.pos.y - box.dim.y / 2 && pos.y - dim.y / 2 < box.pos.y + box.dim.y / 2;
        }

        public bool IsIntersecting(Vector pos, Vector dim)
        {
            return this.pos.x + this.dim.x / 2 > pos.x - dim.x / 2 && this.pos.x - this.dim.x / 2 < pos.x + dim.x / 2 && this.pos.y + this.dim.y / 2 > pos.y - dim.y / 2 && this.pos.y - this.dim.y / 2 < pos.y + dim.y / 2;
        }
    }
}
