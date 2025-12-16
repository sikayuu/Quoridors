// A*探索vs原始モンテカルロ法


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

namespace Quoridor1
{
    // ==========================================================
    // A*探索用のノードクラス
    // ==========================================================
    public class AStarNode
    {
        public int K { get; set; }
        public int G { get; set; }
        public int H { get; set; }
        public int F => G + H;
        public AStarNode Parent { get; set; }

        public AStarNode(int k, int g, int h, AStarNode parent = null)
        {
            K = k; G = g; H = h; Parent = parent;
        }
    }

    public partial class Form1 : Form
    {
        const int N = 5;       // 盤面サイズ
        const int WALL_MAX = 3;  // 壁の最大数

        // モンテカルロ法のシミュレーション回数
        const int MC_SIMULATION_COUNT = 10000;

        // 壁情報
        private int[,] kabeyoko = new int[N - 1, N];
        private int[,] kabetate = new int[N, N - 1];

        private int[,] ugokeru;           // 移動可能情報
        private int[] player0; // Player 0 (A* AI) - 下側スタート
        private int[] player1; // Player 1 (MonteCarlo AI) - 上側スタート

        private int[] playerWalls = new int[2];

        private int turn = 0; // 0: A*, 1: MonteCarlo
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
            Pen p_kabe_player0 = new Pen(Color.DarkBlue, 8); // A* (Blue)
            Pen p_kabe_player1 = new Pen(Color.DarkRed, 8);  // MC (Red)

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
            // Player 0 (A*) = 黒
            int x = player0[0] * haba + 2;
            int y = player0[1] * haba + 2;
            g.FillEllipse(Brushes.Black, x, y, haba - 4, haba - 4);

            // Player 1 (MC) = 白
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

            this.Text = string.Format("A*(P0) vs MonteCarlo(P1) - Walls: {0} vs {1} - Turn: {2}",
                playerWalls[0], playerWalls[1], turn == 0 ? "A*" : "MonteCarlo");
        }

        // ********************************************************
        // データ初期化
        // ********************************************************
        private void InitGameData()
        {
            player0 = new int[] { N / 2, N - 1, xy2to1(N / 2, N - 1) }; // P0: A* (Goal y=0)
            player1 = new int[] { N / 2, 0, xy2to1(N / 2, 0) };         // P1: MC (Goal y=N-1)

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

            turn = rand.Next(2); // 先攻後攻ランダム
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

                await RunAITurnAsync();

                drawBoard();

                if (CheckGoal()) break;

                turn = 1 - turn;
                await Task.Delay(500);
            }
        }

        private async Task RunAITurnAsync()
        {
            if (gameFinished) return;

            if (turn == 0)
            {
                // P0: A* (計算は軽いがTaskでラップ)
                await Task.Run(() => ProcessAStarTurn(0));
            }
            else
            {
                // P1: MonteCarlo (重いのでTask.Run)
                SimAction bestAction = await Task.Run(() => ThinkMonteCarlo(1));
                ApplyMCAction(bestAction, 1);
            }
        }

        // ********************************************************
        // 100戦ベンチマーク (同期処理)
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
                    // 200手制限 (無限ループ防止)
                    if (currentTurnCount > 200)
                    {
                        drawCount++;
                        break;
                    }

                    sw.Restart();

                    // 同期実行
                    if (turn == 0)
                    {
                        ProcessAStarTurn(0);
                    }
                    else
                    {
                        SimAction bestAction = ThinkMonteCarlo(1);
                        ApplyMCAction(bestAction, 1);
                    }

                    sw.Stop();
                    totalThinkingTimeMs[turn] += sw.ElapsedMilliseconds;
                    totalMovesCount[turn]++;
                    currentTurnCount++;

                    if (player0[1] == 0) { winCount[0]++; gameFinished = true; }
                    else if (player1[1] == N - 1) { winCount[1]++; gameFinished = true; }

                    if (!gameFinished) turn = 1 - turn;
                }

                Console.WriteLine($"勝ち比 : {winCount[0]} : {winCount[1]}");
                // UIフリーズ防止
                if (i % 5 == 0) Application.DoEvents();
            }

            ShowBenchmarkResult(totalGames, drawCount);
        }

        private void ShowBenchmarkResult(int games, int draws)
        {
            string msg = $"【{games}戦の結果 (A* vs MonteCarlo)】\n";
            msg += $"引き分け(200手): {draws}回\n\n";

            string[] names = { "Player 0 (A*)", "Player 1 (MonteCarlo)" };

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
            gameFinished = true;
            this.Text = "シミュレーション中... (MCは計算に時間がかかります)";
            Application.DoEvents();
            RunBenchmark100();
        }

        // ********************************************************
        // 共通メソッド
        // ********************************************************
        private bool CheckGoal()
        {
            if (player0[1] == 0)
            {
                gameFinished = true;
                MessageBox.Show("Player 0 (A*) Win!");
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
        // Player 0: A* Search Logic
        // =========================================================
        private void ProcessAStarTurn(int playerIndex)
        {
            // P0: 自分がplayer0, 相手がplayer1
            int[] currentPlayer = player0;
            int[] opponentPlayer = player1;
            int myGoalY = 0;
            int oppGoalY = N - 1;

            // 経路コスト計算
            int currentAIPath = AStarPathLength(currentPlayer[2], myGoalY);
            int currentOpponentPath = AStarPathLength(opponentPlayer[2], oppGoalY);

            // 最適な駒移動を見つける
            (int moveK, int moveCost) = FindBestPawnMove_AStar(playerIndex, currentAIPath, myGoalY);
            int moveBenefit = currentAIPath - moveCost;

            // 最適な壁設置を見つける
            (bool wallSuccess, bool isVertical, int wallX, int wallY, int wallCostIncrease) = FindBestWallPlacement_AStar(playerIndex, currentOpponentPath, oppGoalY);
            int wallBenefit = wallCostIncrease;

            // 緊急事態処理（相手がゴール直前なら妨害優先度アップ）
            int originalWallBenefit = wallBenefit;
            if (currentOpponentPath <= 2 && playerWalls[playerIndex] > 0 && wallSuccess)
            {
                if (wallBenefit >= 1) wallBenefit = moveBenefit + 100;
            }

            bool actionTaken = false;
            // 閾値設定：自分が負けている、または接戦なら壁を置く価値を高く見積もる
            int wallThreshold = (currentAIPath <= currentOpponentPath) ? 2 : 1;

            bool wallIsEffective = wallSuccess && (wallBenefit > originalWallBenefit || originalWallBenefit >= wallThreshold);

            // 壁設置を実行するか？
            if (playerWalls[playerIndex] > 0 && wallIsEffective && (wallBenefit > moveBenefit))
            {
                bool placementResult;
                if (isVertical) placementResult = PlaceVerticalWall(wallX, wallY, playerIndex);
                else placementResult = PlaceHorizontalWall(wallX, wallY, playerIndex);

                if (placementResult) actionTaken = true;
            }

            // 壁を置かなかった、または置けなかった場合は移動
            if (!actionTaken && moveK != -1)
            {
                currentPlayer[0] = xy1to2x(moveK);
                currentPlayer[1] = xy1to2y(moveK);
                currentPlayer[2] = moveK;
                actionTaken = true;
            }
        }

        // A*用: 駒移動探索
        private (int bestMoveK, int minPathCost) FindBestPawnMove_AStar(int playerIndex, int currentPathCost, int goalY)
        {
            int[] currentPlayer = (playerIndex == 0) ? player0 : player1;
            int[] opponentPlayer = (playerIndex == 0) ? player1 : player0;

            List<int> possibleMoves = new List<int>();
            int currentK = currentPlayer[2];
            int opponentK = opponentPlayer[2];

            for (int k2 = 0; k2 < N * N; k2++)
            {
                if (k2 == currentK) continue;
                if (CanMovePawn_Main(currentK, k2, opponentK)) possibleMoves.Add(k2);
            }

            int minPathCost = currentPathCost;
            int bestMove = -1;
            List<int> bestMoves = new List<int>();

            foreach (int move in possibleMoves)
            {
                int pathCost = AStarPathLength(move, goalY);
                if (pathCost < minPathCost)
                {
                    minPathCost = pathCost;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (pathCost == minPathCost)
                {
                    bestMoves.Add(move);
                }
            }

            if (bestMoves.Count > 0)
            {
                bestMove = bestMoves[rand.Next(bestMoves.Count)];
                return (bestMove, minPathCost);
            }
            return (-1, currentPathCost);
        }

        // A*用: 壁設置探索
        private (bool success, bool isVertical, int bestX, int bestY, int costIncrease) FindBestWallPlacement_AStar(int playerIndex, int currentOpponentPath, int oppGoalY)
        {
            if (playerWalls[playerIndex] <= 0) return (false, false, -1, -1, 0);

            int maxCostIncrease = 0;
            int bestX = -1, bestY = -1;
            bool isVertical = false;

            var candidates = new List<(int x, int y, bool isVert)>();
            for (int x = 0; x < N - 1; x++)
            {
                for (int y = 0; y < N - 1; y++)
                {
                    candidates.Add((x, y, true));
                    candidates.Add((x, y, false));
                }
            }
            // ランダム順にして予測不能性を出す
            candidates = candidates.OrderBy(a => rand.Next()).ToList();

            int opponentIndex = 1 - playerIndex;
            int[] opponentPlayer = (opponentIndex == 0) ? player0 : player1;

            foreach (var cand in candidates)
            {
                // 仮置きしてチェック
                if (TryWallPlacement_Main(cand.x, cand.y, cand.isVert, playerIndex, out _))
                {
                    // 相手のパスがどう変わったか
                    int newOpponentPath = AStarPathLength(opponentPlayer[2], oppGoalY);
                    int increase = newOpponentPath - currentOpponentPath;

                    if (increase > maxCostIncrease)
                    {
                        maxCostIncrease = increase;
                        bestX = cand.x; bestY = cand.y; isVertical = cand.isVert;
                    }

                    // 仮置き解除
                    RemoveWall_Main(cand.x, cand.y, cand.isVert);
                }
            }

            if (maxCostIncrease > 0) return (true, isVertical, bestX, bestY, maxCostIncrease);
            return (false, false, -1, -1, 0);
        }

        private int AStarPathLength(int startNode, int goalY)
        {
            int Heuristic(int k) { return Math.Abs(xy1to2y(k) - goalY); }

            var openList = new List<AStarNode>();
            var allNodes = new Dictionary<int, AStarNode>();
            AStarNode startNodeObj = new AStarNode(startNode, 0, Heuristic(startNode));
            openList.Add(startNodeObj);
            allNodes.Add(startNode, startNodeObj);

            while (openList.Count > 0)
            {
                AStarNode current = openList.OrderBy(n => n.F).ThenBy(n => n.H).First();
                openList.Remove(current);

                if (xy1to2y(current.K) == goalY) return current.G;

                int x = xy1to2x(current.K); int y = xy1to2y(current.K);
                int[] dx = { 0, 0, 1, -1 }; int[] dy = { 1, -1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i]; int ny = y + dy[i];
                    if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                    {
                        int neighborK = xy2to1(nx, ny);
                        if (ugokeru[current.K, neighborK] == 1)
                        {
                            int newG = current.G + 1;
                            AStarNode neighborNode;
                            if (allNodes.TryGetValue(neighborK, out neighborNode))
                            {
                                if (newG < neighborNode.G)
                                {
                                    neighborNode.G = newG;
                                    neighborNode.Parent = current;
                                    if (!openList.Contains(neighborNode)) openList.Add(neighborNode);
                                }
                            }
                            else
                            {
                                neighborNode = new AStarNode(neighborK, newG, Heuristic(neighborK), current);
                                allNodes.Add(neighborK, neighborNode);
                                openList.Add(neighborNode);
                            }
                        }
                    }
                }
            }
            return int.MaxValue;
        }

        // =========================================================
        // Player 1: Monte Carlo Logic
        // =========================================================

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

            int totalSimulations = MC_SIMULATION_COUNT;
            int simsPerAction = Math.Max(1, totalSimulations / legalActions.Count);

            // 並列シミュレーション
            Parallel.ForEach(legalActions, action =>
            {
                int wins = 0;
                for (int i = 0; i < simsPerAction; i++)
                {
                    SimState simState = rootState.DeepClone();
                    simState.ApplyAction(action);
                    int winner = simState.PlayOut();

                    if (winner == playerIndex) wins++;
                }
                action.Score = (double)wins / simsPerAction;
            });

            SimAction best = legalActions.OrderByDescending(a => a.Score).First();
            return best;
        }

        private void ApplyMCAction(SimAction action, int playerIndex)
        {
            if (action == null) return;
            if (action.Type == ActionType.Move)
            {
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

        // =========================================================
        // ゲーム操作ヘルパー (Main用)
        // =========================================================

        private bool PlaceVerticalWall(int x, int y, int playerIndex)
        {
            if (!TryWallPlacement_Main(x, y, true, playerIndex, out _)) return false;
            // TryWallPlacement_Main で成功なら壁は設置済み、失敗なら戻されているが、
            // A*探索では「仮置き→評価→戻す」をしているので、
            // ここでは「本当に置く」処理として、TryWallPlacementの処理を確定させる必要がある。
            // しかしTryWallPlacementは「仮置きしてパスがあるか」を見るもの。
            // 簡略化のため、もう一度チェックして確定させる。

            kabetate[x, y] = playerIndex + 1;
            playerWalls[playerIndex]--;
            ugokeru[xy2to1(x, y), xy2to1(x + 1, y)] = 0; ugokeru[xy2to1(x + 1, y), xy2to1(x, y)] = 0;
            ugokeru[xy2to1(x, y + 1), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x, y + 1)] = 0;
            return true;
        }

        private bool PlaceHorizontalWall(int x, int y, int playerIndex)
        {
            if (!TryWallPlacement_Main(x, y, false, playerIndex, out _)) return false;

            kabeyoko[x, y] = playerIndex + 1;
            playerWalls[playerIndex]--;
            ugokeru[xy2to1(x, y), xy2to1(x, y + 1)] = 0; ugokeru[xy2to1(x, y + 1), xy2to1(x, y)] = 0;
            ugokeru[xy2to1(x + 1, y), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x + 1, y)] = 0;
            return true;
        }

        private bool CanMovePawn_Main(int k1, int k2, int opponentK)
        {
            // Code 1のロジックを使用
            int x1 = xy1to2x(k1); int y1 = xy1to2y(k1);
            int x2 = xy1to2x(k2); int y2 = xy1to2y(k2);
            int oppX = xy1to2x(opponentK); int oppY = xy1to2y(opponentK);
            int dist = Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

            if (dist == 1)
            {
                if (ugokeru[k1, k2] == 1 && k2 != opponentK) return true;
            }
            if (ugokeru[k1, opponentK] == 1)
            {
                int dx = oppX - x1; int dy = oppY - y1;
                int jx = oppX + dx; int jy = oppY + dy;
                if (jx >= 0 && jx < N && jy >= 0 && jy < N)
                {
                    int jk = xy2to1(jx, jy);
                    if (ugokeru[opponentK, jk] == 1) { if (k2 == jk) return true; }
                    else goto Diag;
                }
                else goto Diag;
                return false;
            Diag:
                if (dx == 0)
                {
                    int lk = (oppX > 0) ? xy2to1(oppX - 1, oppY) : -1;
                    int rk = (oppX < N - 1) ? xy2to1(oppX + 1, oppY) : -1;
                    if (lk != -1 && k2 == lk && ugokeru[opponentK, lk] == 1) return true;
                    if (rk != -1 && k2 == rk && ugokeru[opponentK, rk] == 1) return true;
                }
                else
                {
                    int uk = (oppY > 0) ? xy2to1(oppX, oppY - 1) : -1;
                    int dk = (oppY < N - 1) ? xy2to1(oppX, oppY + 1) : -1;
                    if (uk != -1 && k2 == uk && ugokeru[opponentK, uk] == 1) return true;
                    if (dk != -1 && k2 == dk && ugokeru[opponentK, dk] == 1) return true;
                }
            }
            return false;
        }

        private bool CanPlaceWallBasic_Main(int x, int y, bool isVertical)
        {
            if (x >= N - 1 || y >= N - 1) return false;
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
            return true;
        }

        private bool TryWallPlacement_Main(int x, int y, bool isVertical, int playerIndex, out int opponentPathCost)
        {
            opponentPathCost = int.MaxValue;
            if (!CanPlaceWallBasic_Main(x, y, isVertical)) return false;

            if (isVertical)
            {
                ugokeru[xy2to1(x, y), xy2to1(x + 1, y)] = 0; ugokeru[xy2to1(x + 1, y), xy2to1(x, y)] = 0;
                ugokeru[xy2to1(x, y + 1), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x, y + 1)] = 0;
            }
            else
            {
                ugokeru[xy2to1(x, y), xy2to1(x, y + 1)] = 0; ugokeru[xy2to1(x, y + 1), xy2to1(x, y)] = 0;
                ugokeru[xy2to1(x + 1, y), xy2to1(x + 1, y + 1)] = 0; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x + 1, y)] = 0;
            }

            bool p0Path = AStarPathLength(player0[2], 0) != int.MaxValue;
            bool p1Path = AStarPathLength(player1[2], N - 1) != int.MaxValue;

            if (p0Path && p1Path)
            {
                return true;
            }
            else
            {
                RemoveWall_Main(x, y, isVertical);
                return false;
            }
        }

        private void RemoveWall_Main(int x, int y, bool isVertical)
        {
            if (isVertical)
            {
                ugokeru[xy2to1(x, y), xy2to1(x + 1, y)] = 1; ugokeru[xy2to1(x + 1, y), xy2to1(x, y)] = 1;
                ugokeru[xy2to1(x, y + 1), xy2to1(x + 1, y + 1)] = 1; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x, y + 1)] = 1;
            }
            else
            {
                ugokeru[xy2to1(x, y), xy2to1(x, y + 1)] = 1; ugokeru[xy2to1(x, y + 1), xy2to1(x, y)] = 1;
                ugokeru[xy2to1(x + 1, y), xy2to1(x + 1, y + 1)] = 1; ugokeru[xy2to1(x + 1, y + 1), xy2to1(x + 1, y)] = 1;
            }
        }

        // --- Monte Carlo Helper Classes ---
        enum ActionType { Move, VerticalWall, HorizontalWall }
        class SimAction { public ActionType Type; public int TargetK; public int WallX; public int WallY; public double Score; }
        class SimState
        {
            public int[,] Ugokeru; public int[,] Kabeyoko; public int[,] Kabetate;
            public int[] P0; public int[] P1; public int[] Walls; public int Turn;
            private Random _rng;

            public SimState(int[,] u, int[,] ky, int[,] kt, int[] p0, int[] p1, int[] walls, int turn)
            {
                Ugokeru = (int[,])u.Clone(); Kabeyoko = (int[,])ky.Clone(); Kabetate = (int[,])kt.Clone();
                P0 = (int[])p0.Clone(); P1 = (int[])p1.Clone(); Walls = (int[])walls.Clone(); Turn = turn;
                _rng = new Random(Guid.NewGuid().GetHashCode());
            }
            public SimState DeepClone() { return new SimState(Ugokeru, Kabeyoko, Kabetate, P0, P1, Walls, Turn); }
            // SimStateクラス内のメソッド
            public int PlayOut()
            {
                int moves = 0;
                while (moves < 100)
                {
                    if (P0[1] == 0) return 0;
                    if (P1[1] == 5 - 1) return 1; // N=5

                    List<SimAction> actions = GetLegalActions();
                    if (actions.Count == 0) return 1 - Turn;

                    SimAction selectedAction = null;


                    // 移動アクションだけ抽出
                    var moveActions = actions.Where(a => a.Type == ActionType.Move).ToList();

                    // 80%の確率で「ゴールに近づく手」を選ぶ (残りはランダム)
                    if (moveActions.Count > 0 && _rng.NextDouble() < 0.8)
                    {
                        int goalY = (Turn == 0) ? 0 : 5 - 1; // N=5
                        int currentY = (Turn == 0) ? P0[1] : P1[1];
                        int currentDist = Math.Abs(currentY - goalY);

                        // 今よりゴールに近づく手を探す
                        var betterMoves = moveActions.Where(a =>
                        {
                            int nextY = a.TargetK / 5; // N=5
                            return Math.Abs(nextY - goalY) < currentDist;
                        }).ToList();

                        if (betterMoves.Count > 0)
                        {
                            // 良い手の中からランダム選択
                            selectedAction = betterMoves[_rng.Next(betterMoves.Count)];
                        }
                    }

                    // 上記で決まらなかった、または壁を置く場合などはランダム
                    if (selectedAction == null)
                    {
                        selectedAction = actions[_rng.Next(actions.Count)];
                    }
                    // ===============================================

                    ApplyAction(selectedAction);
                    moves++;
                }
                return -1; // Draw
            }
            public void ApplyAction(SimAction a)
            {
                if (a.Type == ActionType.Move)
                {
                    if (Turn == 0) { P0[2] = a.TargetK; P0[0] = a.TargetK % N; P0[1] = a.TargetK / N; }
                    else { P1[2] = a.TargetK; P1[0] = a.TargetK % N; P1[1] = a.TargetK / N; }
                }
                else
                {
                    Walls[Turn]--;
                    int x = a.WallX, y = a.WallY;
                    if (a.Type == ActionType.VerticalWall)
                    {
                        Kabetate[x, y] = Turn + 1;
                        Ugokeru[xy2to1(x, y), xy2to1(x + 1, y)] = 0; Ugokeru[xy2to1(x + 1, y), xy2to1(x, y)] = 0;
                        Ugokeru[xy2to1(x, y + 1), xy2to1(x + 1, y + 1)] = 0; Ugokeru[xy2to1(x + 1, y + 1), xy2to1(x, y + 1)] = 0;
                    }
                    else
                    {
                        Kabeyoko[x, y] = Turn + 1;
                        Ugokeru[xy2to1(x, y), xy2to1(x, y + 1)] = 0; Ugokeru[xy2to1(x, y + 1), xy2to1(x, y)] = 0;
                        Ugokeru[xy2to1(x + 1, y), xy2to1(x + 1, y + 1)] = 0; Ugokeru[xy2to1(x + 1, y + 1), xy2to1(x + 1, y)] = 0;
                    }
                }
                Turn = 1 - Turn;
            }
            public List<SimAction> GetLegalActions()
            {
                var acts = new List<SimAction>();
                int mk = (Turn == 0) ? P0[2] : P1[2];
                int ok = (Turn == 0) ? P1[2] : P0[2];
                for (int k2 = 0; k2 < N * N; k2++)
                {
                    if (mk != k2 && SimCanMove(mk, k2, ok)) acts.Add(new SimAction { Type = ActionType.Move, TargetK = k2 });
                }
                if (Walls[Turn] > 0)
                {
                    for (int x = 0; x < N - 1; x++) for (int y = 0; y < N - 1; y++)
                        {
                            if (SimCanPlaceWall(x, y, true)) acts.Add(new SimAction { Type = ActionType.VerticalWall, WallX = x, WallY = y });
                            if (SimCanPlaceWall(x, y, false)) acts.Add(new SimAction { Type = ActionType.HorizontalWall, WallX = x, WallY = y });
                        }
                }
                return acts;
            }
            private bool SimCanMove(int k1, int k2, int ok)
            {
                int x1 = k1 % N, y1 = k1 / N, x2 = k2 % N, y2 = k2 / N;
                int dist = Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
                if (dist == 1 && Ugokeru[k1, k2] == 1 && k2 != ok) return true;
                if (Ugokeru[k1, ok] == 1)
                {
                    int dx = (ok % N) - x1, dy = (ok / N) - y1;
                    int jx = (ok % N) + dx, jy = (ok / N) + dy;
                    if (jx >= 0 && jx < N && jy >= 0 && jy < N && Ugokeru[ok, jx + jy * N] == 1) return k2 == (jx + jy * N);
                    // 斜めジャンプ略
                }
                return false;
            }
            private bool SimCanPlaceWall(int x, int y, bool v)
            {
                if (v) { if (Kabetate[x, y] != 0 || (y > 0 && Kabetate[x, y - 1] != 0) || (y < N - 2 && Kabetate[x, y + 1] != 0) || Kabeyoko[x, y] != 0) return false; }
                else { if (Kabeyoko[x, y] != 0 || (x > 0 && Kabeyoko[x - 1, y] != 0) || (x < N - 2 && Kabeyoko[x + 1, y] != 0) || Kabetate[x, y] != 0) return false; }
                int k1, k2, k3, k4;
                if (v) { k1 = xy2to1(x, y); k2 = xy2to1(x + 1, y); k3 = xy2to1(x, y + 1); k4 = xy2to1(x + 1, y + 1); }
                else { k1 = xy2to1(x, y); k2 = xy2to1(x, y + 1); k3 = xy2to1(x + 1, y); k4 = xy2to1(x + 1, y + 1); }
                Ugokeru[k1, k2] = 0; Ugokeru[k2, k1] = 0; Ugokeru[k3, k4] = 0; Ugokeru[k4, k3] = 0;
                bool p0 = HasPath(P0[2], 0); bool p1 = HasPath(P1[2], N - 1);
                Ugokeru[k1, k2] = 1; Ugokeru[k2, k1] = 1; Ugokeru[k3, k4] = 1; Ugokeru[k4, k3] = 1;
                return p0 && p1;
            }
            private bool HasPath(int s, int gy)
            {
                var q = new Queue<int>(); var v = new bool[N * N]; q.Enqueue(s); v[s] = true;
                while (q.Count > 0)
                {
                    int c = q.Dequeue(); if (c / N == gy) return true;
                    int cx = c % N, cy = c / N;
                    int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = cx + dx[i], ny = cy + dy[i];
                        if (nx >= 0 && nx < N && ny >= 0 && ny < N)
                        {
                            int nk = nx + ny * N;
                            if (!v[nk] && Ugokeru[c, nk] == 1) { v[nk] = true; q.Enqueue(nk); }
                        }
                    }
                }
                return false;
            }
            private int xy2to1(int x, int y) { return x + N * y; }
        }
    }
}