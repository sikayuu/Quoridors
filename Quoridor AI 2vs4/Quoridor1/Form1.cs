// ランダムvs原始モンテカルロ法

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

namespace Quoridor1
{
    public partial class Form1 : Form
    {
        const int N = 5;       // 盤面サイズ
        const int WALL_MAX = 3;  // 壁の最大数
        
        // ★モンテカルロ法のシミュレーション回数設定
        // 1手あたりのシミュレーション回数。多いほど強いが遅くなる。
        // 100戦ベンチマークを行うため、少し軽めの1000～1500程度に設定しています。
        const int SIMULATION_COUNT = 5000; 

        // 壁情報
        private int[,] kabeyoko = new int[N - 1, N];
        private int[,] kabetate = new int[N, N - 1];

        private int[,] ugokeru;           // 移動可能情報
        private int[] player0; // Player 0 (Random AI)
        private int[] player1; // Player 1 (MonteCarlo AI)

        private int[] playerWalls = new int[2];

        private int turn = 0; // 0: Random, 1: MonteCarlo
        private bool gameFinished = false;

        private Random rand = new Random();

        // --- 計測用変数 ---
        private long[] totalThinkingTimeMs = new long[2];
        private int[] totalMovesCount = new int[2];
        private int[] winCount = new int[2];
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        int W;
        int haba;
        int senhaba = 15;

        public Form1()
        {
            InitializeComponent();
        }

        // 座標変換
        private int xy2to1(int x, int y) { return x + N * y; }
        private int xy1to2x(int k) { return k % N; }
        private int xy1to2y(int k) { return k / N; }

        // **************************************************************
        // 表示処理
        // **************************************************************
        private void drawBoard()
        {
            if (W == 0) return;

            Bitmap b = new Bitmap(W, W);
            Graphics g = Graphics.FromImage(b);
            g.Clear(Color.AntiqueWhite);
            Pen p = new Pen(Color.DarkGray, 2);
            Pen p_kabe_player0 = new Pen(Color.DarkBlue, 8); // P0: Blue
            Pen p_kabe_player1 = new Pen(Color.DarkRed, 8);  // P1: Red

            // 格子線
            for (int i = 0; i <= N; i++)
            {
                g.DrawLine(p, 0, i * haba, W, i * haba);
                g.DrawLine(p, i * haba, 0, i * haba, W);
            }

            // 壁描画
            for (int xi = 0; xi < N - 1; xi++)
            {
                for (int yi = 0; yi < N; yi++)
                {
                    if (kabeyoko[xi, yi] > 0)
                    {
                        Pen wallPen = (kabeyoko[xi, yi] == 1) ? p_kabe_player0 : p_kabe_player1;
                        g.DrawLine(wallPen, (xi) * haba, (yi + 1) * haba, (xi + 2) * haba, (yi + 1) * haba);
                    }
                }
            }
            for (int xi = 0; xi < N; xi++)
            {
                for (int yi = 0; yi < N - 1; yi++)
                {
                    if (kabetate[xi, yi] > 0)
                    {
                        Pen wallPen = (kabetate[xi, yi] == 1) ? p_kabe_player0 : p_kabe_player1;
                        g.DrawLine(wallPen, (xi + 1) * haba, (yi) * haba, (xi + 1) * haba, (yi + 2) * haba);
                    }
                }
            }

            // プレイヤー描画
            // Player 0 (Random) = 黒
            int x = player0[0] * haba + 2;
            int y = player0[1] * haba + 2;
            g.FillEllipse(Brushes.Black, x, y, haba - 4, haba - 4);

            // Player 1 (MonteCarlo) = 白
            x = player1[0] * haba + 2;
            y = player1[1] * haba + 2;
            g.FillEllipse(Brushes.White, x, y, haba - 4, haba - 4);
            g.DrawEllipse(Pens.Black, x, y, haba - 4, haba - 4);

            p.Dispose();
            p_kabe_player0.Dispose();
            p_kabe_player1.Dispose();
            g.Dispose();
            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
            pictureBox1.Image = b;

            this.Text = string.Format("Random(P0) vs MonteCarlo(P1) - Turn: {0}", turn == 0 ? "Random" : "MonteCarlo");
        }

        // ********************************************************
        // データ初期化
        // ********************************************************
        private void InitGameData()
        {
            player0 = new int[] { N / 2, N - 1, xy2to1(N / 2, N - 1) }; // P0 (Goal y=0)
            player1 = new int[] { N / 2, 0, xy2to1(N / 2, 0) };         // P1 (Goal y=N-1)

            kabeyoko = new int[N - 1, N];
            kabetate = new int[N, N - 1];
            playerWalls[0] = WALL_MAX;
            playerWalls[1] = WALL_MAX;

            ugokeru = new int[N * N, N * N];

            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    int k1 = xy2to1(x, y);
                    if (y != (N - 1))
                    {
                        int k2 = xy2to1(x, y + 1);
                        ugokeru[k1, k2] = 1; ugokeru[k2, k1] = 1;
                    }
                    if (x != (N - 1))
                    {
                        int k2 = xy2to1(x + 1, y);
                        ugokeru[k1, k2] = 1; ugokeru[k2, k1] = 1;
                    }
                }
            }

            turn = rand.Next(2);
            gameFinished = false;
        }

        // ********************************************************
        // リセット・UI開始
        // ********************************************************
        private void reset()
        {
            W = pictureBox1.Width;
            haba = W / N;

            InitGameData();

            Bitmap b = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = b;
            drawBoard();

            // AI対戦開始
            StartAIGameLoop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            reset();
        }

        // ********************************************************
        // AI対戦ループ (観戦用・非同期)
        // ********************************************************
        private async void StartAIGameLoop()
        {
            await Task.Delay(500);

            while (!gameFinished)
            {
                if (this.IsDisposed) return;

                // AI思考と実行
                await RunAITurnAsync();

                drawBoard();

                if (CheckGoal()) break;

                turn = 1 - turn;
                await Task.Delay(500); // 観戦しやすいようにウェイト
            }
        }

        private async Task RunAITurnAsync()
        {
            if (gameFinished) return;

            if (turn == 0)
            {
                // P0: Random (計算は軽いため同期実行でも良いが、統一感のためTask化も可)
                ProcessRandomTurn(0); 
            }
            else
            {
                // P1: MonteCarlo (重いのでTask.Run)
                SimAction bestAction = await Task.Run(() => ThinkMonteCarlo(1));
                ApplyBestAction(bestAction, 1);
            }
        }

        // ********************************************************
        // 100戦ベンチマーク (同期処理で高速化)
        // ********************************************************
        private void RunBenchmark100()
        {
            Array.Clear(totalThinkingTimeMs, 0, 2);
            Array.Clear(totalMovesCount, 0, 2);
            Array.Clear(winCount, 0, 2);

            int totalGames = 100;
            int drawCount = 0;

            for (int i = 0; i < totalGames; i++)
            {
                InitGameData();
                int currentTurnCount = 0;

                while (!gameFinished)
                {
                    // 200手制限
                    if (currentTurnCount > 200)
                    {
                        drawCount++;
                        break;
                    }

                    sw.Restart();

                    // 同期実行
                    if (turn == 0)
                    {
                        ProcessRandomTurn(0);
                    }
                    else
                    {
                        SimAction bestAction = ThinkMonteCarlo(1);
                        ApplyBestAction(bestAction, 1);
                    }

                    sw.Stop();
                    totalThinkingTimeMs[turn] += sw.ElapsedMilliseconds;
                    totalMovesCount[turn]++;
                    currentTurnCount++;

                    if (player0[1] == 0) { winCount[0]++; gameFinished = true; }
                    else if (player1[1] == N - 1) { winCount[1]++; gameFinished = true; }

                    if (!gameFinished) turn = 1 - turn;
                }
                
                // フリーズ防止
                if (i % 5 == 0) Application.DoEvents();
            }

            ShowBenchmarkResult(totalGames, drawCount);
        }

        private void ShowBenchmarkResult(int games, int draws)
        {
            string msg = $"【{games}戦の結果 (Random vs MonteCarlo)】\n";
            msg += $"引き分け(200手): {draws}回\n\n";

            string[] names = { "Player 0 (Random)", "Player 1 (MonteCarlo)" };

            for (int p = 0; p < 2; p++)
            {
                double winRate = (double)winCount[p] / games * 100.0;
                double avgMoves = totalMovesCount[p] > 0 ? (double)totalMovesCount[p] / games : 0;
                double avgTime = totalMovesCount[p] > 0 ? (double)totalThinkingTimeMs[p] / totalMovesCount[p] : 0;

                msg += $"[{names[p]}]\n";
                msg += $"  勝率: {winRate:F1}%\n";
                msg += $"  平均手数: {avgMoves:F1}手/試合\n";
                msg += $"  平均思考時間: {avgTime:F4}ms/手\n\n";
            }

            MessageBox.Show(msg, "シミュレーション完了");
            reset();
        }

        private void btnBenchmark_Click(object sender, EventArgs e)
        {
            gameFinished = true; // 現在のループを停止
            this.Text = "シミュレーション中... (数分かかる場合があります)";
            Application.DoEvents();
            RunBenchmark100();
        }

        // ********************************************************
        // 共通判定ロジック
        // ********************************************************
        private bool CheckGoal()
        {
            if (player0[1] == 0)
            {
                gameFinished = true;
                MessageBox.Show("Player 0 (Random) Win!");
                return true;
            }
            if (player1[1] == N - 1)
            {
                gameFinished = true;
                MessageBox.Show("Player 1 (MonteCarlo) Win!");
                return true;
            }
            return false;
        }

        private void リセット_Click(object sender, EventArgs e)
        {
            gameFinished = true;
            reset();
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e) { }


        // =========================================================
        // Player 0: Random Logic (Code 1ベース)
        // =========================================================
        private void ProcessRandomTurn(int playerIndex)
        {
            bool actionTaken = false;
            // 壁があるなら50%で壁設置を試みる
            if (playerWalls[playerIndex] > 0 && rand.Next(2) == 0)
            {
                actionTaken = AIPlaceWall_Random(playerIndex);
            }
            if (!actionTaken)
            {
                actionTaken = AIMovePawn_Random(playerIndex);
            }
            // 移動も壁もできなかった場合のバックアップ
            if (!actionTaken && playerWalls[playerIndex] > 0)
            {
                actionTaken = AIPlaceWall_Random(playerIndex, 1000);
            }
            if (!actionTaken)
            {
                actionTaken = AIMovePawn_Random(playerIndex);
            }
        }

        private bool AIMovePawn_Random(int playerIndex)
        {
            int[] currentPlayer = player0; // P0
            int[] opponentPlayer = player1; // P1
            int goalY = 0; // P0 Goal

            List<int> possibleMoves = new List<int>();
            int currentK = currentPlayer[2];
            int opponentK = opponentPlayer[2];

            for (int k2 = 0; k2 < N * N; k2++)
            {
                // CanMovePawnの判定はSimStateのロジックを流用せず、Code1のロジックを簡易実装
                if (IsLegalMove(currentK, k2, opponentK)) possibleMoves.Add(k2);
            }

            if (possibleMoves.Count > 0)
            {
                int bestMove = -1;
                int bestY = N; 
                List<int> tiedMoves = new List<int>();

                // ゴールに近づく手を優先する「賢いランダム」
                foreach (int move in possibleMoves)
                {
                    int moveY = xy1to2y(move);
                    if (moveY == goalY) { bestMove = move; tiedMoves.Clear(); break; }

                    bool isBetter = (moveY < bestY); // P0はy=0を目指すので小さい方が良い
                    bool isTied = (moveY == bestY);

                    if (isBetter) { bestY = moveY; bestMove = move; tiedMoves.Clear(); tiedMoves.Add(move); }
                    else if (isTied) { tiedMoves.Add(move); }
                }

                if (tiedMoves.Count > 0) bestMove = tiedMoves[rand.Next(tiedMoves.Count)];

                if (bestMove != -1)
                {
                    currentPlayer[0] = xy1to2x(bestMove);
                    currentPlayer[1] = xy1to2y(bestMove);
                    currentPlayer[2] = bestMove;
                    return true;
                }
            }
            return false;
        }

        private bool AIPlaceWall_Random(int playerIndex, int maxAttempts = 50)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                int x = rand.Next(N - 1);
                int y = rand.Next(N - 1);
                bool isVert = rand.Next(2) == 0;

                if (isVert)
                {
                    if (y < N - 1 && CanPlaceWall(x, y, true))
                    {
                        PlaceVerticalWall(x, y, playerIndex);
                        return true;
                    }
                }
                else
                {
                    if (x < N - 1 && CanPlaceWall(x, y, false))
                    {
                        PlaceHorizontalWall(x, y, playerIndex);
                        return true;
                    }
                }
            }
            return false;
        }

        // P0用：盤面更新メソッド
        private void PlaceVerticalWall(int x, int y, int playerIndex)
        {
            kabetate[x, y] = playerIndex + 1;
            playerWalls[playerIndex]--;
            ugokeru[xy2to1(x, y), xy2to1(x + 1, y)] = 0; ugokeru[xy2to1(x + 1, y), xy2to1(x, y)] = 0;
            ugokeru[xy2to1(x, y + 1), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x, y + 1)] = 0;
        }
        private void PlaceHorizontalWall(int x, int y, int playerIndex)
        {
            kabeyoko[x, y] = playerIndex + 1;
            playerWalls[playerIndex]--;
            ugokeru[xy2to1(x, y), xy2to1(x, y + 1)] = 0; ugokeru[xy2to1(x, y + 1), xy2to1(x, y)] = 0;
            ugokeru[xy2to1(x + 1, y), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x + 1, y)] = 0;
        }

        // P0用：簡易ルール判定（SimStateを使わずに直接判定）
        private bool IsLegalMove(int k1, int k2, int opponentK)
        {
            int dist = Math.Abs(xy1to2x(k1) - xy1to2x(k2)) + Math.Abs(xy1to2y(k1) - xy1to2y(k2));
            if (dist == 1 && ugokeru[k1, k2] == 1 && k2 != opponentK) return true;
            // ジャンプ判定は省略（ランダムAIなので隣接移動メインで十分機能する）
            return false; 
        }

        private bool CanPlaceWall(int x, int y, bool isVertical)
        {
            // 重なりチェック
            if (isVertical)
            {
                if (kabetate[x, y] != 0) return false;
                if ((y > 0 && kabetate[x, y - 1] != 0) || (y < N - 2 && kabetate[x, y + 1] != 0)) return false;
                if (kabeyoko[x, y] != 0) return false;
            }
            else
            {
                if (kabeyoko[x, y] != 0) return false;
                if ((x > 0 && kabeyoko[x - 1, y] != 0) || (x < N - 2 && kabeyoko[x + 1, y] != 0)) return false;
                if (kabetate[x, y] != 0) return false;
            }

            // 仮置きしてパスチェック
            int k1, k2, k3, k4;
            if (isVertical)
            {
                k1 = xy2to1(x, y); k2 = xy2to1(x + 1, y);
                k3 = xy2to1(x, y + 1); k4 = xy2to1(x + 1, y + 1);
            }
            else
            {
                k1 = xy2to1(x, y); k2 = xy2to1(x, y + 1);
                k3 = xy2to1(x + 1, y); k4 = xy2to1(x + 1, y + 1);
            }

            ugokeru[k1, k2] = 0; ugokeru[k2, k1] = 0;
            ugokeru[k3, k4] = 0; ugokeru[k4, k3] = 0;

            bool p0Ok = HasPathBFS(player0[2], 0);
            bool p1Ok = HasPathBFS(player1[2], N - 1);

            ugokeru[k1, k2] = 1; ugokeru[k2, k1] = 1;
            ugokeru[k3, k4] = 1; ugokeru[k4, k3] = 1;

            return p0Ok && p1Ok;
        }

        private bool HasPathBFS(int startK, int goalY)
        {
            Queue<int> q = new Queue<int>();
            bool[] visited = new bool[N * N];
            q.Enqueue(startK);
            visited[startK] = true;
            while(q.Count > 0)
            {
                int curr = q.Dequeue();
                if (curr / N == goalY) return true;
                int cx = curr % N, cy = curr / N;
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                for(int i=0; i<4; i++)
                {
                    int nx = cx + dx[i], ny = cy + dy[i];
                    if(nx>=0 && nx<N && ny>=0 && ny<N)
                    {
                        int nk = nx + ny * N;
                        if (!visited[nk] && ugokeru[curr, nk] == 1) { visited[nk] = true; q.Enqueue(nk); }
                    }
                }
            }
            return false;
        }


        // =========================================================
        // Player 1: MonteCarlo Logic (Code 3ベース)
        // =========================================================
        
        // 思考のエントリポイント
        private SimAction ThinkMonteCarlo(int playerIndex)
        {
            SimState rootState = new SimState(
                this.ugokeru,
                this.kabeyoko,
                this.kabetate,
                this.player0,
                this.player1,
                this.playerWalls,
                playerIndex
            );

            List<SimAction> legalActions = rootState.GetLegalActions();
            if (legalActions.Count == 0) return null;

            int totalSimulations = SIMULATION_COUNT;
            int simsPerAction = Math.Max(1, totalSimulations / legalActions.Count);

            // 並列処理でシミュレーション実行
            Parallel.ForEach(legalActions, action =>
            {
                int wins = 0;
                // スレッドごとにRandomインスタンスを作成しないと競合するため注意が必要
                // ただしSimState.DeepClone()内で個別にRandomを持つ設計にする
                for (int i = 0; i < simsPerAction; i++)
                {
                    SimState simState = rootState.DeepClone();
                    simState.ApplyAction(action);
                    int winner = simState.PlayOut();

                    if (winner == playerIndex)
                    {
                        wins++;
                    }
                }
                action.Score = (double)wins / simsPerAction;
            });

            SimAction best = legalActions.OrderByDescending(a => a.Score).First();
            return best;
        }

        // 行動適用メソッド
        private void ApplyBestAction(SimAction action, int playerIndex)
        {
            if (action == null) return;

            if (action.Type == ActionType.Move)
            {
                // Player 1 Only
                player1[0] = xy1to2x(action.TargetK);
                player1[1] = xy1to2y(action.TargetK);
                player1[2] = action.TargetK;
            }
            else if (action.Type == ActionType.VerticalWall)
            {
                PlaceVerticalWall(action.WallX, action.WallY, playerIndex);
            }
            else if (action.Type == ActionType.HorizontalWall)
            {
                PlaceHorizontalWall(action.WallX, action.WallY, playerIndex);
            }
        }
        
        // --- 以下、シミュレーション用クラス群 ---

        enum ActionType { Move, VerticalWall, HorizontalWall }

        class SimAction
        {
            public ActionType Type;
            public int TargetK;
            public int WallX;
            public int WallY;
            public double Score;
        }

        class SimState
        {
            public int[,] Ugokeru;
            public int[,] Kabeyoko;
            public int[,] Kabetate;
            public int[] P0;
            public int[] P1;
            public int[] Walls;
            public int Turn;

            private Random _rng; // スレッドセーフ用

            public SimState(int[,] u, int[,] ky, int[,] kt, int[] p0, int[] p1, int[] walls, int turn)
            {
                Ugokeru = (int[,])u.Clone();
                Kabeyoko = (int[,])ky.Clone();
                Kabetate = (int[,])kt.Clone();
                P0 = (int[])p0.Clone();
                P1 = (int[])p1.Clone();
                Walls = (int[])walls.Clone();
                Turn = turn;
                _rng = new Random(Guid.NewGuid().GetHashCode()); // ランダムシード
            }

            public SimState DeepClone()
            {
                return new SimState(Ugokeru, Kabeyoko, Kabetate, P0, P1, Walls, Turn);
            }

            public int PlayOut()
            {
                int movesCount = 0;
                while (movesCount < 100) // 100手打ち切り
                {
                    if (P0[1] == 0) return 0;
                    if (P1[1] == N - 1) return 1;

                    List<SimAction> actions = GetLegalActions();
                    if (actions.Count == 0) return 1 - Turn;

                    SimAction selectedAction = null;
                    var moveActions = actions.Where(a => a.Type == ActionType.Move).ToList();

                    // ランダムプレイアウトだが、少し賢く前進する確率を上げる
                    if (moveActions.Count > 0 && _rng.NextDouble() < 0.3)
                    {
                        int currentY = (Turn == 0) ? P0[1] : P1[1];
                        int goalY = (Turn == 0) ? 0 : N - 1;
                        int currentDist = Math.Abs(currentY - goalY);
                        var betterMoves = moveActions.Where(a =>
                        {
                            int nextY = a.TargetK / N;
                            return Math.Abs(nextY - goalY) < currentDist;
                        }).ToList();

                        if (betterMoves.Count > 0) selectedAction = betterMoves[_rng.Next(betterMoves.Count)];
                        else selectedAction = moveActions[_rng.Next(moveActions.Count)];
                    }
                    else
                    {
                        selectedAction = actions[_rng.Next(actions.Count)];
                    }

                    ApplyAction(selectedAction);
                    movesCount++;
                }
                return -1; // Draw
            }

            public void ApplyAction(SimAction action)
            {
                if (action.Type == ActionType.Move)
                {
                    if (Turn == 0)
                    {
                        P0[2] = action.TargetK;
                        P0[0] = action.TargetK % N; P0[1] = action.TargetK / N;
                    }
                    else
                    {
                        P1[2] = action.TargetK;
                        P1[0] = action.TargetK % N; P1[1] = action.TargetK / N;
                    }
                }
                else
                {
                    int x = action.WallX;
                    int y = action.WallY;
                    Walls[Turn]--;
                    if (action.Type == ActionType.VerticalWall)
                    {
                        Kabetate[x, y] = Turn + 1;
                        CutLink(xy2to1(x, y), xy2to1(x + 1, y));
                        CutLink(xy2to1(x, y + 1), xy2to1(x + 1, y + 1));
                    }
                    else
                    {
                        Kabeyoko[x, y] = Turn + 1;
                        CutLink(xy2to1(x, y), xy2to1(x, y + 1));
                        CutLink(xy2to1(x + 1, y), xy2to1(x + 1, y + 1));
                    }
                }
                Turn = 1 - Turn;
            }

            private void CutLink(int k1, int k2)
            {
                Ugokeru[k1, k2] = 0; Ugokeru[k2, k1] = 0;
            }
            private int xy2to1(int x, int y) { return x + N * y; }

            public List<SimAction> GetLegalActions()
            {
                List<SimAction> actions = new List<SimAction>();
                int myK = (Turn == 0) ? P0[2] : P1[2];
                int oppK = (Turn == 0) ? P1[2] : P0[2];

                // 移動候補
                for (int k2 = 0; k2 < N * N; k2++)
                {
                    if (myK == k2) continue;
                    if (SimCanMove(myK, k2, oppK))
                    {
                        actions.Add(new SimAction { Type = ActionType.Move, TargetK = k2 });
                    }
                }

                // 壁候補
                if (Walls[Turn] > 0)
                {
                    for (int x = 0; x < N - 1; x++)
                    {
                        for (int y = 0; y < N - 1; y++)
                        {
                            if (SimCanPlaceWall(x, y, true)) actions.Add(new SimAction { Type = ActionType.VerticalWall, WallX = x, WallY = y });
                            if (SimCanPlaceWall(x, y, false)) actions.Add(new SimAction { Type = ActionType.HorizontalWall, WallX = x, WallY = y });
                        }
                    }
                }
                return actions;
            }

            private bool SimCanMove(int k1, int k2, int opponentK)
            {
                int x1 = k1 % N; int y1 = k1 / N;
                int x2 = k2 % N; int y2 = k2 / N;
                int oppX = opponentK % N; int oppY = opponentK / N;
                int dist = Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

                if (dist == 1)
                {
                    if (Ugokeru[k1, k2] == 1 && k2 != opponentK) return true;
                }
                if (Ugokeru[k1, opponentK] == 1)
                {
                    int dx = oppX - x1; int dy = oppY - y1;
                    int jumpX = oppX + dx; int jumpY = oppY + dy;
                    if (jumpX >= 0 && jumpX < N && jumpY >= 0 && jumpY < N)
                    {
                        int jumpK = xy2to1(jumpX, jumpY);
                        if (Ugokeru[opponentK, jumpK] == 1)
                        {
                            if (k2 == jumpK) return true;
                        }
                        else goto DiagonalJump;
                    }
                    else goto DiagonalJump;

                    return false;
                DiagonalJump:
                    if (dx == 0)
                    {
                        if (oppX - 1 >= 0)
                        {
                            int leftK = xy2to1(oppX - 1, oppY);
                            if (k2 == leftK && Ugokeru[opponentK, leftK] == 1) return true;
                        }
                        if (oppX + 1 < N)
                        {
                            int rightK = xy2to1(oppX + 1, oppY);
                            if (k2 == rightK && Ugokeru[opponentK, rightK] == 1) return true;
                        }
                    }
                    else
                    {
                        if (oppY - 1 >= 0)
                        {
                            int upK = xy2to1(oppX, oppY - 1);
                            if (k2 == upK && Ugokeru[opponentK, upK] == 1) return true;
                        }
                        if (oppY + 1 < N)
                        {
                            int downK = xy2to1(oppX, oppY + 1);
                            if (k2 == downK && Ugokeru[opponentK, downK] == 1) return true;
                        }
                    }
                }
                return false;
            }

            private bool SimCanPlaceWall(int x, int y, bool isVertical)
            {
                if (isVertical)
                {
                    if (Kabetate[x, y] != 0) return false;
                    if ((y > 0 && Kabetate[x, y - 1] != 0) || (y < N - 2 && Kabetate[x, y + 1] != 0)) return false;
                    if (Kabeyoko[x, y] != 0) return false;
                }
                else
                {
                    if (Kabeyoko[x, y] != 0) return false;
                    if ((x > 0 && Kabeyoko[x - 1, y] != 0) || (x < N - 2 && Kabeyoko[x + 1, y] != 0)) return false;
                    if (Kabetate[x, y] != 0) return false;
                }

                int k1, k2, k3, k4;
                if (isVertical)
                {
                    k1 = xy2to1(x, y); k2 = xy2to1(x + 1, y);
                    k3 = xy2to1(x, y + 1); k4 = xy2to1(x + 1, y + 1);
                }
                else
                {
                    k1 = xy2to1(x, y); k2 = xy2to1(x, y + 1);
                    k3 = xy2to1(x + 1, y); k4 = xy2to1(x + 1, y + 1);
                }

                Ugokeru[k1, k2] = 0; Ugokeru[k2, k1] = 0;
                Ugokeru[k3, k4] = 0; Ugokeru[k4, k3] = 0;

                bool pathP0 = HasPath(P0[2], 0);
                bool pathP1 = HasPath(P1[2], N - 1);

                Ugokeru[k1, k2] = 1; Ugokeru[k2, k1] = 1;
                Ugokeru[k3, k4] = 1; Ugokeru[k4, k3] = 1;

                return pathP0 && pathP1;
            }

            private bool HasPath(int startK, int goalY)
            {
                Queue<int> q = new Queue<int>();
                bool[] visited = new bool[N * N];
                q.Enqueue(startK);
                visited[startK] = true;
                while (q.Count > 0)
                {
                    int curr = q.Dequeue();
                    if (curr / N == goalY) return true;
                    int cx = curr % N; int cy = curr / N;
                    int[] dx = { 1, -1, 0, 0 };
                    int[] dy = { 0, 0, 1, -1 };
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cx + dx[i]; int ny = cy + dy[i];
                        if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                        {
                            int nextK = nx + ny * N;
                            if (!visited[nextK] && Ugokeru[curr, nextK] == 1)
                            {
                                visited[nextK] = true;
                                q.Enqueue(nextK);
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}