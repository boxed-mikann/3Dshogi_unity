using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("盤面設定")]
    [SerializeField] private int boardWidth = 9;
    [SerializeField] private int boardHeight = 9;
    [SerializeField] private int boardDepth = 3;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float levelHeight = 2.0f; // 3D盤面の各レベルの高さ
    
    [Header("駒のプレハブ")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject[] piecePrefabs; // 駒のプレハブ配列（PieceTypeのインデックスに対応）
    
    [Header("ハイライト")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material selectedMaterial;
    
    // 盤面上の駒のGameObjectを保持
    private GameObject[,,] _pieceObjects;
    private GameObject[,,] _tileObjects;
    private List<GameObject> _highlightObjects = new List<GameObject>();
    private Dictionary<Vector3Int, GameObject> _highlightMap = new Dictionary<Vector3Int, GameObject>();
    
    // ゲームコントローラーへの参照
    private GameController _gameController;
    
    // 初期化
    public void Initialize(GameState gameState)
    {
        _gameController = FindObjectOfType<GameController>();
        
        // 盤面のクリア
        ClearBoard();
        
        // 盤面の作成
        CreateBoard();
        
        // 初期状態の反映
        UpdateBoard(gameState);
    }
    
    // 盤面のクリア
    private void ClearBoard()
    {
        // 既存の駒を削除
        if (_pieceObjects != null)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int z = 0; z < boardDepth; z++)
                    {
                        if (_pieceObjects[x, y, z] != null)
                        {
                            Destroy(_pieceObjects[x, y, z]);
                        }
                    }
                }
            }
        }
        
        // 既存のタイルを削除
        if (_tileObjects != null)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int z = 0; z < boardDepth; z++)
                    {
                        if (_tileObjects[x, y, z] != null)
                        {
                            Destroy(_tileObjects[x, y, z]);
                        }
                    }
                }
            }
        }
        
        // ハイライトをクリア
        ClearHighlights();
        
        // 配列の初期化
        _pieceObjects = new GameObject[boardWidth, boardHeight, boardDepth];
        _tileObjects = new GameObject[boardWidth, boardHeight, boardDepth];
    }
    
    // 3D盤面の作成
    private void CreateBoard()
    {
        for (int z = 0; z < boardDepth; z++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    // タイルの生成
                    Vector3 position = GetWorldPosition(new Vector3Int(x, y, z));
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    tile.name = $"Tile_{x}_{y}_{z}";
                    
                    // 市松模様のパターン
                    bool isBlack = (x + y + z) % 2 == 0;
                    Renderer renderer = tile.GetComponent<Renderer>();
                    renderer.material.color = isBlack ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
                    
                    // クリック検出用にコンポーネントをアタッチ
                    TileController tileController = tile.AddComponent<TileController>();
                    tileController.Initialize(new Vector3Int(x, y, z), this);
                    
                    _tileObjects[x, y, z] = tile;
                }
            }
        }
    }
    
    // ゲーム状態に基づいて盤面を更新
    public void UpdateBoard(GameState gameState)
    {
        // 既存の駒をクリア
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                for (int z = 0; z < boardDepth; z++)
                {
                    if (_pieceObjects[x, y, z] != null)
                    {
                        Destroy(_pieceObjects[x, y, z]);
                        _pieceObjects[x, y, z] = null;
                    }
                }
            }
        }
        
        // 駒の配置
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                for (int z = 0; z < boardDepth; z++)
                {
                    Vector3Int position = new Vector3Int(x, y, z);
                    Piece piece = gameState.GetPieceAt(position);
                    
                    if (piece != null)
                    {
                        SpawnPiece(piece, position);
                    }
                }
            }
        }
        
        // 持ち駒の表示更新は別途UIManagerで処理
    }
    
    // 駒の生成
    private void SpawnPiece(Piece piece, Vector3Int boardPosition)
    {
        int pieceIndex = (int)piece.Type;
        if (pieceIndex < 0 || pieceIndex >= piecePrefabs.Length)
        {
            Debug.LogError($"Invalid piece type: {piece.Type}");
            return;
        }
        
        Vector3 worldPosition = GetWorldPosition(boardPosition);
        GameObject piecePrefab = piecePrefabs[pieceIndex];
        
        GameObject pieceObject = Instantiate(piecePrefab, worldPosition, Quaternion.identity, transform);
        pieceObject.name = $"{piece.Type}_{boardPosition.x}_{boardPosition.y}_{boardPosition.z}";
        
        // プレイヤーに応じて回転
        if (piece.Owner == PlayerType.Player2)
        {
            pieceObject.transform.Rotate(Vector3.up, 180f);
        }
        
        // 成り駒の場合は見た目を変更
        if (piece.IsPromoted)
        {
            // 成り駒の見た目変更（マテリアル変更など）
            Renderer renderer = pieceObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red; // 例として赤色に変更
            }
        }
        
        _pieceObjects[boardPosition.x, boardPosition.y, boardPosition.z] = pieceObject;
    }
    
    // 盤面座標をワールド座標に変換
    public Vector3 GetWorldPosition(Vector3Int boardPosition)
    {
        float x = (boardPosition.x - boardWidth / 2f + 0.5f) * tileSize;
        float y = boardPosition.z * levelHeight; // Z軸を高さとして使用
        float z = (boardPosition.y - boardHeight / 2f + 0.5f) * tileSize;
        
        return new Vector3(x, y, z);
    }
    
    // ワールド座標を盤面座標に変換
    public Vector3Int GetBoardPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / tileSize + boardWidth / 2f - 0.5f);
        int z = Mathf.RoundToInt(worldPosition.y / levelHeight);
        int y = Mathf.RoundToInt(worldPosition.z / tileSize + boardHeight / 2f - 0.5f);
        
        return new Vector3Int(x, z, y);
    }
    
    // タイルがクリックされた時の処理
    public void OnTileClicked(Vector3Int boardPosition)
    {
        _gameController.OnBoardPositionSelected(boardPosition);
    }
    
    // 指定された位置をハイライト
    public void HighlightPositions(List<Vector3Int> positions)
    {
        ClearHighlights();
        
        foreach (Vector3Int pos in positions)
        {
            if (pos.x >= 0 && pos.x < boardWidth && 
                pos.y >= 0 && pos.y < boardHeight && 
                pos.z >= 0 && pos.z < boardDepth)
            {
                Vector3 worldPos = GetWorldPosition(pos);
                
                // ハイライトオブジェクトの生成
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highlight.transform.position = worldPos;
                highlight.transform.localScale = new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f);
                highlight.transform.parent = transform;
                
                // マテリアルの設定
                Renderer renderer = highlight.GetComponent<Renderer>();
                renderer.material = highlightMaterial;
                
                // コライダーは不要
                Destroy(highlight.GetComponent<Collider>());
                
                // リストに追加
                _highlightObjects.Add(highlight);
                _highlightMap[pos] = highlight;
            }
        }
    }
    
    // 選択された駒をハイライト
    public void HighlightSelectedPiece(Vector3Int position)
    {
        if (position.x >= 0 && position.x < boardWidth && 
            position.y >= 0 && position.y < boardHeight && 
            position.z >= 0 && position.z < boardDepth)
        {
            Vector3 worldPos = GetWorldPosition(position);
            
            // 選択ハイライトオブジェクトの生成
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.transform.position = worldPos;
            highlight.transform.localScale = new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f);
            highlight.transform.parent = transform;
            
            // マテリアルの設定
            Renderer renderer = highlight.GetComponent<Renderer>();
            renderer.material = selectedMaterial;
            
            // コライダーは不要
            Destroy(highlight.GetComponent<Collider>());
            
            // リストに追加
            _highlightObjects.Add(highlight);
            _highlightMap[position] = highlight;
        }
    }
    
    // ハイライトをクリア
    public void ClearHighlights()
    {
        foreach (GameObject highlight in _highlightObjects)
        {
            Destroy(highlight);
        }
        
        _highlightObjects.Clear();
        _highlightMap.Clear();
    }
}

// タイルのクリック検出用コンポーネント
public class TileController : MonoBehaviour
{
    private Vector3Int _boardPosition;
    private BoardView _boardView;
    
    public void Initialize(Vector3Int boardPosition, BoardView boardView)
    {
        _boardPosition = boardPosition;
        _boardView = boardView;
    }
    
    private void OnMouseDown()
    {
        _boardView.OnTileClicked(_boardPosition);
    }
}
