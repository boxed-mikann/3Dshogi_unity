using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Barracuda; // ONNXモデルを実行するためのBarracudaを使用

public class AIPlayer : MonoBehaviour
{
    [Header("AIモデル設定")]
    [SerializeField] private NNModel valueNetworkModel; // 価値ネットワークのONNXモデル
    [SerializeField] private NNModel policyNetworkModel; // 方策ネットワークのONNXモデル
    
    [Header("AI探索設定")]
    [SerializeField] private int maxSimulations = 800; // MCTSのシミュレーション回数
    [SerializeField] private float explorationConstant = 1.4f; // UCB1の探索定数
    [SerializeField] private float temperature = 1.0f; // ボルツマン温度パラメータ
    
    // モデルランタイム
    private Model _valueRuntimeModel;
    private Model _policyRuntimeModel;
    private IWorker _valueWorker;
    private IWorker _policyWorker;
    
    // AIの思考状態
    private bool _isThinking = false;
    
    // ゲームコントローラーへの参照
    private GameController _gameController;
    
    // コールバックアクション
    public event Action<Move> OnMoveSelected;
    
    private void Start()
    {
        _gameController = FindObjectOfType<GameController>();
        
        // ONNXモデルのロード
        InitializeNeuralNetworks();
    }
    
    private void OnDestroy()
    {
        // ワーカーの解放
        _valueWorker?.Dispose();
        _policyWorker?.Dispose();
    }
    
    private void InitializeNeuralNetworks()
    {
        if (valueNetworkModel != null && policyNetworkModel != null)
        {
            // モデルをランタイム形式に変換
            _valueRuntimeModel = ModelLoader.Load(valueNetworkModel);
            _policyRuntimeModel = ModelLoader.Load(policyNetworkModel);
            
            // 推論ワーカーを作成
            _valueWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, _valueRuntimeModel);
            _policyWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, _policyRuntimeModel);
            
            Debug.Log("ニューラルネットワークモデルが初期化されました");
        }
        else
        {
            Debug.LogError("ONNXモデルが設定されていません");
        }
    }
    
    // ゲーム状態から特徴量を抽出してテンソルに変換
    private Tensor CreateInputTensorFromGameState(GameState gameState)
    {
        // 3次元将棋の状態をニューラルネットワークの入力形式に変換
        // ここでは盤面の各マスの状態、持ち駒情報などを特徴量として抽出
        
        // 仮の実装（実際のモデルに合わせて調整が必要）
        // 入力形式例: [バッチ, チャンネル数, 深さ, 高さ, 幅]
        int batchSize = 1;
        int channels = 20; // 駒の種類や状態を表す特徴マップの数
        int depth = 3;     // 3次元将棋の深さ
        int height = 9;    // 盤面の高さ
        int width = 9;     // 盤面の幅
        
        // 入力テンソルを作成
        float[] inputData = new float[batchSize * channels * depth * height * width];
        
        // ここでゲーム状態から特徴量を抽出してinputDataに設定
        // ...
        
        // 適切な形状でテンソルを作成して返す
        return new Tensor(batchSize, height, width, channels * depth, inputData);
    }
    
    // AIに次の手を考えさせる（非同期処理）
    public void RequestMove(GameState gameState)
    {
        if (_isThinking)
            return;
            
        _isThinking = true;
        
        // UIに思考中であることを通知できる
        
        // 非同期で思考処理を開始
        StartCoroutine(ThinkAsync(gameState));
    }
    
    private IEnumerator ThinkAsync(GameState gameState)
    {
        // バックグラウンドスレッドで思考処理を実行
        Task<Move> thinkingTask = Task.Run(() => RunMCTS(gameState));
        
        // 思考が完了するまで待機
        while (!thinkingTask.IsCompleted)
        {
            yield return null;
        }
        
        // 思考結果を取得
        Move selectedMove = thinkingTask.Result;
        
        // 思考状態を解除
        _isThinking = false;
        
        // 選択された手をコールバックで通知
        OnMoveSelected?.Invoke(selectedMove);
    }
    
    // モンテカルロ木探索を実行
    private Move RunMCTS(GameState rootState)
    {
        MCTSNode rootNode = new MCTSNode(rootState, null, null);
        
        // 指定されたシミュレーション回数だけ探索を実行
        for (int i = 0; i < maxSimulations; i++)
        {
            // 1. 選択: UCB1に基づいて有望なノードを選択
            MCTSNode selectedNode = SelectNode(rootNode);
            
            // 2. 展開: 新しいノードを追加
            MCTSNode expandedNode = ExpandNode(selectedNode);
            
            // 3. 評価: ニューラルネットワークで盤面を評価
            float value = EvaluatePosition(expandedNode.GameState);
            
            // 4. バックプロパゲーション: 評価値を遡って更新
            BackPropagate(expandedNode, value);
        }
        
        // 最も訪問回数の多い子ノードに対応する手を選択
        return SelectBestMove(rootNode);
    }
    
    // UCB1に基づいてノードを選択
    private MCTSNode SelectNode(MCTSNode node)
    {
        // 葉ノードに到達するまで、UCB1スコアが最大のノードを選択
        while (node.IsFullyExpanded && !node.IsTerminal)
        {
            node = node.SelectBestChild(explorationConstant);
        }
        return node;
    }
    
    // ノードを展開
    private MCTSNode ExpandNode(MCTSNode node)
    {
        // すでに終端状態の場合はそのまま返す
        if (node.IsTerminal)
            return node;
            
        // まだ展開されていない合法手があれば、ランダムに1つ選んで展開
        List<Move> untriedMoves = node.GetUntriedMoves();
        if (untriedMoves.Count > 0)
        {
            Move move = untriedMoves[UnityEngine.Random.Range(0, untriedMoves.Count)];
            GameState nextState = node.GameState.Clone();
            nextState.MakeMove(move);
            
            // 新しいノードを作成して返す
            return node.AddChild(move, nextState);
        }
        
        return node;
    }
    
    // ニューラルネットワークで盤面を評価
    private float EvaluatePosition(GameState gameState)
    {
        using (Tensor inputTensor = CreateInputTensorFromGameState(gameState))
        {
            // 価値ネットワークで評価値を取得
            _valueWorker.Execute(inputTensor);
            Tensor outputTensor = _valueWorker.PeekOutput();
            
            // 出力から評価値（-1から1の範囲）を取得
            float value = outputTensor[0];
            
            return value;
        }
    }
    
    // 方策ネットワークで各手の確率を計算
    private Dictionary<Move, float> GetActionProbabilities(GameState gameState)
    {
        Dictionary<Move, float> moveProbs = new Dictionary<Move, float>();
        
        using (Tensor inputTensor = CreateInputTensorFromGameState(gameState))
        {
            // 方策ネットワークで各手の確率を取得
            _policyWorker.Execute(inputTensor);
            Tensor outputTensor = _policyWorker.PeekOutput();
            
            // 出力から合法手の確率を取得（実装はモデルの出力形式に依存）
            List<Move> legalMoves = gameState.GenerateLegalMoves();
            
            // ここで出力テンソルから各合法手の確率を抽出
            foreach (Move move in legalMoves)
            {
                // 移動元と移動先の座標からインデックスを計算
                // （実際のモデル出力形式に合わせて調整が必要）
                int index = EncodeMove(move);
                moveProbs[move] = outputTensor[index];
            }
        }
        
        return moveProbs;
    }
    
    // 手をエンコードしてテンソルのインデックスに変換
    private int EncodeMove(Move move)
    {
        // 実際のモデル出力形式に合わせて実装
        // 仮の実装
        return 0;
    }
    
    // 評価値を遡って更新
    private void BackPropagate(MCTSNode node, float value)
    {
        // ルートに到達するまで遡る
        while (node != null)
        {
            node.UpdateStats(value);
            // 相手の立場からの評価値に変換
            value = -value;
            node = node.Parent;
        }
    }
    
    // 最良の手を選択
    private Move SelectBestMove(MCTSNode rootNode)
    {
        MCTSNode bestChild = null;
        int bestVisits = -1;
        
        foreach (MCTSNode child in rootNode.Children)
        {
            if (child.VisitCount > bestVisits)
            {
                bestVisits = child.VisitCount;
                bestChild = child;
            }
        }
        
        return bestChild?.MoveFromParent;
    }
}

// MCTSのノードクラス
public class MCTSNode
{
    public GameState GameState { get; private set; }
    public MCTSNode Parent { get; private set; }
    public List<MCTSNode> Children { get; private set; }
    public Move MoveFromParent { get; private set; }
    public int VisitCount { get; private set; }
    public float TotalValue { get; private set; }
    public Dictionary<Move, float> ActionPriors { get; private set; }
    public List<Move> UntriedMoves { get; private set; }
    
    public bool IsTerminal => GameState.Status != GameStatus.Playing;
    public bool IsFullyExpanded => UntriedMoves.Count == 0;
    
    public MCTSNode(GameState gameState, MCTSNode parent, Move moveFromParent, Dictionary<Move, float> actionPriors = null)
    {
        GameState = gameState;
        Parent = parent;
        MoveFromParent = moveFromParent;
        Children = new List<MCTSNode>();
        VisitCount = 0;
        TotalValue = 0f;
        ActionPriors = actionPriors ?? new Dictionary<Move, float>();
        
        // 未試行の合法手を取得
        UntriedMoves = gameState.GenerateLegalMoves();
    }
    
    // UCB1スコアを計算
    public float GetUCB1(float explorationConstant)
    {
        if (VisitCount == 0)
            return float.MaxValue;
            
        float exploitation = TotalValue / VisitCount;
        float exploration = explorationConstant * Mathf.Sqrt(Mathf.Log(Parent.VisitCount) / VisitCount);
        
        float prior = 0f;
        if (ActionPriors.TryGetValue(MoveFromParent, out float p))
        {
            prior = p;
        }
        
        // AlphaZero風のPUCTスコア
        return exploitation + prior * exploration;
    }
    
    // UCB1スコアが最も高い子ノードを選択
    public MCTSNode SelectBestChild(float explorationConstant)
    {
        MCTSNode bestChild = null;
        float bestScore = float.MinValue;
        
        foreach (MCTSNode child in Children)
        {
            float score = child.GetUCB1(explorationConstant);
            if (score > bestScore)
            {
                bestScore = score;
                bestChild = child;
            }
        }
        
        return bestChild;
    }
    
    // 子ノードを追加
    public MCTSNode AddChild(Move move, GameState nextState)
    {
        MCTSNode child = new MCTSNode(nextState, this, move, ActionPriors);
        Children.Add(child);
        UntriedMoves.Remove(move);
        return child;
    }
    
    // 統計情報を更新
    public void UpdateStats(float value)
    {
        VisitCount++;
        TotalValue += value;
    }
    
    // 未試行の合法手を取得
    public List<Move> GetUntriedMoves()
    {
        return new List<Move>(UntriedMoves);
    }
}
