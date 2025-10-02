using System;
using System.Collections;
using UnityEngine;

public enum GameMode
{
    PlayerVsPlayer,
    PlayerVsAI,
    AIVsPlayer
}

public class GameController : MonoBehaviour
{
    [Header("ゲーム設定")]
    [SerializeField] private GameMode gameMode = GameMode.PlayerVsPlayer;
    [SerializeField] private float aiThinkingTime = 1.0f; // 最小AI思考時間（UX向上のため）
    
    [Header("参照")]
    [SerializeField] private BoardView boardView;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AIPlayer aiPlayer;
    
    private GameState _gameState;
    private bool _isAIThinking = false;
    private bool _isGameOver = false;
    
    // 選択状態の管理
    private Vector3Int? _selectedPosition = null;
    private Piece _selectedPiece = null;
    private bool _isSelectingDropPosition = false;
    private PieceType _selectedDropPieceType;
    
    // ゲーム開始時の初期化
    private void Start()
    {
        InitializeGame();
    }
    
    // ゲームの初期化
    public void InitializeGame()
    {
        // ゲーム状態の作成
        _gameState = new GameState();
        
        // UIとボードビューの初期化
        boardView.Initialize(_gameState);
        uiManager.Initialize(this, _gameState);
        
        // AIPlayerのセットアップ
        if (aiPlayer != null)
        {
            aiPlayer.OnMoveSelected += HandleAIMoveSelected;
        }
        
        _isGameOver = false;
        
        // 初期手番の設定
        UpdateTurnUI();
        
        // AIが先手の場合、AIの手を要求
        if (gameMode == GameMode.AIVsPlayer && _gameState.CurrentPlayer == PlayerType.Player1)
        {
            RequestAIMove();
        }
    }
    
    // 盤面上のマスが選択された時の処理
    public void OnBoardPositionSelected(Vector3Int position)
    {
        if (_isAIThinking || _isGameOver)
            return;
            
        // 現在の手番が人間プレイヤーかどうかチェック
        bool isHumanTurn = IsHumanTurn();
        if (!isHumanTurn)
            return;
            
        // 駒打ちモードの場合
        if (_isSelectingDropPosition)
        {
            TryDropPiece(position);
            return;
        }
        
        // 駒が選択されていない場合
        if (!_selectedPosition.HasValue)
        {
            TrySelectPiece(position);
        }
        // 既に駒が選択されている場合
        else
        {
            TryMovePiece(_selectedPosition.Value, position);
        }
    }
    
    // 駒打ちを試みる
    private void TryDropPiece(Vector3Int position)
    {
        // 駒を打てる場所かチェック
        if (_gameState.CanDropPiece(_selectedDropPieceType, position))
        {
            // 駒打ちの実行
            Move dropMove = new Move
            {
                IsDrop = true,
                To = position,
                DroppedPiece = _selectedDropPieceType
            };
            
            if (_gameState.MakeMove(dropMove))
            {
                // 成功した場合、ビューを更新
                boardView.UpdateBoard(_gameState);
                _isSelectingDropPosition = false;
                _selectedDropPieceType = PieceType.None;
                boardView.ClearHighlights();
                
                // 次のターンの処理
                ProcessNextTurn();
            }
        }
        else
        {
            // 無効な位置の場合、選択状態をキャンセル
            _isSelectingDropPosition = false;
            _selectedDropPieceType = PieceType.None;
            boardView.ClearHighlights();
        }
    }
    
    // 駒の選択を試みる
    private void TrySelectPiece(Vector3Int position)
    {
        Piece piece = _gameState.GetPieceAt(position);
        
        // 選択位置に自分の駒があるかチェック
        if (piece != null && piece.Owner == _gameState.CurrentPlayer)
        {
            _selectedPiece = piece;
            _selectedPosition = position;
            
            // 選択した駒の移動可能範囲をハイライト
            List<Vector3Int> legalMoves = _gameState.GetLegalMovesForPiece(position);
            boardView.HighlightPositions(legalMoves);
            boardView.HighlightSelectedPiece(position);
        }
    }
    
    // 駒の移動を試みる
    private void TryMovePiece(Vector3Int from, Vector3Int to)
    {
        // 移動が有効かチェック
        Move move = new Move
        {
            From = from,
            To = to
        };
        
        // 成り判定が必要な場合
        if (_gameState.ShouldPromote(move))
        {
            // 成るかどうかのUIを表示
            uiManager.ShowPromotionDialog(() => ExecuteMove(move, true), () => ExecuteMove(move, false));
            return;
        }
        
        // 通常の移動を実行
        ExecuteMove(move, false);
    }
    
    // 移動を実行
    private void ExecuteMove(Move move, bool promote)
    {
        move.IsPromotion = promote;
        
        if (_gameState.MakeMove(move))
        {
            // 成功した場合、ビューを更新
            boardView.UpdateBoard(_gameState);
            _selectedPosition = null;
            _selectedPiece = null;
            boardView.ClearHighlights();
            
            // 次のターンの処理
            ProcessNextTurn();
        }
        else
        {
            // 移動が無効な場合、選択状態をクリア
            _selectedPosition = null;
            _selectedPiece = null;
            boardView.ClearHighlights();
        }
    }
    
    // 持ち駒が選択された時の処理
    public void OnCapturedPieceSelected(PieceType pieceType)
    {
        if (_isAIThinking || _isGameOver || !IsHumanTurn())
            return;
            
        // すでに選択状態があればクリア
        _selectedPosition = null;
        _selectedPiece = null;
        boardView.ClearHighlights();
        
        // 駒打ちモードを設定
        _isSelectingDropPosition = true;
        _selectedDropPieceType = pieceType;
        
        // 駒を打てる場所をハイライト
        List<Vector3Int> dropPositions = _gameState.GetLegalDropPositions(pieceType);
        boardView.HighlightPositions(dropPositions);
    }
    
    // 次のターンの処理
    private void ProcessNextTurn()
    {
        // ゲーム終了判定
        if (_gameState.Status != GameStatus.Playing)
        {
            EndGame();
            return;
        }
        
        // UIの更新
        UpdateTurnUI();
        
        // AIの手番かどうかをチェック
        if (!IsHumanTurn())
        {
            RequestAIMove();
        }
    }
    
    // AIに手を要求
    private void RequestAIMove()
    {
        if (aiPlayer == null)
            return;
            
        _isAIThinking = true;
        uiManager.ShowAIThinking(true);
        
        // 最小思考時間のためのディレイ
        StartCoroutine(DelayedAIRequest());
    }
    
    // UX向上のための遅延AI要求
    private IEnumerator DelayedAIRequest()
    {
        // 最小の「思考中」表示時間を確保
        yield return new WaitForSeconds(aiThinkingTime);
        
        // AIに手を要求
        aiPlayer.RequestMove(_gameState);
    }
    
    // AIが選択した手の処理
    private void HandleAIMoveSelected(Move move)
    {
        _isAIThinking = false;
        uiManager.ShowAIThinking(false);
        
        // AIの手を適用
        _gameState.MakeMove(move);
        
        // ビューを更新
        boardView.UpdateBoard(_gameState);
        
        // 次のターンの処理
        ProcessNextTurn();
    }
    
    // ゲームの終了処理
    private void EndGame()
    {
        _isGameOver = true;
        
        // 結果に応じたUIの表示
        switch (_gameState.Status)
        {
            case GameStatus.Checkmate:
                // 前のプレイヤーが勝者（詰みを作ったプレイヤー）
                PlayerType winner = _gameState.CurrentPlayer == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;
                uiManager.ShowGameResult($"{winner}の勝ち（詰み）");
                break;
                
            case GameStatus.Stalemate:
                uiManager.ShowGameResult("引き分け（ステイルメイト）");
                break;
                
            case GameStatus.Draw:
                uiManager.ShowGameResult("引き分け");
                break;
        }
    }
    
    // ターンUIの更新
    private void UpdateTurnUI()
    {
        uiManager.UpdateTurnInfo(_gameState.CurrentPlayer);
    }
    
    // 現在の手番が人間プレイヤーかどうか
    private bool IsHumanTurn()
    {
        switch (gameMode)
        {
            case GameMode.PlayerVsPlayer:
                return true;
                
            case GameMode.PlayerVsAI:
                return _gameState.CurrentPlayer == PlayerType.Player1;
                
            case GameMode.AIVsPlayer:
                return _gameState.CurrentPlayer == PlayerType.Player2;
                
            default:
                return true;
        }
    }
    
    // ゲームモードの設定
    public void SetGameMode(GameMode mode)
    {
        gameMode = mode;
        
        // 現在進行中のゲームがある場合、リセット
        if (_gameState != null)
        {
            InitializeGame();
        }
    }
    
    // 投了
    public void Resign()
    {
        if (_isGameOver)
            return;
            
        _isGameOver = true;
        
        // 現在のプレイヤーが投了
        PlayerType winner = _gameState.CurrentPlayer == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;
        uiManager.ShowGameResult($"{winner}の勝ち（投了）");
    }
    
    // 新しいゲームの開始
    public void StartNewGame()
    {
        InitializeGame();
    }
}
