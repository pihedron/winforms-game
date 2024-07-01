using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using System.Linq;

namespace Game
{
    public partial class Game : Form
    {
        static Dictionary<Keys, bool> kb = new()
        {
            { Keys.W, false },
            { Keys.A, false },
            { Keys.D, false },
            { Keys.E, false },
            { Keys.G, false },
            { Keys.M, false },
            { Keys.Escape, false },
            { Keys.Space, false },
            { Keys.Up, false },
            { Keys.Down, false },
            { Keys.Left, false },
            { Keys.Right, false },
        };
        static Dictionary<Keys, bool> shadow = new()
        {
            { Keys.G, false },
            { Keys.M, false },
            { Keys.Escape, false },
            { Keys.Space, false },
        };
        static bool mouseDown = false;
        static SolidBrush brush = new(Color.Black);
        static Pen pen = new(Color.Black, 2);

        const float friction = 0.25F;
        const float gravity = 1;
        const float shake = size / 2;
        const int tpf = 5;
        const int tpw = 4;

        public const int size = 64;
        public const string prefix = "../../../";

        public static Vector view = new();
        public static Stopwatch stopwatch = new();

        static int level = 14;
        static Entity player = new(new Vector(), new Vector(size / 2, size), 2, 16, "player");
        static Camera cam = new();
        static Block?[,] grid;
        static readonly Circuit circuitTemplate = new();
        static Circuit circuit = new();
        static Prompt prompt = new("");
        static Vector spawn;
        static bool paused = false;
        static bool interferenceExists = false;
        static int lastFlipped = -1;
        static PauseMenu pm = new();
        static Vector mouse = new();
        static bool controlsLocked = false;
        static bool graphicsOn = true;
        static bool peekOn = true;
        static Vector artifact;
        static bool playerCollectedArtifact = false;
        static int artifactsCollected = 0;
        static int tutorialIndex = 0;
        static List<List<(string hint, Func<bool> f)>> tutorial = new();
        static bool moved = false;
        static bool end = false;

        static int tick = 0;

        // recording tutorials
        static StreamWriter writer;
        static bool recording = false;

        // loading tutorials
        static List<Vector> ghost = new();

        // game hints
        static List<Hint> hints = new();

        public Game()
        {
            InitializeComponent();

            MouseWheel += OnMouseWheel;

            // draw settings
            pen.Alignment = PenAlignment.Inset;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            // fullscreen settings
            canvas.Dock = DockStyle.Fill;
            AdjustView();

            // load assets
            player.GetFrames();

            // create world
            LoadLevel();
            Reset();

            // ghost tutorial
            string ghostPath = $"{prefix}/lvl/{level}/ghost.txt";

            // reset ghost recording
            if (recording)
            {
                File.WriteAllText(ghostPath, string.Empty);
                writer = new(ghostPath);
            }

            // load ghost
            if (!recording && level == 0)
            {
                string[] lines = File.ReadAllLines(ghostPath);
                foreach (var line in lines)
                {
                    string[] positions = line.Split(' ');
                    ghost.Add(new(int.Parse(positions[0]), int.Parse(positions[1])));
                }
            }
        }

        private void LoadLevel()
        {
            try
            {
                var rows = File.ReadLines($"{prefix}/lvl/{level}/world.txt").ToArray();
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
                                string tileName = $"{new string(GetTileVariant(rows, x, y))}.png";
                                grid[x, y] = new(GetPosition(x, y) + dim / 2, dim, $"{prefix}img/block/{tileName}");
                                break;
                            case '^':
                                dim = new(size, size / 2);
                                grid[x, y] = new(GetPosition(x, y) + dim / 2 + new Vector(0, size / 2), dim, $"{prefix}img/block/1111.png", true);
                                break;
                            case '[':
                                spawn = GetPosition(x, y) + dim / 2;
                                break;
                            case ']':
                                grid[x, y] = new(GetPosition(x, y) + dim / 2, dim, $"{prefix}img/block/1111.png")
                                {
                                    isEnd = true
                                };
                                break;
                            case '*':
                                artifact = GetPosition(x, y) + dim / 2;
                                break;
                            default:
                                grid[x, y] = new(GetPosition(x, y) + dim / 2, dim, $"{prefix}img/block/1111.png")
                                {
                                    isSolid = false
                                };
                                break;
                        }
                    }
                }
                hints.Clear();
                circuitTemplate.nodes.Clear();
                circuitTemplate.outputs.Clear();
                string[] lines = File.ReadLines($"{prefix}/lvl/{level}/circuits.txt").ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split(' ');
                    Vector pos = new Vector(float.Parse(values[1]), float.Parse(values[2])) * size + new Vector(size, size) / 2;
                    switch (values[0])
                    {
                        case "SRC":
                            circuitTemplate.nodes.Add(new Node(pos, int.Parse(values[3]) != 0));
                            break;
                        case "BOX":
                            Node node = new(pos, values[3..].Select((string s) => int.Parse(s)).ToArray())
                            {
                                dim = new(size, size)
                            };
                            circuitTemplate.outputs.Add(node);
                            break;
                        case "HINT":
                            Vector target = new Vector(float.Parse(values[3]), float.Parse(values[4])) * size + new Vector(size, size) / 2;
                            Hint hint = new(pos, target, string.Join(' ', values[5..]).Replace('_', '\n'));
                            hints.Add(hint);
                            break;
                        default:
                            circuitTemplate.nodes.Add(new Node(pos, values[0], values[3..].Select((string s) => int.Parse(s)).ToArray()));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                grid = new Block?[0, 0];
                circuitTemplate.nodes.Clear();
                circuitTemplate.outputs.Clear();
                end = true;
                spawn = new(0, 0);
            }

            // text tutorial
            for (int i = tutorial.Count; i <= level; i++)
            {
                tutorial.Add(new());
            }
            string tutorialPath = $"{prefix}/lvl/{level}/tutorial.txt";
            try
            {
                string[] lines = File.ReadAllLines(tutorialPath);
                foreach (var line in lines)
                {
                    string[] cells = line.Split(':');
                    int keyCode = int.Parse(cells[0]);
                    string text = cells[1];
                    tutorial[level].Add((text, () => Pressed((Keys)keyCode)));
                }
            }
            catch (Exception e)
            {
                // ignore
            }

            if (recording && level != 0)
            {
                writer.Close();
            }
        }

        private char GetChar(string[] rows, int x, int y)
        {
            if (0 <= x && x < rows[0].Length && 0 <= y && y < rows.Length)
            {
                return rows[y][x];
            }
            if (y >= rows.Length) return '#';
            return ' ';
        }

        private char[] GetTileVariant(string[] rows, int x, int y)
        {
            char[] adj = new char[4]
            {
                GetChar(rows, x - 1, y),
                GetChar(rows, x + 1, y),
                GetChar(rows, x, y - 1),
                GetChar(rows, x, y + 1),
            };

            return adj.Select((char c) => c == '#' ? '1' : '0').ToArray();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = true;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = false;
            shadow[e.KeyCode] = false;
        }

        private static bool Pressed(Keys key)
        {
            return kb[key];
        }

        private static bool Down(Keys key)
        {
            return shadow[key];
        }

        private void Reset()
        {
            moved = false;

            player.isDying = false;
            player.state = EntityState.Idle;
            player.frameIndex = 0;
            player.pos = spawn;
            player.vel = new();
            player.isGrounded = false;

            circuit.outputs.Clear();
            circuit.nodes.Clear();

            circuitTemplate.outputs.ForEach((item) => circuit.outputs.Add((Node)item.Clone()));
            circuitTemplate.nodes.ForEach((item) => circuit.nodes.Add((Node)item.Clone()));

            lastFlipped = 0;

            if (playerCollectedArtifact)
            {
                playerCollectedArtifact = false;
                artifactsCollected--;
            }
        }

        private void MovePlayer()
        {
            if (controlsLocked) return;

            if (Pressed(Keys.D))
            {
                moved = true;
                player.vel.x += player.movePower;
            }

            if (Pressed(Keys.A))
            {
                moved = true;
                player.vel.x -= player.movePower;
            }

            if (Pressed(Keys.W) && player.isGrounded)
            {
                moved = true;
                player.vel.y = -player.jumpHeight;
            }

            if (!end && !paused && !stopwatch.IsRunning && moved)
            {
                stopwatch.Start();
            }
        }

        private void ShowPauseMenu(Graphics g)
        {
            brush.Color = Color.FromArgb(200, 0, 0, 0);
            DrawBox(g, new(), view);
            pm.Show(g);
        }

        private void Toggle(ref bool b, Keys key)
        {
            if (Pressed(key) && !Down(key))
            {
                b ^= true;
                shadow[key] = true;
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            Vector old = player.pos;

            if (player.isDying)
            {
                if (player.frameIndex == player.stateFrames[player.state].Length / 2 - 1)
                {
                    tick = 0;
                    Reset();
                    return;
                }

                if (tick % tpf == 0)
                {
                    player.frameIndex++;
                }

                player.vel.y += gravity;
                player.pos += player.vel;

                HandleCollisions(old);

                cam.pos.x += (float)(Math.Sin(tick / 2) * shake) / (tick + 1);

                tick++;
                canvas.Invalidate();
                return;
            }

            if (Pressed(Keys.Escape) && !Down(Keys.Escape))
            {
                pm.scroll = new();
                paused ^= true;
                if (paused)
                {
                    stopwatch.Stop();
                }
                else if (moved && !end)
                {
                    stopwatch.Start();
                }
                shadow[Keys.Escape] = true;
            }

            Toggle(ref graphicsOn, Keys.G);
            Toggle(ref peekOn, Keys.M);

            if (paused)
            {
                if (Pressed(Keys.Down)) pm.vel.y++;
                if (Pressed(Keys.Up)) pm.vel.y--;
                pm.scroll += pm.vel;
                pm.vel *= 1 - friction;
                canvas.Invalidate();
                return;
            }

            MovePlayer();

            player.vel.y += gravity;

            if (mouseDown && player.isGrounded && player.state != EntityState.Attack)
            {
                player.state = EntityState.Attack;
                player.frameIndex = 0;
            }

            if (player.state == EntityState.Attack && player.frameIndex < player.stateFrames[player.state].Length / 2 - 1)
            {
                controlsLocked = true;
            }
            else
            {
                controlsLocked = false;
                AnimatePlayer();
            }

            player.pos += player.vel;

            player.vel.x *= 1 - friction;

            HandleCollisions(old);

            if (player.isDying) return;

            cam.pos += (player.pos + (peekOn ? 1 : 0) * ((mouse - view / 2) / 2) - cam.pos) / 4;

            if (tick % tpf == 0)
            {
                player.frameIndex++;
            }

            if (tick % tpw == 0 && level == 0 && recording)
            {
                writer.WriteLine($"{Math.Round(player.pos.x)} {Math.Round(player.pos.y)}");
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
            return new((int)(pos.x / size), (int)(pos.y / size + grid.GetLength(1)));
        }

        private Vector GetPosition(int x, int y)
        {
            return new(x * size, (y - grid.GetLength(1)) * size);
        }

        private Block? GetZone(int x, int y)
        {
            if (0 <= x && x < grid.GetLength(0) && 0 <= y && y < grid.GetLength(1))
            {
                return grid[x, y];
            }
            return null;
        }

        private Block?[] GetAdjacent(int x, int y)
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

            if (!end && !playerCollectedArtifact && player.IsIntersecting(artifact, new(size / 2, size / 2)))
            {
                playerCollectedArtifact = true;
                artifactsCollected++;
            }

            (int x, int y) = GetIndex(player.pos);
            foreach (var block in GetAdjacent(x, y))
            {
                if (block != null && block.isSolid && player.IsIntersecting(block))
                {
                    if (block.isDangerous && !player.isDying)
                    {
                        stopwatch.Stop();

                        player.vel.y = 0;
                        player.vel.x = 0;
                        player.isDying = true;
                        player.state = EntityState.Die;
                        tick = 0;
                        player.frameIndex = 0;
                        break;
                    }
                    else if (block.isEnd && !player.isDying)
                    {
                        prompt.text = "[SPACE] next level";
                        if (Pressed(Keys.Space))
                        {
                            stopwatch.Stop();
                            level++;
                            playerCollectedArtifact = false;
                            artifact = null;
                            LoadLevel();
                            Reset();
                            break;
                        }
                    }
                    else
                    {
                        CollideWithBox(old, block);
                    }
                }
            }

            foreach (var node in circuit.outputs)
            {
                Box box = new(node.pos, node.dim);
                if (player.IsIntersecting(box))
                {
                    if (node.isActivated)
                    {
                        CollideWithBox(old, box);
                    }
                    else
                    {
                        interferenceExists = true;
                    }
                }
            }

            for (int i = 0; i < circuit.nodes.Count; i++)
            {
                Node node = circuit.nodes[i];
                if (node.children.Length != 0) continue;
                if (player.IsIntersecting(node.pos, node.dim) && Pressed(Keys.Space) && !Down(Keys.Space) && !interferenceExists)
                {
                    node.isActivated = !node.isActivated;
                    shadow[Keys.Space] = true;
                    lastFlipped = i;
                }
            }

            interferenceExists = false;
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

        public Vector Offset(Vector vec)
        {
            return vec - cam.pos + view / 2;
        }

        public Vector Offset(Vector vec, float z)
        {
            return (vec - cam.pos) / z + view / 2;
        }

        private static bool IntersectingTopLeft(Vector p1, Vector d1, Vector p2, Vector d2)
        {
            return p1.x + d1.x > p2.x && p1.x < p2.x + d2.x && p1.y + d1.y > p2.y && p1.y < p2.y + d2.y;
        }

        private static void DrawBox(Graphics g, Vector pos, Vector dim)
        {
            g.FillRectangle(brush, pos.x, pos.y, dim.x, dim.y);
        }

        private void DrawFinish(Graphics g, Block block)
        {
            Vector pos = Offset(block.pos - block.dim / 2);
            const int div = 16;
            const int step = size / div;
            for (int x = 0; x < div; x++)
            {
                for (int y = 0; y < div; y++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        brush.Color = Color.Black;
                    }
                    else
                    {
                        brush.Color = Color.White;
                    }
                    g.FillRectangle(brush, pos.x + x * step, pos.y + y * step, step, step);
                }
            }
        }

        private static void DrawBoxOutline(Graphics g, Vector pos, Vector dim)
        {
            g.DrawRectangle(pen, pos.x, pos.y, dim.x, dim.y);
        }

        private void DrawAnimatedImage(Graphics g, Vector pos, Vector dim)
        {
            Bitmap[] frames = player.stateFrames[player.state];
            player.frameIndex %= frames.Length / 2;
            Bitmap frame = frames[player.frameIndex * 2 + (player.isFacingLeft ? 1 : 0)];
            Vector vec = Offset(new(pos.x - frame.Width / 2, pos.y - frame.Height + dim.y / 2));
            g.DrawImage(frame, vec.x, vec.y, frame.Width, frame.Height);
        }

        private void DrawBlock(Graphics g, Block block)
        {
            Vector pos = Offset(block.pos - block.dim / 2);
            g.DrawImage(block.image, new RectangleF(pos.x, pos.y, block.dim.x, block.dim.y), new RectangleF(0, 0, size / 4, size / 4), GraphicsUnit.Pixel);
        }

        private void DrawSpike(Graphics g, Block block)
        {
            Vector pos = Offset(block.pos - new Vector(0, block.dim.y / 2));
            g.FillPolygon(brush, new PointF[3] { new(pos.x - block.dim.x / 2, pos.y + block.dim.y), new(pos.x - block.dim.x / 4, pos.y), new(pos.x, pos.y + block.dim.y) });
            g.FillPolygon(brush, new PointF[3] { new(pos.x + block.dim.x / 2, pos.y + block.dim.y), new(pos.x + block.dim.x / 4, pos.y), new(pos.x, pos.y + block.dim.y) });
        }

        private void DrawEntity(Graphics g, Entity entity)
        {
            DrawAnimatedImage(g, entity.pos, entity.dim);
        }

        private void DrawArtifact(Graphics g)
        {
            if (playerCollectedArtifact) return;
            const int s = size / 2;
            Vector dim = new(s, s);
            Vector pos = Offset(artifact - dim / 2 + new Vector(0, (float)Math.Sin(tick * Math.PI / size) * dim.y / 8));
            g.FillEllipse(brush, pos.x, pos.y, dim.x, dim.y);
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

        private void DrawGhost(Graphics g)
        {
            int index = tick / tpw;
            index %= ghost.Count - 1;
            int trail = index - tpw * 2 + ghost.Count;
            trail %= ghost.Count - 1;
            for (int i = trail; i < index; i++)
            {
                Vector current = Offset(ghost[i]);
                Vector next = Offset(ghost[i + 1]);
                pen.Color = Color.Orange;
                pen.Width = 4;
                g.DrawLine(pen, current.x, current.y, next.x, next.y);
            }
        }

        private void DrawWorld(Graphics g)
        {
            if (cam.pos.y + view.y / 2 > 0)
            {
                float y = view.y / 2 - cam.pos.y;
                g.FillRectangle(brush, 0, y, view.x, view.y - y); // infinite floor
            }

            if (!recording && level == 0 && !player.isDying) DrawGhost(g);

            brush.Color = Color.Black;
            pen.Color = Color.Black;
            pen.Width = 2;
            foreach (var node in circuit.outputs)
            {
                Vector dim = node.dim;
                Vector pos = Offset(node.pos - dim / 2);
                Vector p = Offset(circuit.nodes[node.children[0]].pos);
                g.DrawLine(pen, pos.x + dim.x / 2, pos.y + dim.y / 2, p.x, p.y);
                if (lastFlipped >= 0)
                {
                    node.isActivated = ActivateNode(node);
                }
                if (node.isActivated)
                {
                    DrawBox(g, pos, dim);
                }
                else
                {
                    DrawBoxOutline(g, pos, dim);
                }
            }
            lastFlipped = -1;

            foreach (var node in circuit.nodes)
            {
                Vector dim = node.dim;
                Vector pos = Offset(node.pos - dim / 2);
                if (node.gateName == null)
                {
                    brush.Color = Color.White;
                    pen.Color = Color.White;
                    if (player.IsIntersecting(node.pos, node.dim))
                    {
                        prompt.text = "[SPACE] flip switch";
                    }
                }
                else
                {
                    brush.Color = Node.gates[node.gateName].color;
                    pen.Color = Node.gates[node.gateName].color;
                    if (player.IsIntersecting(node.pos, node.dim))
                    {
                        prompt.text = node.gateName;
                        prompt.brush.Color = brush.Color;
                    }
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

            Vector position = cam.pos - view / 2;
            Vector n = view / size;
            (int sx, int sy) = GetIndex(position);
            for (int x = 0; x < n.x + 1; x++)
            {
                for (int y = 0; y < n.y + 1; y++)
                {
                    Block? block = GetZone(sx + x, sy + y);
                    if (block == null) continue;
                    if (block.isDangerous)
                    {
                        DrawSpike(g, block);
                    }
                    else if (block.isEnd)
                    {
                        DrawFinish(g, block);
                    }
                    else if (block.isSolid)
                    {
                        if (graphicsOn) DrawBlock(g, block);
                        else DrawBox(g, Offset(block.pos - block.dim / 2), block.dim);
                    }
                    else
                    {
                        // dynamic tutorial blocks
                    }
                }
            }

            foreach (var hint in hints)
            {
                float squaredDistance = (hint.pos - player.pos).SquaredDistance();
                int squaredRadius = Hint.radius * Hint.radius;
                if (squaredDistance > squaredRadius) continue;
                float fract = Math.Min(1, 2 - 2 * squaredDistance / squaredRadius);
                Color c = Color.FromArgb((byte)(fract * byte.MaxValue), Color.Black);
                brush.Color = c;
                pen.Color = c;
                StringFormat stringFormat = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                Vector pos = Offset(hint.pos);
                Vector target = Offset(hint.target);
                g.DrawString(hint.message, pm.font, brush, pos.x, pos.y, stringFormat);

                if (target == pos) continue;

                Vector delta = target - pos;

                if (delta.x != 0)
                {
                    delta.x /= Math.Abs(delta.x);
                }

                if (delta.y != 0)
                {
                    delta.y /= Math.Abs(delta.y);
                }

                g.DrawLine(pen, pos.x + delta.x * size / 2, pos.y + delta.y * size / 2, target.x, target.y);
            }

            brush.Color = Color.Black;
            pen.Color = Color.Black;

            if (!end) DrawArtifact(g);
        }

        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            // make Bitmap drawing crisp
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            e.Graphics.Clear(Color.Gray);
            brush.Color = Color.Black;

            if (end)
            {
                int z = 4;
                Font font = new(pm.ff, pm.font.Size * 4 / z);
                Vector pos = Offset(new(size * 16, -size / 2), z);
                e.Graphics.DrawString($"{artifactsCollected} / {level}\nartifacts", font, brush, pos.x, pos.y, new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                pos = Offset(new(size * -16, -size / 2), z);
                e.Graphics.DrawString($"{stopwatch.ElapsedMilliseconds}\nmilliseconds", font, brush, pos.x, pos.y, new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            DrawWorld(e.Graphics);
            DrawEntity(e.Graphics, player);

            prompt.Show(e.Graphics, Offset(player.pos - new Vector(0, player.dim.y / 2)));

            if (tutorialIndex < tutorial[level].Count)
            {
                prompt.text = tutorial[level][tutorialIndex].hint;
                if (tutorial[level][tutorialIndex].f())
                {
                    tutorialIndex++;
                }
            }
            else
            {
                prompt.text = Pressed(Keys.E) ? $"artifacts: {artifactsCollected}" : "";
                prompt.brush.Color = Prompt.defaultColor;
            }

            pm.DrawTimer(e.Graphics, stopwatch);

            if (paused) ShowPauseMenu(e.Graphics);
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

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            pm.vel.y -= e.Delta / Math.Abs(e.Delta) * size / 2;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            mouse.x = e.X;
            mouse.y = e.Y;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }
    }
}