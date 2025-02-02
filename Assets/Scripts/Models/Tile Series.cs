using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using System.Linq;


public class TileSeries
{
    public const int tileSize = 125;

    public uint ID { get; private set; }
    public int Count => Tiles.Count;
    public List<Tile> Tiles { get; private set; }

    public ReactiveProperty<Texture2D> Texture2D { get; private set; }
    private IReadOnlyReactiveProperty<Texture2D> _Texture2D { get; set; }
    public IReadOnlyReactiveProperty<float> AdjustedOffset { get; private set; }
    public IReadOnlyReactiveProperty<float> ClampedOffset { get; private set; }
    public IReactiveProperty<float> Offset { get; private set; }


    public IReactiveProperty<HoverInfo> IsHover { get; set; }
    public bool IsRow { get; private set; }

    public TileSeries(List<Tile> tiles, uint id, bool isRow = false)
    {
        this.Tiles = tiles;
        this.IsRow = isRow;
        this.ID = id;
        //Operation Order
        //Grab Tile Colors
        //Combine them all into an array of colors
        //Only Continue the Sequence if doMove is true
        //Generate a New Texture2D
        //Make this a ReactiveProperty
        this._Texture2D = this.Tiles
            .Select(x => x.CurrentTileColor)
            .CombineLatest()
            .DistinctUntilChanged()
            .Select(x => MakeTexture2d(x))
            .ToReactiveProperty();

        this.Texture2D = new ReactiveProperty<Texture2D>(MakeTexture2d(this.Tiles.Select(x => x.CurrentTileColor.Value).ToList()));

        this._Texture2D
            .DistinctUntilChanged()
            .Subscribe(x =>
        {
            Texture2D.SetValueAndForceNotify(x);
        });

        this.Offset = new ReactiveProperty<float>(0);

        this.ClampedOffset = Offset.Select(x => Mathf.Clamp(x, -tileSize * Tiles.Count, tileSize * Tiles.Count)).ToReactiveProperty();

        this.AdjustedOffset = ClampedOffset
            .Select(x => x + tileSize * Tiles.Count)
            .ToReactiveProperty();

        this.IsHover = new ReactiveProperty<HoverInfo>(new HoverInfo(null, false));

        SetupTiles();
    }

    public void ChangeOffset(float val)
    {
        Offset.Value += val;
    }

    public void ResetOffset()
    {
        Offset.Value = 0;
    }
    public void MoveTiles(int positions)
    {
        SmartColor[] buffer = new SmartColor[Tiles.Count];
        for (int i = 0; i < Tiles.Count; i++)
        {
            var Tile = Tiles[mod(i + positions, Tiles.Count)];
            buffer[i] = Tile.CurrentTileColor.Value;
        }

        for (int j = 0; j < Tiles.Count; j++)
        {
            //if (j == Tiles.Count - 1) textureBufferBool = true;
            var Tile = Tiles[j];
            Tile.CurrentTileColor.Value = buffer[j];
        }
    }
    private int mod(int x, int mod)
    {
        int r = x % mod;
        return (r < 0) ? r + mod : r;
    }
    private void SetupTiles()
    {
        for (int i = 0; i < Tiles.Count; i++)
        {
            var tile = Tiles[i];
            if (IsRow)
            {
                tile.AddRow(this, i);
            }
            else
            {
                tile.AddColumn(this, i);
            }
        }
    }
    public Texture2D MakeTexture2d(IList<SmartColor> colors)
    {
        if (IsRow)
        {
            Texture2D returnTexture = new Texture2D(colors.Count * 3, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            var data = returnTexture.GetRawTextureData<Color32>();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    data[colors.Count * j + i] = colors[i].GetColor();
                }
            }

            returnTexture.Apply();
            TextureScaler.scale(returnTexture, tileSize * colors.Count * 3, tileSize, FilterMode.Point);

            return returnTexture;
        }
        else
        {
            Texture2D returnTexture = new Texture2D(1, colors.Count * 3, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            var data = returnTexture.GetRawTextureData<Color32>();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    data[colors.Count * j + i] = colors[colors.Count - i - 1].GetColor();
                }
            }

            returnTexture.Apply();
            TextureScaler.scale(returnTexture, tileSize, tileSize * colors.Count * 3, FilterMode.Point);

            return returnTexture;
        }
    }
}


public class HoverInfo
{
    public Tile selectedTile { get; set; }
    public bool isHovered { get; set; }

    public HoverInfo(Tile tile, bool isHovered)
    {
        this.selectedTile = tile;
        this.isHovered = isHovered;
    }
}