﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.Primitives;
using System;
using Yugen.Mosaic.Uwp.Models;

namespace Yugen.Mosaic.Uwp.Services
{
    public sealed class GetPixelProcessor : IImageProcessor
    {
        private int _x;
        private int _y;

        public MyColor MyColor;

        public GetPixelProcessor(int x, int y, MyColor myColor)
        {
            _x = x;
            _y = y;

            MyColor = myColor;
        }

        /// <inheritdoc/>
        public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Image<TPixel> source, Rectangle sourceRectangle) where TPixel : struct, IPixel<TPixel>
        {
            return new GetPixelProcessor<TPixel>(this, source, sourceRectangle, _x, _y, MyColor);
        }
    }

    public class GetPixelProcessor<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// The source <see cref="Image{TPixel}"/> instance to modify
        /// </summary>
        private readonly Image<TPixel> Source;

        private int _x;
        private int _y;

        public MyColor MyColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslGaussianBlurProcessor"/> class
        /// </summary>
        /// <param name="definition">The <see cref="HlslGaussianBlurProcessor"/> defining the processor parameters</param>
        /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance</param>
        /// <param name="sourceRectangle">The source area to process for the current processor instance</param>
        public GetPixelProcessor(GetPixelProcessor definition, Image<TPixel> source, Rectangle sourceRectangle, int x, int y, MyColor myColor)
        {
            Source = source;

            _x = x;
            _y = y;

            MyColor = myColor;
        }


        /// <inheritdoc/>
        public void Apply()
        {
            int width = Source.Width;
            Image<TPixel> source = Source; // Avoid capturing this
            
            Rgba32 pixel = new Rgba32();
            source[_x,_y].ToRgba32(ref pixel);

            MyColor.R = pixel.R;
            MyColor.G = pixel.G;
            MyColor.B = pixel.B;
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
