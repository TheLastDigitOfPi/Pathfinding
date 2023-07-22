using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;
using DG.Tweening;

public class TilePlacer : MonoBehaviour
{
    Vector2Int startTilePos;
    Vector2Int endTilePos;

    [Header("Tile Placing")]

    [SerializeField] CellTypes currentHeldTile = CellTypes.None;
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
    public void ChangeTile(CellTypes tileToChangeTo)
    {
        if (currentHeldTile == tileToChangeTo)
            return;
        currentHeldTile = tileToChangeTo;
    }


    public void WhiteTilePlacer()
    {
        if (currentSelector == WhiteTileSelector)
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
        currentSelector = WhiteTileSelector;
        currentSelector.DORotate(new Vector3(0, 0, 360), _rotateSpeed, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        ChangeTile(CellTypes.Floor);
    }
    public void YellowTilePlacer()
    {
        if (currentSelector == GoalTileSelector)
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
        currentSelector = GoalTileSelector;
        currentSelector.DORotate(new Vector3(0, 0, 360), _rotateSpeed, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        ChangeTile(CellTypes.End);
    }
    public void GreenTilePlacer()
    {
        if (currentSelector == StartTileSelector)
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
        currentSelector = StartTileSelector;
        currentSelector.DORotate(new Vector3(0, 0, 360), _rotateSpeed, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        ChangeTile(CellTypes.Start);
    }
    public void BlackTilePlacer()
    {
        if (currentSelector == BlackTileSelector)
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
        currentSelector = BlackTileSelector;
        currentSelector.DORotate(new Vector3(0, 0, 360), _rotateSpeed, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        ChangeTile(CellTypes.Wall);

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

        if (Input.GetMouseButton(1))
        {
            var tile = MazeHandler.Instance.MazeMap.GetTile(gridPos);
            if (tile == null)
                return;
            MazeHandler.Instance.PlaceTile(CellTypes.Empty, gridPos);
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
    Empty,
    Searching,
    FoundPath,
    None
}