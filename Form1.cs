using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;


namespace BreakoutGame
{
    public class Form1 : Form
    {
        // Game area boundaries
        private Rectangle gameArea;
        private const int gameAreaX = 50;
        private const int gameAreaY = 50;
        private const int gameAreaWidth = 700;
        private const int gameAreaHeight = 550;

        // Game objects
        private int ballX = 400;
        private int ballY = 450;
        private float ballSpeedX = 4;
        private float ballSpeedY = -4;
        private const int ballSize = 12;

        private int paddleX = 350;
        private const int paddleWidth = 100;
        private const int paddleHeight = 15;
        private int paddleY;
        private const int paddleSpeed = 20;

        private int score = 0;
        private int lives = 3;
        private bool gameRunning = false;
        private bool gameOver = false;

        private System.Windows.Forms.Timer gameTimer;
        private System.Windows.Forms.Timer neonTimer;

        private bool[,] bricks;
        private const int brickRows = 5;
        private const int brickCols = 11;
        private const int brickWidth = 60;
        private const int brickHeight = 20;
        private const int pointsPerBrick = 36;
        private int brickOffsetX;
        private int brickOffsetY;

        private Label lblScore;
        private Label lblLives;
        private Label lblMessage;
        private Button btnStart;
        private Button btnRestart;

        private float neonIntensity = 1.0f;
        private bool neonDirection = true;
        private Random random = new Random();

        public Form1()
        {
            InitializeForm();
            CalculateGameArea();
            CreateBricks();
            UpdateUI();

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            neonTimer = new System.Windows.Forms.Timer();
            neonTimer.Interval = 50;
            neonTimer.Tick += (s, e) =>
            {
                if (neonDirection)
                {
                    neonIntensity += 0.03f;
                    if (neonIntensity >= 1.3f) neonDirection = false;
                }
                else
                {
                    neonIntensity -= 0.03f;
                    if (neonIntensity <= 0.7f) neonDirection = true;
                }
                this.Refresh();
            };
            neonTimer.Start();

            this.KeyPreview = true;

            // CRITICAL FIX: Force form to capture all keys
            this.Select();
            this.Focus();

            // SAFETY NET - guarantee that objects exist
            lblScore = lblScore ?? new Label();
            lblLives = lblLives ?? new Label();
            lblMessage = lblMessage ?? new Label();
            btnStart = btnStart ?? new Button();
            btnRestart = btnRestart ?? new Button();
            bricks = bricks ?? new bool[0, 0];

            // If bricks not created (empty array), recreate them
            if (bricks.GetLength(0) == 0 || bricks.GetLength(1) == 0)
            {
                CreateBricks();
            }
        }

        private void CalculateGameArea()
        {
            gameArea = new Rectangle(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
            paddleY = gameAreaY + gameAreaHeight - 50;
            brickOffsetX = gameAreaX + 20;
            brickOffsetY = gameAreaY + 30;

            // Initial positions
            ballX = gameAreaX + gameAreaWidth / 2 - ballSize / 2;
            ballY = paddleY - 30;
            paddleX = gameAreaX + gameAreaWidth / 2 - paddleWidth / 2;
        }

        private void InitializeForm()
        {
            this.Text = "✦ ARKANOID 1980 ✦";
            this.Size = new Size(820, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(8, 4, 20);
            this.Paint += Form1_Paint;

            // Score label
            lblScore = new Label()
            {
                Text = "⚡ SCORE: 000",
                Location = new Point(15, 15),
                ForeColor = Color.FromArgb(0, 255, 255),
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 14, FontStyle.Bold),
                AutoSize = true
            };

            // Lives label
            lblLives = new Label()
            {
                Text = "❤️ x 3",
                Location = new Point(720, 15),
                ForeColor = Color.FromArgb(255, 80, 120),
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 14, FontStyle.Bold),
                AutoSize = true
            };

            // Message label
            lblMessage = new Label()
            {
                Text = "PRESS [START] TO BEGIN",
                Location = new Point(260, 320),
                Size = new Size(300, 50),
                ForeColor = Color.FromArgb(255, 50, 200),
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Start button
            btnStart = new Button()
            {
                Text = "▶ START",
                Location = new Point(280, 620),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(20, 10, 40),
                ForeColor = Color.FromArgb(0, 255, 100),
                Font = new Font("Consolas", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnStart.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 100);
            btnStart.FlatAppearance.BorderSize = 2;
            btnStart.Click += BtnStart_Click;

            // Restart button
            btnRestart = new Button()
            {
                Text = "⟳ RESTART",
                Location = new Point(410, 620),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(20, 10, 40),
                ForeColor = Color.FromArgb(255, 100, 0),
                Font = new Font("Consolas", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.FlatAppearance.BorderColor = Color.FromArgb(255, 100, 0);
            btnRestart.FlatAppearance.BorderSize = 2;
            btnRestart.Click += BtnRestart_Click;
            this.Controls.Add(lblScore);
            this.Controls.Add(lblLives);
            this.Controls.Add(lblMessage);
            this.Controls.Add(btnStart);
            this.Controls.Add(btnRestart);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle movement keys FIRST (before buttons can steal them)
            if (gameRunning && !gameOver)
            {
                if (keyData == Keys.Left || keyData == Keys.A)
                {
                    paddleX -= paddleSpeed;
                    if (paddleX < gameAreaX) paddleX = gameAreaX;
                    this.Invalidate();
                    return true;
                }
                else if (keyData == Keys.Right || keyData == Keys.D)
                {
                    paddleX += paddleSpeed;
                    if (paddleX > gameAreaX + gameAreaWidth - paddleWidth)
                        paddleX = gameAreaX + gameAreaWidth - paddleWidth;
                    this.Invalidate();
                    return true;
                }
            }

            // Handle button navigation when game is not active
            if (!gameRunning || gameOver)
            {
                // LEFT or A - always select START button
                if (keyData == Keys.Left || keyData == Keys.A)
                {
                    btnStart.Focus();
                    return true;
                }

                // RIGHT or D - always select RESTART button
                if (keyData == Keys.Right || keyData == Keys.D)
                {
                    btnRestart.Focus();
                    return true;
                }

                // ENTER or SPACE - click the selected button
                if (keyData == Keys.Enter || keyData == Keys.Space)
                {
                    if (btnStart.Focused)
                        btnStart.PerformClick();
                    else if (btnRestart.Focused)
                        btnRestart.PerformClick();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SetRandomBallDirection()
        {
            int direction = random.Next(0, 2);  // 0 = left, 1 = right
            int speed = random.Next(3, 6);      // Random speed between 3 and 5
            ballSpeedX = direction == 0 ? -speed : speed;
            ballSpeedY = -4;                    // Always up
        }
        private void CreateBricks()
        {
            bricks = new bool[brickRows, brickCols];
            for (int i = 0; i < brickRows; i++)
                for (int j = 0; j < brickCols; j++)
                    bricks[i, j] = true;
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            // Don't allow start if game is over
            if (gameOver) return;

            if (!gameRunning)
            {
                gameRunning = true;
                lblMessage.Visible = false;

                // Random starting angle for the ball
                SetRandomBallDirection();

                this.Refresh();
            }
            // Return focus to form so arrow keys work immediately
            this.Select();
            this.Focus();
        }

        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            // Full reset
            gameRunning = false;
            gameOver = false;
            score = 0;
            lives = 3;

            // Random starting angle for restart
            SetRandomBallDirection();

            // Reset positions
            ballX = gameAreaX + gameAreaWidth / 2 - ballSize / 2;
            ballY = paddleY - 30;
            paddleX = gameAreaX + gameAreaWidth / 2 - paddleWidth / 2;

            CreateBricks();
            UpdateUI();

            lblMessage.Visible = true;
            lblMessage.Text = "PRESS [START] TO BEGIN";
            lblMessage.ForeColor = Color.FromArgb(255, 50, 200);
            this.Refresh();

            // Return focus to form
            this.Select();
            this.Focus();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (!gameRunning) return;

            ballX += (int)ballSpeedX;
            ballY += (int)ballSpeedY;

            // Collision with game area borders
            if (ballX <= gameAreaX)
            {
                ballX = gameAreaX;
                ballSpeedX = -ballSpeedX;
            }
            if (ballX >= gameAreaX + gameAreaWidth - ballSize)
            {
                ballX = gameAreaX + gameAreaWidth - ballSize;
                ballSpeedX = -ballSpeedX;
            }
            if (ballY <= gameAreaY)
            {
                ballY = gameAreaY;
                ballSpeedY = -ballSpeedY;
            }

            // Lose life
            if (ballY >= gameAreaY + gameAreaHeight)
            {
                lives--;
                UpdateUI();

                // Lose live sound
                _ = PlayLoseLifeMelody();

                if (lives <= 0)
                {
                    GameOver();
                    return;
                }

                gameRunning = false;
                ballX = paddleX + paddleWidth / 2 - ballSize / 2;
                ballY = paddleY - ballSize;

                // Random starting angle for next life
                SetRandomBallDirection();

                lblMessage.Visible = true;
                lblMessage.Text = $"LOST! {lives} LIVES LEFT\nPRESS [START]";
                lblMessage.ForeColor = Color.FromArgb(255, 100, 100);
                this.Refresh();
                return;
            }

            // Collision with paddle
            if (ballY + ballSize >= paddleY &&
                ballY <= paddleY + paddleHeight &&
                ballX + ballSize >= paddleX &&
                ballX <= paddleX + paddleWidth)
            {
                ballY = paddleY - ballSize;
                ballSpeedY = -Math.Abs(ballSpeedY);

                int hitPos = (ballX + ballSize / 2) - (paddleX + paddleWidth / 2);
                ballSpeedX = hitPos / 8;
                Console.Beep(500, 20);

                if (Math.Abs(ballSpeedX) < 3) ballSpeedX = ballSpeedX > 0 ? 3 : -3;
                if (Math.Abs(ballSpeedX) > 7) ballSpeedX = ballSpeedX > 0 ? 7 : -7;
            }

            // Collision with bricks
            for (int i = 0; i < brickRows; i++)
            {
                for (int j = 0; j < brickCols; j++)
                {
                    if (bricks[i, j])
                    {
                        int brickX = j * brickWidth + brickOffsetX;
                        int brickY = i * brickHeight + brickOffsetY;

                        if (ballX + ballSize >= brickX &&
                            ballX <= brickX + brickWidth &&
                            ballY + ballSize >= brickY &&
                            ballY <= brickY + brickHeight)
                        {
                            bricks[i, j] = false;
                            score += pointsPerBrick;
                            UpdateUI();
                            ballSpeedY = -ballSpeedY;
                            //test
                            Console.Beep(800, 30);
                            //test

                            this.Refresh();

                            if (CheckWin())
                            {
                                WinGame();
                                return;
                            }
                            return;
                        }
                    }
                }
            }

            this.Refresh();
        }

        private bool CheckWin()
        {
            for (int i = 0; i < brickRows; i++)
                for (int j = 0; j < brickCols; j++)
                    if (bricks[i, j]) return false;
            return true;
        }

        private void WinGame()
        {
            gameRunning = false;
            gameOver = true;

            // Victory sound
            _ = PlayVictoryMelody();

            lblMessage.Visible = true;
            lblMessage.Text = $"★ ★ ★ VICTORY 1980 ★ ★ ★\nPRESS [RESTART]";
            lblMessage.ForeColor = Color.FromArgb(0, 255, 150);
            this.Refresh();
        }

        private void GameOver()
        {
            gameRunning = false;
            gameOver = true;

            // Game Over sound
            _ = PlayGameOverMelody();

            lblMessage.Visible = true;
            lblMessage.Text = "█ GAME OVER █\nPRESS [RESTART]";
            lblMessage.ForeColor = Color.FromArgb(255, 50, 80);
            this.Refresh();
        }

        private void UpdateUI()
        {
            lblScore.Text = $"⚡ SCORE: {score:000}";
            lblLives.Text = lives == 3 ? "❤️ x 3" : lives == 2 ? "❤️ x 2" : lives == 1 ? "❤️ x 1" : "💀 x 0";
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawNeonGrid(g);
            DrawGameAreaBorder(g);
            DrawNeonBall(g);
            DrawNeonPaddle(g);
            DrawRetroBricks(g);
            DrawRetroBorder(g);
        }

        private void DrawGameAreaBorder(Graphics g)
        {
            // Main game area border
            Pen borderPen = new Pen(Color.FromArgb((int)(150 * neonIntensity), 0, 255, 200), 3);
            g.DrawRectangle(borderPen, gameArea);

            // Inner glow
            for (int i = 1; i <= 3; i++)
            {
                Pen glowPen = new Pen(Color.FromArgb(20 / i, 0, 255, 255), 1);
                g.DrawRectangle(glowPen,
                    gameAreaX - i, gameAreaY - i,
                    gameAreaWidth + i * 2, gameAreaHeight + i * 2);
            }
        }

        private void DrawNeonGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.FromArgb(30, 0, 255, 200), 1);

            for (int y = gameAreaY; y < gameAreaY + gameAreaHeight; y += 40)
                g.DrawLine(gridPen, gameAreaX, y, gameAreaX + gameAreaWidth, y);

            for (int x = gameAreaX; x < gameAreaX + gameAreaWidth; x += 40)
                g.DrawLine(gridPen, x, gameAreaY, x, gameAreaY + gameAreaHeight);
        }

        private void DrawNeonBall(Graphics g)
        {
            // Glow effect
            for (int i = 3; i > 0; i--)
            {
                int alpha = 30 / i;
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(alpha, 255, 50, 150)))
                    g.FillEllipse(glow, ballX - i, ballY - i, ballSize + i * 2, ballSize + i * 2);
            }

            // Main ball
            using (SolidBrush ballBrush = new SolidBrush(Color.FromArgb(255, 255, 80, 180)))
                g.FillEllipse(ballBrush, ballX, ballY, ballSize, ballSize);

            // Highlight
            g.FillEllipse(Brushes.White, ballX + 3, ballY + 3, 3, 3);
        }

        private void DrawNeonPaddle(Graphics g)
        {
            // Bottom glow
            for (int i = 2; i > 0; i--)
            {
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(20 / i, 0, 255, 255)))
                    g.FillRectangle(glow, paddleX - i, paddleY + paddleHeight, paddleWidth + i * 2, 5);
            }

            // Paddle
            using (LinearGradientBrush paddleBrush = new LinearGradientBrush(
                new Rectangle(paddleX, paddleY, paddleWidth, paddleHeight),
                Color.FromArgb(0, 200, 255),
                Color.FromArgb(0, 100, 200),
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(paddleBrush, paddleX, paddleY, paddleWidth, paddleHeight);
            }

            // Neon border
            Pen neonPen = new Pen(Color.FromArgb((int)(150 * neonIntensity), 0, 255, 255), 2);
            g.DrawRectangle(neonPen, paddleX, paddleY, paddleWidth, paddleHeight);
        }

        private void DrawRetroBricks(Graphics g)
        {
            Color[] retroColors = {
                Color.FromArgb(0, 100, 200),   // Blue
                Color.FromArgb(0, 200, 200),   // Cyan
                Color.FromArgb(255, 50, 150),  // Pink
                Color.FromArgb(150, 50, 255),  // Purple
                Color.FromArgb(255, 100, 50)   // Orange
            };

            for (int i = 0; i < brickRows; i++)
            {
                for (int j = 0; j < brickCols; j++)
                {
                    if (bricks[i, j])
                    {
                        int x = j * brickWidth + brickOffsetX;
                        int y = i * brickHeight + brickOffsetY;

                        using (LinearGradientBrush brickBrush = new LinearGradientBrush(
                            new Rectangle(x, y, brickWidth - 2, brickHeight - 2),
                            retroColors[i],
                            Color.FromArgb(50, retroColors[i].R / 2, retroColors[i].G / 2, retroColors[i].B / 2),
                            LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(brickBrush, x, y, brickWidth - 2, brickHeight - 2);
                        }

                        g.DrawRectangle(Pens.White, x, y, brickWidth - 2, brickHeight - 2);
                    }
                }
            }
        }

        private void DrawRetroBorder(Graphics g)
        {
            Pen retroPen = new Pen(Color.FromArgb((int)(100 * neonIntensity), 0, 255, 200), 2);

            // 80's style corners
            g.DrawLine(retroPen, 10, 10, 40, 10);
            g.DrawLine(retroPen, 10, 10, 10, 40);
            g.DrawLine(retroPen, this.Width - 10, 10, this.Width - 40, 10);
            g.DrawLine(retroPen, this.Width - 10, 10, this.Width - 10, 40);
            g.DrawLine(retroPen, 10, this.Height - 10, 40, this.Height - 10);
            g.DrawLine(retroPen, 10, this.Height - 10, 10, this.Height - 40);
            g.DrawLine(retroPen, this.Width - 10, this.Height - 10, this.Width - 40, this.Height - 10);
            g.DrawLine(retroPen, this.Width - 10, this.Height - 10, this.Width - 10, this.Height - 40);
        }

        private async Task PlayVictoryMelody()
        {
            await Task.Run(() =>
            {
                Console.Beep(523, 200);
                Thread.Sleep(50);
                Console.Beep(659, 200);
                Thread.Sleep(50);
                Console.Beep(784, 400);
                Thread.Sleep(100);
                Console.Beep(1047, 600);
            });
        }

        private async Task PlayGameOverMelody()
        {
            await Task.Run(() =>
            {
                Console.Beep(440, 200);
                Thread.Sleep(100);
                Console.Beep(392, 200);
                Thread.Sleep(100);
                Console.Beep(349, 200);
                Thread.Sleep(100);
                Console.Beep(262, 600);
            });
        }

        private async Task PlayLoseLifeMelody()
        {
            await Task.Run(() =>
            {
                Console.Beep(294, 250);
                Thread.Sleep(50);
                Console.Beep(262, 250);
                Thread.Sleep(50);
                Console.Beep(233, 800);
            });
        }
    }
}