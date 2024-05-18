using System.Windows.Forms;
using System.Xml.Linq;

namespace Game
{
    public partial class Game : Form
    {
        static Dictionary<Keys, bool> kb = new();
        static Dictionary<Keys, bool> shadow = new();
        static SolidBrush brush = new(Color.Black);
        static Pen pen = new(Color.Black);

        const float friction = 0.25F;
        const float gravity = 1;

        static Entity player = new(new Vector(), new Vector(32, 64), 2, 16, "player");
        static Camera cam = new();
        static Vector view = new();
        static List<Block> world = new();
        static Circuit circuit;

        static int tick = 0;

        public Game()
        {
            InitializeComponent();

            // fullscreen settings
            canvas.Dock = DockStyle.Fill;
            AdjustView();

            // event listeners
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            // load assets
            player.GetFrames();
            foreach (var line in File.ReadLines("../../../world.txt"))
            {
                float[] values = line.Split(' ').Select((string val) => float.Parse(val)).ToArray();
                world.Add(new Block(new Vector(values[0], values[1]), new Vector(values[2], values[3])));
            }
            string[] lines = File.ReadLines("../../../circuits.txt").ToArray();
            circuit = new();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(' ');
                switch (values[0])
                {
                    case "SRC":
                        circuit.nodes.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), int.Parse(values[3]) != 0));
                        break;
                    case "BOX":
                        circuit.outputs.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), values[3..].Select((string s) => int.Parse(s)).ToArray()));
                        break;
                    default:
                        circuit.nodes.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), values[0], values[3..].Select((string s) => int.Parse(s)).ToArray()));
                        break;
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = true;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = false;
            shadow[Keys.S] = false;
        }

        private static bool Pressed(Keys key)
        {
            return kb.ContainsKey(key) && kb[key];
        }

        private static bool Down(Keys key)
        {
            return shadow.ContainsKey(key) && shadow[key];
        }

        private void MovePlayer()
        {
            if (Pressed(Keys.D))
            {
                player.vel.x += player.movePower;
            }

            if (Pressed(Keys.A))
            {
                player.vel.x -= player.movePower;
            }

            if (Pressed(Keys.W) && player.isGrounded)
            {
                player.vel.y = -player.jumpHeight;
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            Vector old = player.pos;

            MovePlayer();

            player.vel.y += gravity;

            AnimatePlayer();

            player.pos += player.vel;

            player.vel.x *= 1 - friction;

            HandleCollisions(old);

            cam.pos += (player.pos - cam.pos) / 4;

            if (tick % 5 == 0)
            {
                player.frameIndex++;
            }

            tick++;

            canvas.Invalidate();
        }

        private static void AnimatePlayer()
        {
            if (Math.Abs(player.vel.x) < 1)
            {
                player.state = EntityState.Idle;
            }
            else
            {
                player.state = EntityState.Walk;
                if (player.vel.x < 0)
                {
                    player.isFacingLeft = true;
                }
                else
                {
                    player.isFacingLeft = false;
                }
            }
        }

        private static void HandleCollisions(Vector old)
        {
            player.isGrounded = false;

            if (player.pos.y + player.dim.y / 2 > 0)
            {
                player.vel.y = 0;
                player.pos.y = -player.dim.y / 2;
                player.isGrounded = true;
            }

            foreach (var block in world)
            {
                if (block.isClose && player.IsIntersecting(block))
                {
                    CollideWithBox(old, block);
                }
            }

            foreach (var node in circuit.outputs)
            {
                if (!node.isActivated) continue;
                Box box = new(node.pos, Node.dim);
                if (player.IsIntersecting(box))
                {
                    CollideWithBox(old, box);
                }
            }

            foreach (var node in circuit.nodes)
            {
                if (node.children.Length != 0) continue;
                Box box = new(node.pos, Node.dim);
                if (player.IsIntersecting(box) && Pressed(Keys.S) && !Down(Keys.S))
                {
                    node.isActivated = !node.isActivated;
                    shadow[Keys.S] = true;
                }
            }
        }

        private static void CollideWithBox(Vector old, Box box)
        {
            Vector delta = box.pos - old;
            float x = Math.Abs(delta.x);
            float y = Math.Abs(delta.y);
            if (x >= player.dim.x / 2 + box.dim.x / 2 && x > 0)
            {
                float dx = -delta.x / x;
                player.pos.x = box.pos.x + dx * (box.dim.x / 2 + player.dim.x / 2);
                player.vel.x = 0;
            }
            if (y >= player.dim.y / 2 + box.dim.y / 2 && y > 0)
            {
                float dy = -delta.y / y;
                player.pos.y = box.pos.y + dy * (box.dim.y / 2 + player.dim.y / 2);
                if (delta.y > 0)
                {
                    player.isGrounded = true;
                }
                player.vel.y = 0;
            }
        }

        private Vector Offset(Vector vec)
        {
            return vec - cam.pos + view / 2;
        }

        private static bool IntersectingTopLeft(Vector p1, Vector d1, Vector p2, Vector d2)
        {
            return p1.x + d1.x > p2.x && p1.x < p2.x + d2.x && p1.y + d1.y > p2.y && p1.y < p2.y + d2.y;
        }

        private static void DrawBox(Graphics g, Vector pos, Vector dim)
        {
            g.FillRectangle(brush, pos.x, pos.y, dim.x, dim.y);
        }

        private static void DrawBoxOutline(Graphics g, Vector pos, Vector dim)
        {
            g.DrawRectangle(pen, pos.x, pos.y, dim.x, dim.y);
        }

        private void DrawImage(Graphics g, Vector pos, Vector dim)
        {
            Bitmap[] frames = player.stateFrames[player.state];
            player.frameIndex %= frames.Length;
            Bitmap frame = frames[player.frameIndex];
            Vector v = Offset(new(pos.x - frame.Width / 2, pos.y - frame.Height + dim.y / 2));
            Bitmap bitmap = (Bitmap)frame.Clone();
            if (player.isFacingLeft)
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            g.DrawImage(bitmap, v.x, v.y, bitmap.Width, bitmap.Height);
            bitmap.Dispose();
        }

        private void DrawEntity(Graphics g, Entity entity)
        {
            DrawImage(g, entity.pos, entity.dim);
        }

        private bool ActivateNode(Node node)
        {
            return node.children.Length switch
            {
                0 => node.isActivated,
                1 => ActivateNode(circuit.nodes[node.children[0]]),
                2 => Node.gates[node.gateName].eval(ActivateNode(circuit.nodes[node.children[0]]), ActivateNode(circuit.nodes[node.children[1]])),
                _ => throw new Exception("Node contraception failed."),
            };
        }

        private void DrawWorld(Graphics g)
        {
            if (cam.pos.y + view.y / 2 > 0)
            {
                float y = view.y / 2 - cam.pos.y;
                g.FillRectangle(brush, 0, y, view.x, view.y - y); // infinite floor
            }

            foreach (var block in world)
            {
                Vector pos = Offset(block.pos - block.dim / 2);
                if (IntersectingTopLeft(pos, block.dim, new(), view))
                {
                    DrawBox(g, pos, block.dim);
                    block.isClose = true;
                }
                else
                {
                    block.isClose = false;
                }
            }

            foreach (var node in circuit.outputs)
            {
                Vector dim = Node.dim;
                Vector pos = Offset(node.pos - dim / 2);
                Vector p = Offset(circuit.nodes[node.children[0]].pos);
                g.DrawLine(pen, pos.x + dim.x / 2, pos.y + dim.y / 2, p.x, p.y);
                node.isActivated = ActivateNode(node);
                if (node.isActivated)
                {
                    DrawBox(g, pos, dim);
                }
                else
                {
                    DrawBoxOutline(g, pos, dim);
                }
            }

            foreach (var node in circuit.nodes)
            {
                Vector dim = Node.dim;
                Vector pos = Offset(node.pos - dim / 2);
                if (node.gateName == null)
                {
                    brush.Color = Color.White;
                    pen.Color = Color.White;
                }
                else
                {
                    brush.Color = Node.gates[node.gateName].color;
                    pen.Color = Node.gates[node.gateName].color;
                }
                foreach (var id in node.children)
                {
                    Vector p = Offset(circuit.nodes[id].pos);
                    g.DrawLine(pen, pos.x + dim.x / 2, pos.y + dim.y / 2, p.x, p.y);
                }
                node.isActivated = ActivateNode(node);
                if (node.isActivated)
                {
                    DrawBox(g, pos, dim);
                }
                else
                {
                    DrawBoxOutline(g, pos, dim);
                }
            }
            brush.Color = Color.Black;
            pen.Color = Color.Black;
        }

        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Gray);
            DrawWorld(e.Graphics);
            brush.Color = Color.Blue;
            DrawEntity(e.Graphics, player);
            brush.Color = Color.Black;
        }

        private void AdjustView()
        {
            view.x = canvas.Width;
            view.y = canvas.Height;
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            AdjustView();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // game settings
        }
    }
}