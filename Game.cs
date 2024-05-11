using System.Windows.Forms;

namespace Game
{
    public partial class Game : Form
    {
        static Dictionary<Keys, bool> kb = new();
        static SolidBrush brush = new(Color.Black);

        const float friction = 0.25F;
        const float gravity = 1;

        Entity player = new(new Vector(), new Vector(32, 32), 2, 16);
        Camera cam = new();
        Vector view = new();
        List<Block> world = new()
        {
            new Block(new Vector(0, 32), new Vector(256, 64)),
            new Block(new Vector(0, -64), new Vector(128, 32)),
            new Block(new Vector(112, -128), new Vector(32, 256)),
        };

        public Game()
        {
            InitializeComponent();

            // fullscreen settings
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            canvas.Dock = DockStyle.Fill;
            AdjustView();

            // event listeners
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = true;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            kb[e.KeyCode] = false;
        }

        private bool Down(Keys key)
        {
            return kb.ContainsKey(key) && kb[key];
        }

        private void MovePlayer()
        {
            if (Down(Keys.D))
            {
                player.vel.x += player.movePower;
            }

            if (Down(Keys.A))
            {
                player.vel.x -= player.movePower;
            }

            if (Down(Keys.W) && player.grounded)
            {
                player.vel.y = -player.jumpHeight;
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            Vector old = player.pos;
            player.vel.y += gravity;

            MovePlayer();

            player.pos += player.vel;

            player.vel.x *= 1 - friction;

            player.grounded = false;

            foreach (var block in world)
            {
                if (block.isClose && Intersecting(player.pos, player.dim, block.pos, block.dim))
                {
                    if (old.y <= block.pos.y - block.dim.y / 2)
                    {
                        player.vel.y = 0;
                        player.pos.y = block.pos.y - block.dim.y / 2 - player.dim.y / 2;
                        player.grounded = true;
                    }

                    if (old.y >= block.pos.y + block.dim.y / 2)
                    {
                        player.vel.y = 0;
                        player.pos.y = block.pos.y + block.dim.y / 2 + player.dim.y / 2;
                    }

                    if (old.x <= block.pos.x - block.dim.x / 2)
                    {
                        player.vel.x = 0;
                        player.pos.x = block.pos.x - block.dim.x / 2 - player.dim.x / 2;
                    }

                    if (old.x >= block.pos.x + block.dim.x / 2)
                    {
                        player.vel.x = 0;
                        player.pos.x = block.pos.x + block.dim.x / 2 + player.dim.x / 2;
                    }
                }
            }

            cam.pos += (player.pos - cam.pos) / 2;

            canvas.Invalidate();
        }

        private Vector Offset(Vector v)
        {
            return v - cam.pos + view / 2;
        }

        private bool Intersecting(Vector p1, Vector d1, Vector p2, Vector d2)
        {
            return p1.x + d1.x / 2 > p2.x - d2.x / 2 && p1.x - d1.x / 2 < p2.x + d2.x / 2 && p1.y + d1.y / 2 > p2.y - d2.y / 2 && p1.y - d1.y / 2 < p2.y + d2.y / 2;
        }

        private bool IntersectingTopLeft(Vector p1, Vector d1, Vector p2, Vector d2)
        {
            return p1.x + d1.x > p2.x && p1.x < p2.x + d2.x && p1.y + d1.y > p2.y && p1.y < p2.y + d2.y;
        }

        private void DrawBox(Graphics g, Vector pos, Vector dim)
        {
            g.FillRectangle(brush, pos.x, pos.y, dim.x, dim.y);
        }

        private void DrawEntity(Graphics g, Entity entity)
        {
            DrawBox(g, Offset(entity.pos - entity.dim / 2), entity.dim);
        }

        private void DrawWorld(Graphics g)
        {
            foreach (var block in world)
            {
                Vector pos = Offset(block.pos - block.dim / 2);
                if (IntersectingTopLeft(pos, block.dim, new Vector(), view))
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
            e.Graphics.Clear(Color.White);
            DrawWorld(e.Graphics);
            DrawEntity(e.Graphics, player);
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
    }
}