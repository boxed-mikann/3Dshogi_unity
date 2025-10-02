using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TMP_Text turnInfoText;
    [SerializeField] private GameObject aiThinkingPanel;
    [SerializeField] private GameObject gameResultPanel;
    [SerializeField] private TMP_Text gameResultText;
    [SerializeField] private GameObject promotionDialog;
    
    [Header("持ち駒UI")]
    [SerializeField] private Transform player1HandPanel;
    [SerializeField] private Transform player2HandPanel;
    [SerializeField] private GameObject handPiecePrefab;
    
    [Header("ゲームモード設定")]
    [SerializeField] private Button playerVsPlayerButton;
    [SerializeField] private Button playerVsAIButton;
    [SerializeField] private Button aiVsPlayerButton;
    
    [Header("ゲーム操作ボタン")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button resignButton;
    
    // ダイアログのコールバック
    private Action _onPromoteConfirm;
    private Action _onPromoteDeny;
    
    // 参照
    private GameController _gameController;
    private GameState _gameState;
    
    // 持ち駒ボタンの辞書
    private Dictionary<PieceType, Button> _player1HandButtons = new Dictionary<PieceType, Button>();
    private Dictionary<PieceType, Button> _player2HandButtons = new Dictionary<PieceType, Button>();
    
    // 初期化
    public void Initialize(GameController controller, GameState gameState)
    {
        _gameController = controller;
        _gameState = gameState;
        
        // ボタンのイベント登録
        SetupButtons();
        
        // 初期UI状態の設定
        gameResultPanel.SetActive(false);
        promotionDialog.SetActive(false);
        aiThinkingPanel.SetActive(false);
        
        // 持ち駒UIの初期化
        ClearHandPanels();
        UpdateCapturedPiecesUI();
    }
    
    // ボタンのセットアップ
    private void SetupButtons()
    {
        // ゲームモードボタン
        if (playerVsPlayerButton != null)
            playerVsPlayerButton.onClick.AddListener(() => _gameController.SetGameMode(GameMode.PlayerVsPlayer));
            
        if (playerVsAIButton != null)
            playerVsAIButton.onClick.AddListener(() => _gameController.SetGameMode(GameMode.PlayerVsAI));
            
        if (aiVsPlayerButton != null)
            aiVsPlayerButton.onClick.AddListener(() => _gameController.SetGameMode(GameMode.AIVsPlayer));
            
        // ゲーム操作ボタン
        if (newGameButton != null)
            newGameButton.onClick.AddListener(() => _gameController.StartNewGame());
            
        if (resignButton != null)
            resignButton.onClick.AddListener(() => _gameController.Resign());
            
        // 成り確認ダイアログのボタン
        Transform confirmButton = promotionDialog.transform.Find("ConfirmButton");
        Transform denyButton = promotionDialog.transform.Find("DenyButton");
        
        if (confirmButton != null)
            confirmButton.GetComponent<Button>().onClick.AddListener(() => OnPromotionResponse(true));
            
        if (denyButton != null)
            denyButton.GetComponent<Button>().onClick.AddListener(() => OnPromotionResponse(false));
            
        // ゲーム結果パネルの「OK」ボタン
        Transform okButton = gameResultPanel.transform.Find("OkButton");
        if (okButton != null)
            okButton.GetComponent<Button>().onClick.AddListener(() => gameResultPanel.SetActive(false));
    }
    
    // 手番情報の更新
    public void UpdateTurnInfo(PlayerType currentPlayer)
    {
        string playerName = currentPlayer == PlayerType.Player1 ? "先手" : "後手";
        turnInfoText.text = $"手番: {playerName}";
    }
    
    // 持ち駒UIのクリア
    private void ClearHandPanels()
    {
        foreach (Transform child in player1HandPanel)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in player2HandPanel)
        {
            Destroy(child.gameObject);
        }
        
        _player1HandButtons.Clear();
        _player2HandButtons.Clear();
    }
    
    // 持ち駒UIの更新
    public void UpdateCapturedPiecesUI()
    {
        // 既存のUIをクリア
        ClearHandPanels();
        
        // 先手の持ち駒を表示
        Dictionary<PieceType, int> player1Hand = _gameState.GetCapturedPieces(PlayerType.Player1);
        foreach (var pair in player1Hand)
        {
            CreateHandPieceButton(pair.Key, pair.Value, PlayerType.Player1);
        }
        
        // 後手の持ち駒を表示
        Dictionary<PieceType, int> player2Hand = _gameState.GetCapturedPieces(PlayerType.Player2);
        foreach (var pair in player2Hand)
        {
            CreateHandPieceButton(pair.Key, pair.Value, PlayerType.Player2);
        }
    }
    
    // 持ち駒ボタンの作成
    private void CreateHandPieceButton(PieceType pieceType, int count, PlayerType owner)
    {
        Transform parentPanel = owner == PlayerType.Player1 ? player1HandPanel : player2HandPanel;
        
        GameObject buttonObj = Instantiate(handPiecePrefab, parentPanel);
        Button button = buttonObj.GetComponent<Button>();
        
        // ボタンテキストの設定（駒の種類と個数）
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = $"{GetPieceDisplayName(pieceType)} x{count}";
        }
        
        // クリックイベントの設定
        button.onClick.AddListener(() => OnCapturedPieceClicked(pieceType, owner));
        
        // 辞書に保存
        if (owner == PlayerType.Player1)
        {
            _player1HandButtons[pieceType] = button;
        }
        else
        {
            _player2HandButtons[pieceType] = button;
        }
    }
    
    // 駒の表示名を取得
    private string GetPieceDisplayName(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.King: return "王";
            case PieceType.Rook: return "飛";
            case PieceType.Bishop: return "角";
            case PieceType.Gold: return "金";
            case PieceType.Silver: return "銀";
            case PieceType.Knight: return "桂";
            case PieceType.Lance: return "香";
            case PieceType.Pawn: return "歩";
            case PieceType.PromotedRook: return "龍";
            case PieceType.PromotedBishop: return "馬";
            case PieceType.PromotedSilver: return "成銀";
            case PieceType.PromotedKnight: return "成桂";
            case PieceType.PromotedLance: return "成香";
            case PieceType.PromotedPawn: return "と";
            default: return "駒";
        }
    }
    
    // 持ち駒がクリックされた時の処理
    private void OnCapturedPieceClicked(PieceType pieceType, PlayerType owner)
    {
        // 自分の持ち駒のみ使用可能
        if (owner == _gameState.CurrentPlayer)
        {
            _gameController.OnCapturedPieceSelected(pieceType);
        }
    }
    
    // AI思考中の表示
    public void ShowAIThinking(bool show)
    {
        aiThinkingPanel.SetActive(show);
    }
    
    // ゲーム結果の表示
    public void ShowGameResult(string resultText)
    {
        gameResultText.text = resultText;
        gameResultPanel.SetActive(true);
    }
    
    // 成り確認ダイアログの表示
    public void ShowPromotionDialog(Action onConfirm, Action onDeny)
    {
        _onPromoteConfirm = onConfirm;
        _onPromoteDeny = onDeny;
        promotionDialog.SetActive(true);
    }
    
    // 成り確認ダイアログの応答処理
    private void OnPromotionResponse(bool promote)
    {
        promotionDialog.SetActive(false);
        
        if (promote && _onPromoteConfirm != null)
        {
            _onPromoteConfirm.Invoke();
        }
        else if (!promote && _onPromoteDeny != null)
        {
            _onPromoteDeny.Invoke();
        }
    }
}
