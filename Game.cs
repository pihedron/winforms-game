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
        const int size = 64;

        static Entity player = new(new Vector(), new Vector(size / 2, size), 2, 16, "player");
        static Camera cam = new();
        static Vector view = new();
        static List<Block> world = new();
        static Block?[,] grid;
        static readonly Circuit circuitTemplate = new();
        static Circuit circuit = new();
        static Vector posExt = new();
        static Vector dimExt = new(player.dim.x, player.dim.y);

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
            var rows = File.ReadLines("../../../world.txt").ToArray();
            grid = new Block[rows[0].Length, rows.Length];
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    grid[i, j] = null;
                }
            }
            for (int y = 0; y < rows.Length; y++)
            {
                char[] chars = rows[y].ToCharArray();
                for (int x = 0; x < chars.Length; x++)
                {
                    Vector dim = new(size, size);
                    switch (chars[x])
                    {
                        case '#':
                            grid[x, y] = new(GetPosition(x, y) + dim / 2, dim, "../../../img/spike/spike.png");
                            break;
                        case '^':
                            dim = new(size, size / 2);
                            grid[x, y] = new(GetPosition(x, y) + dim / 2 + new Vector(0, size / 2), dim, "../../../img/spike/spike.png", true);
                            break;
                    }
                }
            }
            world.Clear();
            string[] lines = File.ReadLines("../../../circuits.txt").ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(' ');
                switch (values[0])
                {
                    case "SRC":
                        circuitTemplate.nodes.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), int.Parse(values[3]) != 0));
                        break;
                    case "BOX":
                        circuitTemplate.outputs.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), values[3..].Select((string s) => int.Parse(s)).ToArray()));
                        break;
                    default:
                        circuitTemplate.nodes.Add(new Node(new(float.Parse(values[1]), float.Parse(values[2])), values[0], values[3..].Select((string s) => int.Parse(s)).ToArray()));
                        break;
                }
            }
            Reset();
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

        private void Reset()
        {
            player.pos = new();
            player.vel = new();

            circuit.outputs.Clear();
            circuit.nodes.Clear();

            circuitTemplate.outputs.ForEach((item) => circuit.outputs.Add((Node)item.Clone()));
            circuitTemplate.nodes.ForEach((item) => circuit.nodes.Add((Node)item.Clone()));
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
            // TESTING
            if (Pressed(Keys.Escape))
            {
                Reset();
            }

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

        private Tuple<int, int> GetIndex(Vector pos)
        {
            return new((int)pos.x / size, (int)pos.y / size + grid.GetLength(1));
        }

        private Vector GetPosition(int x, int y)
        {
            return new(x * size, (y - grid.GetLength(1)) * size);
        }

        private Block GetZone(int x, int y)
        {
            if (0 <= x && x < grid.GetLength(0) && 0 <= y && y < grid.GetLength(1))
            {
                return grid[x, y];
            }
            return null;
        }

        private Block[] GetAdjacent(int x, int y)
        {
            return new[] {
                GetZone(x, y),
                GetZone(x + 1, y),
                GetZone(x - 1, y),
                GetZone(x, y + 1),
                GetZone(x, y - 1),
                GetZone(x + 1, y + 1),
                GetZone(x + 1, y - 1),
                GetZone(x - 1, y + 1),
                GetZone(x - 1, y - 1),
            };
        }

        private void HandleCollisions(Vector old)
        {
            player.isGrounded = false;

            if (player.pos.y + player.dim.y / 2 > 0)
            {
                player.vel.y = 0;
                player.pos.y = -player.dim.y / 2;
                player.isGrounded = true;
            }

            int x;
            int y;
            (x, y) = GetIndex(player.pos);
            foreach (var block in GetAdjacent(x, y))
            {
                if (block != null && block.isClose && player.IsIntersecting(block))
                {
                    if (block.isDangerous)
                    {
                        Reset();
                    }
                    else
                    {
                        CollideWithBox(old, block);
                    }
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

            bool xc = x >= player.dim.x / 2 + box.dim.x / 2 && x > 0;
            bool yc = y >= player.dim.y / 2 + box.dim.y / 2 && y > 0;
            if (xc)
            {
                float dx = -delta.x / x;
                player.pos.x = box.pos.x + dx * (box.dim.x / 2 + player.dim.x / 2);
                player.vel.x = 0;
            }
            else if (yc)
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

        private void DrawAnimatedImage(Graphics g, Vector pos, Vector dim)
        {
            Bitmap[] frames = player.stateFrames[player.state];
            player.frameIndex %= frames.Length;
            Bitmap frame = frames[player.frameIndex];
            Vector vec = Offset(new(pos.x - frame.Width / 2, pos.y - frame.Height + dim.y / 2));
            Bitmap bitmap = (Bitmap)frame.Clone();
            if (player.isFacingLeft)
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            g.DrawImage(bitmap, vec.x, vec.y, bitmap.Width, bitmap.Height);
            bitmap.Dispose();
        }

        private void DrawBlock(Graphics g, Block block)
        {
            Vector pos = Offset(block.pos - block.dim / 2);
            g.DrawImage(block.image, pos.x, pos.y, block.image.Width, block.image.Height);
        }

        private void DrawEntity(Graphics g, Entity entity)
        {
            DrawAnimatedImage(g, entity.pos, entity.dim);
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

            foreach (var block in grid)
            {
                if (block == null) continue;
                Vector pos = Offset(block.pos - block.dim / 2);
                if (IntersectingTopLeft(pos, block.dim, new(), view))
                {
                    if (block.isDangerous)
                    {
                        DrawBlock(g, block);
                    }
                    else
                    {
                        DrawBox(g, pos, block.dim);
                    }
                    block.isClose = true;
                }
                else
                {
                    block.isClose = false;
                }
            }

            brush.Color = Color.Black;
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