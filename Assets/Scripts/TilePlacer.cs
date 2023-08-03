using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

public class TilePlacer : MonoBehaviour
{
    Vector2Int startTilePos;
    Vector2Int endTilePos;

    [Header("Tile Placing")]

    [SerializeField] CellTypes currentHeldTile = CellTypes.NotGenerated;
    [SerializeField] Transform BlackTileSelector;
    [SerializeField] Transform WhiteTileSelector;
    [SerializeField] Transform GoalTileSelector;
    [SerializeField] Transform StartTileSelector;
    [SerializeField] float _rotateSpeed = 1f;
    Transform currentSelector;

    [Header("Highlighting")]
    [SerializeField] Tilemap HighlightTileMap;
    [SerializeField] TileBase highlightTile;
    [SerializeField] TileBase blackHighlightTile;
    Vector3Int _noHighlightedCell = Vector3Int.one * 999;
    [SerializeField] Vector3Int currentHighlightedTile = Vector3Int.one * 999;

    Transform CellTypeToTransform(CellTypes type)
    {
        switch (type)
        {
            case CellTypes.Start:
                return StartTileSelector;
            case CellTypes.End:
                return GoalTileSelector;
            case CellTypes.Wall:
                return BlackTileSelector;
            case CellTypes.Floor:
                return WhiteTileSelector;
            case CellTypes.NotGenerated:
                break;
            case CellTypes.Searching:
                break;
            case CellTypes.FoundPath:
                break;
            case CellTypes.None:
                break;
            default:
                break;
        }
        return null;
    }

    /// <summary>
    /// Set the tile placer to the desired cell type, will update visuals if visual available for type
    /// </summary>
    /// <param name="cellType"></param>
    void SetTilePlacer(CellTypes cellType)
    {
        var newSelector = CellTypeToTransform(cellType);
        if (newSelector == null)
        {
            if (currentSelector != null)
            {
                currentSelector.DOKill();
                currentSelector.eulerAngles = new Vector3(0, 0, 0);
            }
            currentSelector = null;
            currentHeldTile = cellType;
            return;
        }
        if (currentSelector == newSelector)
        {
            currentSelector.DOKill();
            currentSelector.eulerAngles = new Vector3(0, 0, 0);
            currentSelector = null;
            currentHeldTile = CellTypes.None;
            return;
        }
        if (currentSelector != null)
        {
            currentSelector.DOKill();
            currentSelector.eulerAngles = new Vector3(0, 0, 0);
        }
        currentSelector = newSelector;
        currentSelector.DORotate(new Vector3(0, 0, 360), _rotateSpeed, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        currentHeldTile = cellType;
    }

    public void WhiteTilePlacer()
    {
        SetTilePlacer(CellTypes.Floor);
    }
    public void YellowTilePlacer()
    {
        SetTilePlacer(CellTypes.End);
    }
    public void GreenTilePlacer()
    {
        SetTilePlacer(CellTypes.Start);
    }
    public void BlackTilePlacer()
    {
        SetTilePlacer(CellTypes.Wall);
    }
    private void Update()
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var gridPos = MazeHandler.Instance.MazeMap.WorldToCell(pos);
        HighlightMouseTile();
        void HighlightMouseTile()
        {
            //If on same tile, return
            if (gridPos.Equals(currentHighlightedTile))
                return;

            //Unhighlight last tile
            if (!currentHighlightedTile.Equals(_noHighlightedCell))
                HighlightTileMap.SetTile(currentHighlightedTile, null);

            //Highlight next tile if on map
            var mazeTile = MazeHandler.Instance.MazeMap.GetTile(gridPos);
            if (mazeTile == null)
                return;
            currentHighlightedTile = gridPos;
            HighlightTileMap.SetTile(currentHighlightedTile, mazeTile == MazeHandler.Instance.WhiteTile ? blackHighlightTile : highlightTile);
        }

        if (Input.GetMouseButtonDown(2))
        {
            var tile = MazeHandler.Instance.MazeMap.GetTile(gridPos);
            if (tile == null)
                return;
            SetTilePlacer(MazeHandler.Instance.TileBaseToEnum(tile));
        }
        if (Input.GetMouseButton(1))
        {
            var tile = MazeHandler.Instance.MazeMap.GetTile(gridPos);
            if (tile == null)
                return;
            MazeHandler.Instance.PlaceTile(CellTypes.NotGenerated, gridPos);
            return;
        }
        if (Input.GetMouseButton(0))
        {
            var tile = MazeHandler.Instance.MazeMap.GetTile(gridPos);
            if (tile == null)
                return;
            if (currentHeldTile == CellTypes.None)
                return;
            MazeHandler.Instance.PlaceTile(currentHeldTile, gridPos);
        }
    }


}

[Serializable]
public enum CellTypes
{
    Start,
    End,
    Wall,
    Floor,
    NotGenerated,
    Searching,
    FoundPath,
    None
}