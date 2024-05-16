using System.Windows.Forms;
using System.Xml.Linq;

namespace Game
{
    public partial class Game : Form
    {
        static Dictionary<Keys, bool> kb = new();
        static SolidBrush brush = new(Color.Black);

        const float friction = 0.25F;
        const float gravity = 1;

        static Entity player = new(new Vector(), new Vector(32, 64), 2, 16, "player");
        static Camera cam = new();
        static Vector view = new();
        static List<Block> world = new();

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
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = true;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = false;
        }

        private static bool Pressed(Keys key)
        {
            return kb.ContainsKey(key) && kb[key];
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

            player.pos += player.vel;

            player.vel.x *= 1 - friction;

            HandleCollisions(old);

            cam.pos += (player.pos - cam.pos) / 2;

            if (tick % 5 == 0)
            {
                player.frameIndex++;
            }

            tick++;

            canvas.Invalidate();
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
                    Vector delta = new();

                    if (old.y <= block.pos.y - block.dim.y / 2)
                    {
                        delta.y = -1;
                        player.isGrounded = true;
                    }

                    if (old.y >= block.pos.y + block.dim.y / 2)
                    {
                        delta.y = 1;
                    }

                    if (old.x <= block.pos.x - block.dim.x / 2)
                    {
                        delta.x = -1;
                    }

                    if (old.x >= block.pos.x + block.dim.x / 2)
                    {
                        delta.x = 1;
                    }

                    float x = Math.Abs(delta.x);
                    float y = Math.Abs(delta.y);
                    if (x > 0 && y == 0 || x >= y)
                    {
                        player.vel.x = 0;
                        player.pos.x = block.pos.x + delta.x * block.dim.x / 2 + delta.x * player.dim.x / 2;
                        player.isGrounded = false;
                    }
                    if (y > 0 && x == 0)
                    {
                        player.vel.y = 0;
                        player.pos.y = block.pos.y + delta.y * block.dim.y / 2 + delta.y * player.dim.y / 2;
                    }
                }
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