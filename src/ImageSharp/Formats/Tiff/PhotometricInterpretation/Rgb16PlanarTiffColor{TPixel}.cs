// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using SixLabors.ImageSharp.Formats.Tiff.Utils;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Formats.Tiff.PhotometricInterpretation
{
    /// <summary>
    /// Implements the 'RGB' photometric interpretation with 'Planar' layout for all 16 bit.
    /// </summary>
    internal class Rgb16PlanarTiffColor<TPixel> : TiffBasePlanarColorDecoder<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly bool isBigEndian;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rgb16PlanarTiffColor{TPixel}" /> class.
        /// </summary>
        /// <param name="isBigEndian">if set to <c>true</c> decodes the pixel data as big endian, otherwise as little endian.</param>
        public Rgb16PlanarTiffColor(bool isBigEndian) => this.isBigEndian = isBigEndian;

        /// <inheritdoc/>
        public override void Decode(IMemoryOwner<byte>[] data, Buffer2D<TPixel> pixels, int left, int top, int width, int height)
        {
            var color = default(TPixel);

            System.Span<byte> redData = data[0].GetSpan();
            System.Span<byte> greenData = data[1].GetSpan();
            System.Span<byte> blueData = data[2].GetSpan();

            int offset = 0;
            var rgba = default(Rgba64);
            for (int y = top; y < top + height; y++)
            {
                System.Span<TPixel> pixelRow = pixels.GetRowSpan(y);
                if (this.isBigEndian)
                {
                    for (int x = left; x < left + width; x++)
                    {
                        ulong r = TiffUtils.ConvertToShortBigEndian(redData.Slice(offset, 2));
                        ulong g = TiffUtils.ConvertToShortBigEndian(greenData.Slice(offset, 2));
                        ulong b = TiffUtils.ConvertToShortBigEndian(blueData.Slice(offset, 2));

                        offset += 2;

                        pixelRow[x] = TiffUtils.ColorFromRgba64(rgba, r, g, b, color);
                    }
                }
                else
                {
                    for (int x = left; x < left + width; x++)
                    {
                        ulong r = TiffUtils.ConvertToShortLittleEndian(redData.Slice(offset, 2));
                        ulong g = TiffUtils.ConvertToShortLittleEndian(greenData.Slice(offset, 2));
                        ulong b = TiffUtils.ConvertToShortLittleEndian(blueData.Slice(offset, 2));

                        offset += 2;

                        pixelRow[x] = TiffUtils.ColorFromRgba64(rgba, r, g, b, color);
                    }
                }
            }
        }
    }
}
