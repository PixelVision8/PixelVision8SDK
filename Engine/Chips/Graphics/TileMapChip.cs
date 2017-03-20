﻿//  
// Copyright (c) Jesse Freeman. All rights reserved.  
// 
// Licensed under the Microsoft Public License (MS-PL) License. 
// See LICENSE file in the project root for full license information. 
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman - @JesseFreeman
// Christer Kaitila - @McFunkypants
// Pedro Medeiros - @saint11
// Shawn Rakowski - @shwany
// 

using System;
using PixelVisionSDK.Utils;

namespace PixelVisionSDK.Chips
{

    /// <summary>
    ///     The tile map chip represents a grid of sprites used to populate the background
    ///     layer of the game. These sprites are fixed and laid out in column and row
    ///     positions making it easier to create grids of tiles. The TileMapChip also
    ///     manages flag values per tile for use in collision detection. Finally, the TileMapChip
    ///     also stores a color offset per tile to simulate palette shifting.
    /// </summary>
    public class TileMapChip : AbstractChip, ILayer
    {

        protected int _columns;
        protected int _rows;
        protected int _scrollX;
        protected int _scrollY;
        protected SpriteChip _spriteChip;
        protected int _totalLayers = -1;

        protected TextureData[] layers;
        protected int offscreenPadding = 0;
        protected int tmpIndex;
        private int[] tmpPaletteIDs = new int[0];
        protected int[] tmpPixelData = new int[8 * 8];
        private int[] tmpSpriteIDs = new int[0];
        private TextureData tmpTextureData = new TextureData(0, 0, false);

        private readonly TextureData tmpViewportData = new TextureData(0, 0, false);
        protected int tmpX;
        protected int tmpY;

        /// <summary>
        ///     Total number of collision flags the chip will support.
        ///     The default value is 16.
        /// </summary>
        public int totalFlags = 16;

        protected SpriteChip spriteChip
        {
            get
            {
                if (_spriteChip == null)
                    _spriteChip = engine.spriteChip;

                return _spriteChip;
            }
        }

        public int tileWidth
        {
            get { return spriteChip == null ? 8 : engine.spriteChip.width; }
        }

        public int tileHeight
        {
            get { return spriteChip == null ? 8 : engine.spriteChip.height; }
        }

        public int realWidth
        {
            get { return tileWidth * columns; }
        }

        public int realHeight
        {
            get { return tileHeight * rows; }
        }

        /// <summary>
        ///     Returns the total number of data layers stored in the Tilemap. It uses the Layer enum and
        ///     caches the value the first time it is called.
        /// </summary>
        public int totalLayers
        {
            get
            {
                // Let's check to see if the value has been cached yet?
                if (_totalLayers == -1)
                    _totalLayers = Enum.GetNames(typeof(Layer)).Length;

                // Return the cached value
                return _totalLayers;
            }
        }

        /// <summary>
        ///     The total tiles in the chip.
        /// </summary>
        public int total
        {
            get { return columns * rows; }
        }

        /// <summary>
        ///     The width of the tile map by tiles.
        /// </summary>
        public int columns { get; private set; }

        /// <summary>
        ///     The height of the tile map in tiles.
        /// </summary>
        public int rows { get; private set; }

        public bool invalid { get; private set; }

        public void Invalidate()
        {
            invalid = true;
        }

        public void ResetValidation()
        {
            invalid = false;
        }


        public int scrollX
        {
            get { return _scrollX; }

            set
            {
                if (_scrollX == value)
                    return;

                //                if (value > realWidth)
                //                    value = MathUtil.Repeat(value, realWidth);

                _scrollX = MathUtil.Repeat(value, realWidth);

                Invalidate();
            }
        }

        public int scrollY
        {
            get { return _scrollY; }

            set
            {
                if (_scrollY == value)
                    return;

                //                if (value > realHeight)
                //                    value = MathUtil.Repeat(value, realHeight);

                _scrollY = MathUtil.Repeat(value, realHeight);

                Invalidate();
            }
        }


        public void ReadPixelData(int width, int height, ref int[] pixelData, int offsetX = 0, int offsetY = 0)
        {

            // We need to make sure we have enough pixel data to draw the tiles
            if (tmpViewportData.width != width || tmpViewportData.height != height)
            {
                tmpViewportData.Resize(width, height);
            }
            else
            {
                // Since we don't need to resize the viewport data, we just clear it.
                tmpViewportData.Clear();
            }

            var scrollX = this.scrollX;
            var scrollY = this.scrollY;

            // Calculate the first column ID
            var startCol = MathUtil.CeilToInt((float)scrollX / tileWidth) - offscreenPadding;
            startCol += MathUtil.CeilToInt((float)offsetX / tileWidth);
            scrollX += offsetX;

            var startOffsetX = (startCol * tileWidth) - scrollX;
            var totalCols = MathUtil.CeilToInt((float)width / tileWidth) + offscreenPadding;

            //UnityEngine.Debug.Log("Scroll X " + scrollX + " startCol " + startCol + " startOffsetX " + startOffsetX + " totalCols "+ totalCols);

            var startRow = MathUtil.CeilToInt((float)scrollY / tileHeight) - offscreenPadding;
            startRow += MathUtil.CeilToInt((float)offsetY / tileHeight);
            scrollY += offsetY;

            var startOffsetY = (startRow * tileHeight) - scrollY;
            var totalRows = MathUtil.CeilToInt((float)height / tileHeight) + offscreenPadding;

            //UnityEngine.Debug.Log("Scroll Y " + scrollY + " startRow " + startRow + " startOffsetY " + startOffsetY + " totalRows " + totalRows);

            var totalTiles = totalCols * totalRows;

            var tiles = new int[totalTiles];
            var offsets = new int[totalTiles];

            layers[(int) Layer.Sprites].GetPixels(startCol, startRow, totalCols, totalRows, ref tiles);
            //layers[(int) Layer.Palettes].GetPixels(startCol, startRow, totalCols, totalRows, ref offsets);


            //            var xMin = 0;
            //            var xMax = width + tileWidth;
            //            var yMin = 0;
            //            var yMax = height + tileHeight;
            //
            int x, y, spriteID;

            for (var i = 0; i < totalTiles; i++)
            {
                spriteID = tiles[i];

                if (spriteID > -1)
                {
                    PosUtil.CalculatePosition(i, totalCols, out x, out y);
                    //UnityEngine.Debug.Log("Sprite ID " + spriteID + " " + i + " " + totalTiles + " " + x +" " + y +" "+totalRows);

                    x *= tileWidth;
                    y = totalRows - 1 - y;
                    y *= tileHeight;

                    spriteChip.ReadSpriteAt(spriteID, tmpPixelData, 0);// offsets[spriteID]);

                    tmpViewportData.SetPixels(x + startOffsetX, y - startOffsetY, tileWidth, tileHeight, tmpPixelData);

                }
            }

            // 612
            //UnityEngine.Debug.Log("Total Tiles Rendered " + tilecount);
            //ConvertToTextureData(tmpTextureData);

            tmpViewportData.CopyPixels(ref pixelData);

            //            if (tmpTextureData.width != width || tmpTextureData.height != height)
            //                tmpTextureData.Resize(width, height);


            //            var columns = width / spriteWidth;
            //            var rows = height / spriteHeight;
            //
            //            ReadPixelData(tmpTextureData, offsetX, offsetY, columns, rows);
            //
            //            tmpTextureData.CopyPixels(ref pixelData);
        }


        /// <summary>
        ///     Reads the current tile and output the spriteID,
        ///     <paramref name="paletteID" /> and <paramref name="flag" /> value. Use
        ///     this to get access to the underlying tile map data structure.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="spriteID">The id of the sprite to use.</param>
        /// <param name="paletteID">
        ///     The color offset to use when rendering the sprite.
        /// </param>
        /// <param name="flag">The flag value used for collision.</param>
        public void ReadTileAt(int column, int row, out int spriteID, out int paletteID, out int flag)
        {
            spriteID = ReadDataAt(Layer.Sprites, column, row);
            paletteID = ReadDataAt(Layer.Palettes, column, row);
            flag = ReadDataAt(Layer.Flags, column, row);
        }

        /// <summary>
        ///     Returns the value in a given Tilemap layer. Accepts a layer enum and automatically converts is to a layer id.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        protected int ReadDataAt(Layer name, int column, int row)
        {
            return ReadDataAt((int) name, column, row);
        }

        protected int ReadDataAt(int id, int column, int row)
        {
            return layers[id].GetPixel(column, row);
        }

        protected void UpdateDataAt(Layer name, int column, int row, int value)
        {
            UpdateDataAt((int) name, column, row, value);

        }

        protected void UpdateDataAt(int id, int column, int row, int value)
        {
            layers[id].SetPixel(column, row, value);
            Invalidate();
        }

        /// <summary>
        ///     Updates a tile's data in the tile map. A tile consists of 3 values,
        ///     the sprite id, the palette id and the flag. Each value is an int.
        /// </summary>
        /// <param name="spriteID">The id of the sprite to use.</param>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="flag">The flag value used for collision.</param>
        /// <param name="paletteID">
        ///     The color offset to use when rendering the sprite.
        /// </param>
        public void UpdateTileAt(int spriteID, int column, int row, int flag = 0, int paletteID = 0)
        {

            UpdateDataAt(Layer.Sprites, column, row, spriteID);
            UpdateDataAt(Layer.Palettes, column, row, paletteID);
            UpdateDataAt(Layer.Flags, column, row, flag);

        }

        /// <summary>
        ///     Returns the value of a sprite at a given position in the tile map.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <returns>
        ///     Returns anint for the sprite id set at the
        ///     specified position. If the tile is empty it will return -1.
        /// </returns>
        public int ReadSpriteAt(int column, int row)
        {
            return ReadDataAt(Layer.Sprites, column, row);
        }

        /// <summary>
        ///     Updates a sprite id for a tile at a given position. Set this value
        ///     to -1 if you want it to be empty. Empty tiles will automatically be
        ///     filled in with the engine's transparent color when rendered to the
        ///     ScreenBufferChip.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="spriteID">
        ///     The index of the sprite to use for the tile.
        /// </param>
        public void UpdateSpriteAt(int column, int row, int spriteID)
        {
            UpdateDataAt(Layer.Sprites, column, row, spriteID);

            //layers[(int)Layer.Sprites].UpdateDataAt(column, row, spriteID);
        }

        /// <summary>
        ///     Reads the palette offset at a give position in the tile map. When
        ///     reading the pixel data of a sprite from the tile map, the palette
        ///     value will be added to all of the pixel data ints to shift the
        ///     colors of the tile.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <returns>
        ///     Returns the color int offset.
        /// </returns>
        public int ReadPaletteAt(int column, int row)
        {
            return ReadDataAt(Layer.Palettes, column, row);

            //return layers[(int)Layer.Palettes].GetDataAt(column, row);
        }

        /// <summary>
        ///     Used to offset the pixel data of a tile sprite. Set the value which
        ///     is added to all the ints in a requested tile's data when being
        ///     rendered to the ScreenBufferChip.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="paletteID">
        ///     A color int offset.
        /// </param>
        public void UpdatePaletteAt(int column, int row, int paletteID)
        {
            UpdateDataAt(Layer.Palettes, column, row, paletteID);

            //layers[(int)Layer.Palettes].UpdateDataAt(column, row, paletteID);
        }

        /// <summary>
        ///     Returns the flag value at a specific position. The flag can be used
        ///     for collision detection on the tile map.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <returns>
        ///     Returns an int for the flag value.
        /// </returns>
        public int ReadFlagAt(int column, int row)
        {
            return ReadDataAt(Layer.Flags, column, row);

            //return layers[(int)Layer.Flags].GetDataAt(column, row);
        }

        /// <summary>
        ///     This method updates the <paramref name="flag" /> value at a given
        ///     position. -1 means there is no <paramref name="flag" /> and the
        ///     maximum value is capped by the totalFlag field.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="flag">The value of the flag as an int.</param>
        public void UpdateFlagAt(int column, int row, int flag)
        {
            UpdateDataAt(Layer.Flags, column, row, flag);

            //layers[(int)Layer.Flags].UpdateDataAt(column, row, flag);
        }

        /// <summary>
        ///     Resizes the tile map. When a tile map is resized, all of the sprite,
        ///     palette and flag data is destroyed.
        /// </summary>
        /// <param name="column">
        ///     The column position of the tile. 0 is the left of the tile map.
        /// </param>
        /// <param name="row">
        ///     The row position of the tile. 0 is the top of the tile map.
        /// </param>
        /// <param name="clear">
        ///     A optional value to perform a clear on the resized spriteID,
        ///     paletteID and <see cref="flags" /> arrays to return their values to
        ///     -1. This is set to true by default.
        /// </param>
        public void Resize(int columns, int rows, bool clear = true)
        {
            this.columns = columns;
            this.rows = rows;

            var size = total;

            // Get the total number of layers we are working with
            //var totalLayers = Enum.GetNames(typeof(Layer)).Length;

            // Make sure we have the layers we need
            if (layers == null)
                layers = new TextureData[totalLayers];

            // Loop through each data layer and resize it
            for (var i = 0; i < totalLayers; i++)
                if (layers[i] == null)
                    layers[i] = new TextureData(columns, rows, true);
                else
                    layers[i].Resize(columns, rows);

        }

        /// <summary>
        ///     This clears all the tile map data. The spriteID and flag arrays are
        ///     set to -1 as their default value and the palette array is set to 0.
        /// </summary>
        public void Clear()
        {
            // Get the total number of layers we are working with
            var totalLayers = Enum.GetNames(typeof(Layer)).Length;

            // Make sure we have the layers we need
            if (layers == null)
                layers = new TextureData[totalLayers];

            // Loop through each data layer and resize it
            for (var i = 0; i < totalLayers; i++)
                if (layers[i] == null)
                    layers[i] = new TextureData(columns, rows, false);
                else
                    layers[i].Clear();

        }

        /// <summary>
        ///     This method converts the tile map into pixel data that can be
        ///     rendered by the engine. It's an expensive operation and should only
        ///     be called when the game or level is loading up. This data can be
        ///     passed into the ScreenBufferChip to allow cached rendering of the
        ///     tile map as well as scrolling of the tile map if it is larger then
        ///     the screen's resolution.
        /// </summary>
        /// <param name="textureData">
        ///     A reference to a <see cref="TextureData" /> class to populate with
        ///     tile map pixel data.
        /// </param>
        /// <param name="clearColor">
        ///     The transparent color to use when a tile is set to -1. The default
        ///     value is -1 for transparent.
        /// </param>
        public void ConvertToTextureData(TextureData textureData, int clearColor = -1)
        {
            if (spriteChip == null)
                return;

            ReadPixelData(textureData, 0, 0, columns, rows);
        }

        protected void ReadPixelData(TextureData textureData, int startColumn, int startRow, int blockWidth, int blockHeight, int clearColor = -1)
        {

            if (textureData.width != realWidth || textureData.height != realWidth)
                textureData.Resize(realWidth, realHeight);

            layers[(int) Layer.Sprites].CopyPixels(ref tmpSpriteIDs);
            layers[(int) Layer.Palettes].CopyPixels(ref tmpPaletteIDs);

            int x, y, spriteID;

            for (var i = 0; i < total; i++)
            {
                spriteID = tmpSpriteIDs[i];

                if (spriteID > -1)
                {

                    spriteChip.ReadSpriteAt(spriteID, tmpPixelData, tmpPaletteIDs[spriteID]);

                    PosUtil.CalculatePosition(i, columns, out x, out y);

                    x *= tileWidth;
                    y = rows - 1 - y;
                    y *= tileHeight;

                    textureData.SetPixels(x, y, tileWidth, tileHeight, tmpPixelData);
                }
            }
        }

        /// <summary>
        ///     Configured the TileMapChip. This method sets the
        ///     <see cref="TileMapChip" /> as the default tile map for the engine. It
        ///     also resizes the tile map to its default size of 32 x 30 which is a
        ///     resolution of 256 x 240.
        /// </summary>
        public override void Configure()
        {
            //ppu.tileMap = this;
            engine.tileMapChip = this;

            //tmpPixelData = new int[engine.spriteChip.width*engine.spriteChip.height];
            // Resize to default nes resolution
            Resize(32, 30);
        }

        public override void Deactivate()
        {
            base.Deactivate();
            engine.tileMapChip = null;
        }

        protected enum Layer
        {

            Sprites,
            Palettes,
            Flags

        }

    }

}