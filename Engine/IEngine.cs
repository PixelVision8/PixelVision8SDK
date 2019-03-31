﻿//   
// Copyright (c) Jesse Freeman, Pixel Vision 8. All rights reserved.  
//  
// Licensed under the Microsoft Public License (MS-PL) except for a few
// portions of the code. See LICENSE file in the project root for full 
// license information. Third-party libraries used by Pixel Vision 8 are 
// under their own licenses. Please refer to those libraries for details 
// on the license they use.
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman - @JesseFreeman
// Christina-Antoinette Neofotistou @CastPixel
// Christer Kaitila - @McFunkypants
// Pedro Medeiros - @saint11
// Shawn Rakowski - @shwany
//

using System.Collections.Generic;
using PixelVision8.Engine.Chips;

namespace PixelVision8.Engine
{
    /// <summary>
    ///     The <see cref="IEngine" /> internal represents the
    ///     core API for the Engine class. Implement this
    ///     internal to create custom engine classes.
    /// </summary>
    public interface IEngine : IEngineChips, IUpdate, IDraw
    {
        /// <summary>
        ///     A flag for if the engine is <see cref="running" />
        /// </summary>
//        bool running { get; }

        string name { get; set; }

        Dictionary<string, string> metaData { get; }
//        bool canWrite { get; set; }

        /// <summary>
        ///     This method loads a <paramref name="game" /> into memory
        /// </summary>
        /// <param name="game"></param>
//        void LoadGame();

        /// <summary>
        ///     Run the game in memory
        /// </summary>
        void RunGame();

        /// <summary>
        /// </summary>
        void Shutdown();

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        string GetMetaData(string key, string defaultValue = "");

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetMetaData(string key, string value);

        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="ignoreKeys"></param>
        void DumpMetaData(Dictionary<string, string> target, string[] ignoreKeys = null);
    }
}