using System;
using System.Collections.Generic;
using UnityEngine;

// ゲームの状態を表すクラス（MonoBehaviourを継承しないPOCOクラスに変更）
public class GameState
{
    // 盤面の状態（3次元配列）
    private Piece[,,] board;
    
    // 持ち駒リスト
    private Dictionary<PlayerType, List<PieceType>> capturedPieces;
    
    // 現在の手番
    public PlayerType CurrentPlayer { get; private set; }
    
    // ゲームの進行状況
    public GameStatus Status { get; private set; }
    
    // 履歴（取り消し機能のため）
    private Stack<Move> moveHistory;
    
    // 盤面評価用のハッシュ値（AIがポジションを記憶するのに使用）
    public ulong PositionHash { get; private set; }
    
    // コンストラクタ
    public GameState(int boardWidth = 9, int boardHeight = 9, int boardDepth = 3)
    {
        // 3次元盤面の初期化
        board = new Piece[boardWidth, boardHeight, boardDepth];
        capturedPieces = new Dictionary<PlayerType, List<PieceType>>();
        capturedPieces[PlayerType.Player1] = new List<PieceType>();
        capturedPieces[PlayerType.Player2] = new List<PieceType>();
        
        moveHistory = new Stack<Move>();
        CurrentPlayer = PlayerType.Player1;
        Status = GameStatus.Playing;
        
        InitializeBoard();
        CalculatePositionHash();
    }
    
    // 盤面の初期化
    private void InitializeBoard()
    {
        // 3次元将棋の初期配置を設定
        // ここに駒の初期配置コードを実装
    }
    
    // 駒の移動が合法かチェック
    public bool IsLegalMove(Move move)
    {
        // 移動のルールチェックを実装
        return true; // 仮の実装
    }
    
    // 駒を動かす
    public bool MakeMove(Move move)
    {
        if (!IsLegalMove(move))
            return false;
            
        // 駒の移動処理
        // 駒の捕獲処理
        
        // 履歴に追加
        moveHistory.Push(move);
        
        // 手番の交代
        CurrentPlayer = CurrentPlayer == PlayerType.Player1 ? PlayerType.Player2 : PlayerType.Player1;
        
        // 盤面ハッシュ値の更新
        UpdatePositionHash(move);
        
        // チェックメイト/ステイルメイトの確認
        CheckGameStatus();
        
        return true;
    }
    
    // 合法手のリストを生成（AI用）
    public List<Move> GenerateLegalMoves()
    {
        List<Move> legalMoves = new List<Move>();
        // すべての合法手を列挙
        return legalMoves;
    }
    
    // 盤面のコピーを作成（探索用）
    public GameState Clone()
    {
        // ディープコピーを実装
        return null; // 仮の実装
    }
    
    // 盤面のハッシュ値を計算
    private void CalculatePositionHash()
    {
        // Zobristハッシュなどを使って盤面をハッシュ化
    }
    
    // ハッシュ値を更新
    private void UpdatePositionHash(Move move)
    {
        // 差分更新でハッシュ値を効率的に更新
    }
    
    // ゲーム状態の確認
    private void CheckGameStatus()
    {
        // 王が詰んでいるか、または引き分け条件を満たしているかをチェック
    }
}

// プレイヤーの種類
public enum PlayerType
{
    Player1,
    Player2
}

// ゲームの状態
public enum GameStatus
{
    Playing,
    Checkmate,
    Stalemate,
    Draw
}

// 駒の種類
public enum PieceType
{
    King,
    Rook,
    Bishop,
    Gold,
    Silver,
    Knight,
    Lance,
    Pawn,
    // 成り駒
    PromotedRook,
    PromotedBishop,
    PromotedSilver,
    PromotedKnight,
    PromotedLance,
    PromotedPawn
}

// 駒クラス
public class Piece
{
    public PieceType Type { get; set; }
    public PlayerType Owner { get; set; }
    public bool IsPromoted { get; set; }
    
    public Piece(PieceType type, PlayerType owner)
    {
        Type = type;
        Owner = owner;
        IsPromoted = false;
    }
}

// 手の表現
public class Move
{
    // 移動元座標
    public Vector3Int From { get; set; }
    
    // 移動先座標
    public Vector3Int To { get; set; }
    
    // 成り
    public bool IsPromotion { get; set; }
    
    // 打つ手かどうか
    public bool IsDrop { get; set; }
    
    // 打つ駒の種類（打つ手の場合）
    public PieceType DroppedPiece { get; set; }
    
    // 捕獲した駒
    public Piece CapturedPiece { get; set; }
}
