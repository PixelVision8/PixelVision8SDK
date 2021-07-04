using UnityEngine;

namespace PixelVision8.Runner
{
    public class ColorUtils
    {
        /// <summary>
        ///     The display target is in charge of converting system colors into MonoGame colors. This utility method
        ///     can be used by any external runner class to correctly convert hex colors into Colors.
        /// </summary>
        /// <param name="hexColors"></param>
        /// <param name="maskColor"></param>
        /// <param name="debugMode"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public static Color[] ConvertColors(string[] hexColors, string maskColor = "#FF00FF", bool debugMode = false, int backgroundColor = 0)
        {
            var t = hexColors.Length;
            var colors = new Color[t];

            for (var i = 0; i < t; i++)
            {
                var colorHex = hexColors[i];

                if (colorHex == maskColor && debugMode == false) colorHex = hexColors[backgroundColor];

                // ColorData.HexToRgb(colorHex, out var r, out var g, out var b);

                // Debug.Log("Color " + i+ " r" + r +" g" + b " b" + b);

                ColorUtility.TryParseHtmlString(colorHex, out var newCol);

                colors[i] = newCol;
                 
            }

            return colors;
        }

        public static string RgbToHex(ColorData colorData)
        {
            return "#" + string.Format("{0:X2}{1:X2}{2:X2}", colorData.R, colorData.G, colorData.B);
        }
        
        public static string RgbToHex(Color color)
        {
            return "#" + string.Format("{0:X2}{1:X2}{2:X2}", (int)(color.r*255), (int)(color.g * 255), (int)(color.b * 255));
        }
    }
}
