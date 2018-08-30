﻿﻿//   
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelVisionSDK.Chips
{

    public class DisplayChip : AbstractChip, IDraw
    {
        public int overscanX { get; set; }
        public int overscanY { get; set; }
        public int totalPixels;
        public int[] pixels = new int[0];
        
        protected int _width = 256;
        protected int _height = 240;
        protected Stack<int[]> drawRequestPixelDataPool = new Stack<int[]>();
        protected List<DrawRequest>[] drawRequestLayers = new List<DrawRequest>[0];
        
        public int layers
        {
            get { return drawRequestLayers.Length; }
            set
            {
                Array.Resize(ref drawRequestLayers, value);
                for (var i = value - 1; i > -1; i--)
                {
                    var requests = drawRequestLayers[i];
                    if (requests == null)
                        drawRequestLayers[i] = new List<DrawRequest>();
                    else
                        requests.Clear();
                }
            }
        }

        public int overscanXPixels
        {
            get { return overscanX * engine.spriteChip.width; }
        }

        public int overscanYPixels
        {
            get { return overscanY * engine.spriteChip.height; }
        }

//        public bool displayMaskColor;
        
        /// <summary>
        ///     This returns the visble areas sprites should be displayed on. Note that x and y may be negative if overscan is set since the screen wraps.
        /// </summary>
        public Rect visibleBounds
        {
            get
            {
                return new Rect(-overscanXPixels, -overscanYPixels, width - overscanXPixels, height - overscanYPixels);
            }
        }

        /// <summary>
        ///     Returns the display's <see cref="width" />
        /// </summary>
        public int width
        {
            get { return _width; }
        }

        /// <summary>
        ///     Returns the display's <see cref="height" />
        /// </summary>
        public int height
        {
            get { return _height; }
        }

        /// <summary>
        /// </summary>
        public void Draw()
        {
            // Loop through all draw requests
            for (var layer = 0; layer < drawRequestLayers.Length; layer++)
            {
                // TODO need to add back in support for turning layers on and off

                var drawRequests = drawRequestLayers[layer];
                var totalDR = drawRequests.Count;
                for (var i = 0; i < totalDR; i++)
                {
                    var draw = drawRequests[i];

                    CopyDrawRequest(draw.pixelData, draw.x, draw.y, draw.width, draw.height, draw.colorOffset);

                }
            }

            // Reset Draw Requests after they have been processed
            ResetDrawCalls();
            
        }

        private DrawRequest draw;
        
        /// <summary>
        ///     Creates a new draw by copying the supplied pixel data over
        ///     to the Display's TextureData.
        /// </summary>
        /// <param name="pixelData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="layer"></param>
        /// <param name="colorOffset"></param>
        /// <param name="layerOrder"></param>
        public void NewDrawCall(int[] pixelData, int x, int y, int width, int height, int layer = 0, int colorOffset = 0)
        {
                draw = NextDrawRequest();
                draw.x = x;
                draw.y = y;
                draw.width = width;
                draw.height = height;
                draw.pixelData = pixelData;
                draw.colorOffset = colorOffset;
                drawRequestLayers[layer].Add(draw);
        }

        /// <summary>
        ///     Changes the resolution of the display.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void ResetResolution(int width, int height)
        {
            _width = width;
            _height = height;
            
            totalPixels = _width * _height;
            
            Array.Resize(ref pixels, totalPixels);

        }

        /// <summary>
        ///     This configures the DisplayChip. It registers itself as the default
        ///     <see cref="DisplayChip" /> for the engine, gets a reference to the
        ///     engine's renderTarget, sets <see cref="autoClear" /> and
        ///     <see cref="wrapMode" /> to true and
        ///     finally resets the resolution to its default value
        ///     of 256 x 240.
        /// </summary>
        public override void Configure()
        {
            //Debug.Log("Pixel Data Renderer: Configure ");
            engine.displayChip = this;

            ResetResolution(256, 240);
            
            // By default set the total layers to the DrawModes minus Tilemap Cache which isn't used for rendering
            layers = Enum.GetNames(typeof(DrawMode)).Length - 1;

        }

        public override void Deactivate()
        {
            base.Deactivate();
            engine.displayChip = null;
        }

        public void ResetDrawCalls()
        {
            // Reset all draw requests
            for (var layer = drawRequestLayers.Length - 1; layer > -1; layer--)
            {
                var drawRequests = drawRequestLayers[layer];

                for (var i = drawRequests.Count - 1; i > -1; i--)
                {
                    var request = drawRequests[i];
                    drawRequestPixelDataPool.Push(request.pixelData);
                }

                drawRequests.Clear();
            }
        }

        public DrawRequest NextDrawRequest()
        {
            var request = new DrawRequest();

            if (drawRequestPixelDataPool.Count > 0)
            {
                request.pixelData = drawRequestPixelDataPool.Pop();
            }
            else
            {
                request.pixelData = new int[0];
            }

            return request;
        }

        private int total;
        private int srcX;
        private int srcY;
        private int colorID;
        private int i;
        private int index;
        
        public void CopyDrawRequest(int[] pixelData, int x, int y, int width, int height, int colorOffset = 0)
        {
            
            total = width * height;

            for (i = 0; i < total; i++)
            {
                
                colorID = pixelData[i];

                if (colorID > -1)
                {
                    if (colorOffset > 0)
                        colorID += colorOffset;
                    
                    srcX = (i % width) + x;
                    srcY = (i / width) + y; 
                    
                    // Make sure x & y are wrapped around the display
                    srcX = (int) (srcX - Math.Floor(srcX / (float) _width) * _width);

                    srcY = (int) (srcY - Math.Floor(srcY / (float) _height) * _height);
                    
                    // Find the index
                    index = srcX + _width * srcY;
                    
                    // Set the pixel
                    pixels[index] = colorID;


                }
                
            }
        }
        
    }

}