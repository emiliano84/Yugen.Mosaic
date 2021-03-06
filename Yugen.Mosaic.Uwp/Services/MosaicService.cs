﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Yugen.Mosaic.Uwp.Enums;
using Yugen.Mosaic.Uwp.Interfaces;
using Yugen.Mosaic.Uwp.Models;
using Yugen.Toolkit.Standard.Core.Models;
using Yugen.Toolkit.Standard.Services;
using Yugen.Toolkit.Uwp.Helpers;

namespace Yugen.Mosaic.Uwp.Services
{
    public class MosaicService : IMosaicService
    {
        private readonly IProgressService _progressService;
        private readonly ISearchAndReplaceAsciiArtService _searchAndReplaceAsciiArtService;
        private readonly ISearchAndReplaceServiceFactory _searchAndReplaceServiceFactory;
        private readonly List<Tile> _tileImageList = new List<Tile>();

        private Rgba32[,] _avgsMaster;
        private int _tX;
        private int _tY;
        private Image<Rgba32> _masterImage;
        private Size _tileSize;
        private ISearchAndReplaceService _searchAndReplaceService;

        public MosaicService(
            IProgressService progressService,
            ISearchAndReplaceAsciiArtService searchAndReplaceAsciiArtService,
            ISearchAndReplaceServiceFactory searchAndReplaceServiceFactory)
        {
            _progressService = progressService;
            _searchAndReplaceAsciiArtService = searchAndReplaceAsciiArtService;
            _searchAndReplaceServiceFactory = searchAndReplaceServiceFactory;

            StorageApplicationPermissions.FutureAccessList.Clear();
        }

        public string GetAsciiText => _searchAndReplaceAsciiArtService.Text;

        public async Task<Size> AddMasterImage(StorageFile storageFile)
        {
            using (var inputStream = await storageFile.OpenReadAsync())
            using (var stream = inputStream.AsStreamForRead())
            {
                _masterImage = Image.Load<Rgba32>(stream);
            }

            return _masterImage.Size();
        }

        public void AddTileImage(string name, StorageFile storageFile)
        {
            var faToken = StorageApplicationPermissions.FutureAccessList.Add(storageFile);
            _tileImageList.Add(new Tile(name, faToken));
        }

        public async Task<Result<Image<Rgba32>>> Generate(Size outputSize, Size tileSize, MosaicTypeEnum selectedMosaicType)
        {
            if (_masterImage == null)
            {
                var message = ResourceHelper.GetText("MosaicServiceErrorMasterImage");
                return Result.Fail<Image<Rgba32>>(message);
            }

            if (selectedMosaicType != MosaicTypeEnum.PlainColor &&
                selectedMosaicType != MosaicTypeEnum.AsciiArt &&
                _tileImageList.Count < 1)
            {
                var message = ResourceHelper.GetText("MosaicServiceErrorTiles");
                return Result.Fail<Image<Rgba32>>(message);
            }

            Image<Rgba32> resizedMasterImage = _masterImage.Clone(x => x.Resize(outputSize.Width, outputSize.Height));

            if (selectedMosaicType == MosaicTypeEnum.AsciiArt)
            {
                return GenerateAsciiArt(outputSize, resizedMasterImage);
            }
            else
            {
                return await GenerateMosaic(outputSize, resizedMasterImage, tileSize, selectedMosaicType);
            }
        }

        public Image<Rgba32> GetResizedImage(Image<Rgba32> image, int size)
        {
            var resizeOptions = new ResizeOptions()
            {
                Mode = ResizeMode.Max,
                Size = new Size(size, size)
            };

            return image.Clone(x => x.Resize(resizeOptions));
        }

        public InMemoryRandomAccessStream GetStream(Image<Rgba32> image)
        {
            var outputStream = new InMemoryRandomAccessStream();
            image.SaveAsJpeg(outputStream.AsStreamForWrite());
            outputStream.Seek(0);
            return outputStream;
        }

        public void RemoveTileImage(string name)
        {
            Tile item = _tileImageList.FirstOrDefault(x => x.Name.Equals(name));
            if (item != null)
            {
                _tileImageList.Remove(item);
            }
        }

        public void Reset()
        {
            _masterImage = null;
            _tileImageList.Clear();
        }

        private Result<Image<Rgba32>> GenerateAsciiArt(Size outputSize, Image<Rgba32> resizedMasterImage)
        {
            var finalImage = _searchAndReplaceAsciiArtService.SearchAndReplace(resizedMasterImage);

            GC.Collect();

            return Result.Ok(finalImage);
        }

        private async Task<Result<Image<Rgba32>>> GenerateMosaic(Size outputSize, Image<Rgba32> resizedMasterImage, Size tileSize, MosaicTypeEnum selectedMosaicType)
        {
            _tileSize = tileSize;
            _tX = resizedMasterImage.Width / tileSize.Width;
            _tY = resizedMasterImage.Height / tileSize.Height;
            _avgsMaster = new Rgba32[_tX, _tY];

            GetTilesAverage(resizedMasterImage);

            if (selectedMosaicType != MosaicTypeEnum.PlainColor)
            {
                await LoadTilesAndResize();
            }

            _searchAndReplaceService = _searchAndReplaceServiceFactory.Create(selectedMosaicType);
            _searchAndReplaceService.Init(_avgsMaster, outputSize, _tileImageList, tileSize, _tX, _tY);

            var finalImage = _searchAndReplaceService.SearchAndReplace();

            Dispose();

            return Result.Ok(finalImage);
        }

        private void GetTilesAverage(Image<Rgba32> masterImage)
        {
            Parallel.For(0, _tY, y =>
            {
                Span<Rgba32> rowSpan = masterImage.GetPixelRowSpan(y);

                for (var x = 0; x < _tX; x++)
                {
                    _avgsMaster[x, y].FromRgba32(Helpers.ColorHelper.GetAverageColor(masterImage, x, y, _tileSize));
                }

                _progressService.IncrementProgress(_tY, 0, 33);
            });
        }

        private async Task LoadTilesAndResize()
        {
            _progressService.Reset();

            var processTiles = _tileImageList.AsParallel().Select(tile => ProcessTile(tile));
            await Task.WhenAll(processTiles);
        }

        private async Task ProcessTile(Tile tile)
        {
            StorageFile storageFile = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(tile.FaToken);
            IRandomAccessStream randomAccessStream = await storageFile.OpenAsync(FileAccessMode.Read);
            tile.Process(_tileSize, randomAccessStream);

            _progressService.IncrementProgress(_tileImageList.Count, 33, 66);
        }

        private void Dispose()
        {
            Parallel.ForEach(_tileImageList, tile =>
            {
                tile.Dispose();
            });

            Configuration.Default.MemoryAllocator.ReleaseRetainedResources();

            GC.Collect();
        }
    }
}