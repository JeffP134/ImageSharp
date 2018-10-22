﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.ImageSharp.PixelFormats
{
    /// <summary>
    /// An interface that represents a generic pixel type.
    /// The naming convention of each pixel format is to order the color components from least significant to most significant, reading from left to right.
    /// For example in the <see cref="Rgba32"/> pixel format the R component is the least significant byte, and the A component is the most significant.
    /// </summary>
    /// <typeparam name="TSelf">The type implementing this interface</typeparam>
    public interface IPixel<TSelf> : IPixel, IEquatable<TSelf>
        where TSelf : struct, IPixel<TSelf>
    {
        /// <summary>
        /// Creates a <see cref="PixelOperations{TPixel}"/> instance for this pixel type.
        /// This method is not intended to be consumed directly. Use <see cref="PixelOperations{TPixel}.Instance"/> instead.
        /// </summary>
        /// <returns>The <see cref="PixelOperations{TPixel}"/> instance.</returns>
        PixelOperations<TSelf> CreatePixelOperations();
    }

    /// <summary>
    /// An interface that represents a pixel type.
    /// </summary>
    public interface IPixel
    {
        /// <summary>
        /// Sets the packed representation from a scaled <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">The vector to create the packed representation from.</param>
        void FromScaledVector4(Vector4 vector);

        /// <summary>
        /// Expands the packed representation into a scaled <see cref="Vector4"/>
        /// with values clamped between <value>0</value> and <value>1</value>.
        /// The vector components are typically expanded in least to greatest significance order.
        /// </summary>
        /// <returns>The <see cref="Vector4"/>.</returns>
        Vector4 ToScaledVector4();

        /// <summary>
        /// Sets the packed representation from a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">The vector to create the packed representation from.</param>
        void FromVector4(Vector4 vector);

        /// <summary>
        /// Expands the packed representation into a <see cref="Vector4"/>.
        /// The vector components are typically expanded in least to greatest significance order.
        /// </summary>
        /// <returns>The <see cref="Vector4"/>.</returns>
        Vector4 ToVector4();

        /// <summary>
        /// Packs the pixel from an <see cref="Argb32"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Argb32"/> value.</param>
        void FromArgb32(Argb32 source);

        /// <summary>
        /// Packs the pixel from an <see cref="Bgr24"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Bgr24"/> value.</param>
        void FromBgr24(Bgr24 source);

        /// <summary>
        /// Packs the pixel from an <see cref="Bgra32"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Bgra32"/> value.</param>
        void FromBgra32(Bgra32 source);

        /// <summary>
        /// Packs the Pixel from an <see cref="Gray8"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Gray8"/> value.</param>
        void FromGray8(Gray8 source);

        /// <summary>
        /// Packs the Pixel from an <see cref="Gray16"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Gray16"/> value.</param>
        void FromGray16(Gray16 source);

        /// <summary>
        /// Packs the pixel from an <see cref="Rgb24"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Rgb24"/> value.</param>
        void FromRgb24(Rgb24 source);

        /// <summary>
        /// Packs the pixel from an <see cref="Rgba32"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Rgba32"/> value.</param>
        void FromRgba32(Rgba32 source);

        /// <summary>
        /// Expands the packed representation into an <see cref="Rgba32"/>.
        /// </summary>
        /// <param name="dest">The reference to the destination <see cref="Rgba32"/> pixel</param>
        void ToRgba32(ref Rgba32 dest);

        /// <summary>
        /// Packs the pixel from an <see cref="Rgb48"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Rgb48"/> value.</param>
        void FromRgb48(Rgb48 source);

        /// <summary>
        /// Packs the pixel from an <see cref="Rgba64"/> value.
        /// </summary>
        /// <param name="source">The <see cref="Rgba64"/> value.</param>
        void FromRgba64(Rgba64 source);
    }
}