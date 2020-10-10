﻿using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Text;
using Yugen.Mosaic.Uwp.Interfaces;
using Yugen.Toolkit.Standard.Services;

namespace Yugen.Mosaic.Uwp.Services
{
    public class SearchAndReplaceAsciiArtService : ISearchAndReplaceAsciiArtService
    {
        private static readonly string[] asciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", " " };
        private readonly IProgressService _progressService;

        public SearchAndReplaceAsciiArtService(IProgressService progressService)
        {
            _progressService = progressService;
        }

        public Image<Rgba32> SearchAndReplace(Image<Rgba32> masterImage, int ratio = 5)
        {
            _progressService.Reset();

            bool toggle = false;
            StringBuilder sb = new StringBuilder();

            for (int h = 0; h < masterImage.Height; h += ratio)
            {
                for (int w = 0; w < masterImage.Width; w += ratio)
                {
                    var pixelColor = masterImage[w, h];
                    var color = Convert.ToByte((pixelColor.R + pixelColor.G + pixelColor.B) / 3);
                    var grayColor = new Rgba32(color, color, color);

                    if (!toggle)
                    {
                        int index = grayColor.R * 10 / 255;
                        sb.Append(asciiChars[index]);
                    }
                }

                if (!toggle)
                {
                    sb.AppendLine();
                    toggle = true;
                }
                else
                {
                    toggle = false;
                }

                _progressService.IncrementProgress(masterImage.Height);
            }

            var font = SystemFonts.CreateFont("Courier New", 14);
            var text = sb.ToString();
            var size = TextMeasurer.Measure(text, new RendererOptions(font));

            var finalImage = new Image<Rgba32>((int)size.Width, (int)size.Height);
            finalImage.Mutate(i =>
            {
                i.Fill(SixLabors.ImageSharp.Color.White);
                i.DrawText(text, font, Color.Black, new PointF(0, 0));
             });

            return finalImage;
        }
    }
}