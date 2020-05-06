﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using Windows.Storage.Streams;

namespace Yugen.Mosaic.Uwp.Interfaces
{
    public interface IMosaicService
    {
        Image<Rgba32> AddMasterImage(Stream stream);
        Image<Rgba32> AddTileImage(string name, Stream stream);
        Image<Rgba32> GenerateMosaic(Size outputSize, Size tileSize, int mosaicType);
        Image<Rgba32> GetResizedImage(Image<Rgba32> image, int size);
        InMemoryRandomAccessStream GetStream(Image<Rgba32> image);
        void RemoveTileImage(string name);
        void Reset();
    }
}