// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers.Binary;

namespace SixLabors.ImageSharp.Formats.WebP
{
    internal static class LossyUtils
    {
        private static void Put16(int v, Span<byte> dst)
        {
            for (int j = 0; j < 16; ++j)
            {
                Memset(dst.Slice(j * WebPConstants.Bps), (byte)v, 0, 16);
            }
        }

        public static void DC16_C(Span<byte> dst, byte[] yuv, int offset)
        {
            int dc = 16;
            for (int j = 0; j < 16; ++j)
            {
                // DC += dst[-1 + j * BPS] + dst[j - BPS];
                dc += yuv[offset - 1 + (j * WebPConstants.Bps)] + yuv[offset + j - WebPConstants.Bps];
            }

            Put16(dc >> 5, dst);
        }

        public static void TM16_C(Span<byte> dst, byte[] yuv, int offset)
        {
            TrueMotion(dst, yuv, offset, 16);
        }

        public static void VE16_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // vertical
            Span<byte> src = yuv.AsSpan(offset - WebPConstants.Bps, 16);
            for (int j = 0; j < 16; ++j)
            {
                // memcpy(dst + j * BPS, dst - BPS, 16);
                src.CopyTo(dst.Slice(j * WebPConstants.Bps));
            }
        }

        public static void HE16_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // horizontal
            for (int j = 16; j > 0; --j)
            {
                // memset(dst, dst[-1], 16);
                byte v = yuv[offset - 1];
                Memset(dst, v, 0, 16);
                offset += WebPConstants.Bps;
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        public static void DC16NoTop_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // DC with top samples not available.
            int dc = 8;
            for (int j = 0; j < 16; ++j)
            {
                // DC += dst[-1 + j * BPS];
                dc += yuv[-1 + (j * WebPConstants.Bps) + offset];
            }

            Put16(dc >> 4, dst);
        }

        public static void DC16NoLeft_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // DC with left samples not available.
            int dc = 8;
            for (int i = 0; i < 16; ++i)
            {
                // DC += dst[i - BPS];
                dc += yuv[i - WebPConstants.Bps + offset];
            }

            Put16(dc >> 4, dst);
        }

        public static void DC16NoTopLeft_C(Span<byte> dst)
        {
            // DC with no top and left samples.
            Put16(0x80, dst);
        }

        public static void DC8uv_C(Span<byte> dst, byte[] yuv, int offset)
        {
            int dc0 = 8;
            for (int i = 0; i < 8; ++i)
            {
                // dc0 += dst[i - BPS] + dst[-1 + i * BPS];
                dc0 += yuv[offset + i - WebPConstants.Bps] + yuv[offset - 1 + (i * WebPConstants.Bps)];
            }

            Put8x8uv((byte)(dc0 >> 4), dst);
        }

        public static void TM8uv_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // TrueMotion
            TrueMotion(dst, yuv, offset, 8);
        }

        public static void VE8uv_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // vertical
            Span<byte> src = yuv.AsSpan(offset - WebPConstants.Bps, 8);

            for (int j = 0; j < 8; ++j)
            {
                // memcpy(dst + j * BPS, dst - BPS, 8);
                src.CopyTo(dst.Slice(j * WebPConstants.Bps));
            }
        }

        public static void HE8uv_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // horizontal
            for (int j = 0; j < 8; ++j)
            {
                // memset(dst, dst[-1], 8);
                // dst += BPS;
                byte v = yuv[offset - 1];
                Memset(dst, v, 0, 8);
                dst = dst.Slice(WebPConstants.Bps);
                offset += WebPConstants.Bps;
            }
        }

        public static void DC8uvNoTop_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // DC with no top samples.
            int dc0 = 4;
            for (int i = 0; i < 8; ++i)
            {
                // dc0 += dst[-1 + i * BPS];
                dc0 += yuv[offset - 1 + (i * WebPConstants.Bps)];
            }

            Put8x8uv((byte)(dc0 >> 3), dst);
        }

        public static void DC8uvNoLeft_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // DC with no left samples.
            int dc0 = 4;
            for (int i = 0; i < 8; ++i)
            {
                // dc0 += dst[i - BPS];
                dc0 += yuv[offset + i - WebPConstants.Bps];
            }

            Put8x8uv((byte)(dc0 >> 3), dst);
        }

        public static void DC8uvNoTopLeft_C(Span<byte> dst)
        {
            // DC with nothing.
            Put8x8uv(0x80, dst);
        }

        public static void DC4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            int dc = 4;
            for (int i = 0; i < 4; ++i)
            {
                dc += yuv[offset + i - WebPConstants.Bps] + yuv[offset - 1 + (i * WebPConstants.Bps)];
            }

            dc >>= 3;
            for (int i = 0; i < 4; ++i)
            {
                Memset(dst, (byte)dc, i * WebPConstants.Bps, 4);
            }
        }

        public static void TM4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            TrueMotion(dst, yuv, offset, 4);
        }

        public static void VE4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // vertical
            int topOffset = offset - WebPConstants.Bps;
            byte[] vals =
            {
                Avg3(yuv[topOffset - 1], yuv[topOffset], yuv[topOffset + 1]),
                Avg3(yuv[topOffset], yuv[topOffset + 1], yuv[topOffset + 2]),
                Avg3(yuv[topOffset + 1], yuv[topOffset + 2], yuv[topOffset + 3]),
                Avg3(yuv[topOffset + 2], yuv[topOffset + 3], yuv[topOffset + 4])
            };

            for (int i = 0; i < 4; ++i)
            {
                vals.CopyTo(dst.Slice(i * WebPConstants.Bps));
            }
        }

        public static void HE4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // horizontal
            byte A = yuv[offset - 1 - WebPConstants.Bps];
            byte B = yuv[offset - 1];
            byte C = yuv[offset - 1 + WebPConstants.Bps];
            byte D = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte E = yuv[offset - 1 + (3 * WebPConstants.Bps)];
            uint val = 0x01010101U * Avg3(A, B, C);
            BinaryPrimitives.WriteUInt32BigEndian(dst, val);
            val = 0x01010101U * Avg3(B, C, D);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(WebPConstants.Bps), val);
            val = 0x01010101U * Avg3(C, D, E);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(2 * WebPConstants.Bps), val);
            val = 0x01010101U * Avg3(D, E, E);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(3 * WebPConstants.Bps), val);
        }

        public static void RD4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Down-right
            byte I = yuv[offset - 1];
            byte J = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte K = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte L = yuv[offset - 1 + (3 * WebPConstants.Bps)];
            byte X = yuv[offset - 1 - WebPConstants.Bps];
            byte A = yuv[offset - WebPConstants.Bps];
            byte B = yuv[offset + 1 - WebPConstants.Bps];
            byte C = yuv[offset + 2 - WebPConstants.Bps];
            byte D = yuv[offset + 3 - WebPConstants.Bps];

            Dst(dst, 0, 3, Avg3(J, K, L));
            byte ijk = Avg3(I, J, K);
            Dst(dst, 1, 3, ijk);
            Dst(dst, 0, 2, ijk);
            byte xij = Avg3(X, I, J);
            Dst(dst, 2, 3, xij);
            Dst(dst, 1, 2, xij);
            Dst(dst, 0, 1, xij);
            byte axi = Avg3(A, X, I);
            Dst(dst, 3, 3, axi);
            Dst(dst, 2, 2, axi);
            Dst(dst, 1, 1, axi);
            Dst(dst, 0, 0, axi);
            byte bax = Avg3(B, A, X);
            Dst(dst, 3, 2, bax);
            Dst(dst, 2, 1, bax);
            Dst(dst, 1, 0, bax);
            byte cba = Avg3(C, B, A);
            Dst(dst, 3, 1, cba);
            Dst(dst, 2, 0, cba);
            Dst(dst, 3, 0, Avg3(D, C, B));
        }

        public static void VR4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Vertical-Right
            byte I = yuv[offset - 1];
            byte J = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte K = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte X = yuv[offset - 1 - WebPConstants.Bps];
            byte A = yuv[offset - WebPConstants.Bps];
            byte B = yuv[offset + 1 - WebPConstants.Bps];
            byte C = yuv[offset + 2 - WebPConstants.Bps];
            byte D = yuv[offset + 3 - WebPConstants.Bps];

            byte xa = Avg2(X, A);
            Dst(dst, 0, 0, xa);
            Dst(dst, 1, 2, xa);
            byte ab = Avg2(A, B);
            Dst(dst, 1, 0, ab);
            Dst(dst, 2, 2, ab);
            byte bc = Avg2(B, C);
            Dst(dst, 2, 0, bc);
            Dst(dst, 3, 2, bc);
            Dst(dst, 3, 0, Avg2(C, D));
            Dst(dst, 0, 3, Avg3(K, J, I));
            Dst(dst, 0, 2, Avg3(J, I, X));
            byte ixa = Avg3(I, X, A);
            Dst(dst, 0, 1, ixa);
            Dst(dst, 1, 3, ixa);
            byte xab = Avg3(X, A, B);
            Dst(dst, 1, 1, xab);
            Dst(dst, 2, 3, xab);
            byte abc = Avg3(A, B, C);
            Dst(dst, 2, 1, abc);
            Dst(dst, 3, 3, abc);
            Dst(dst, 3, 1, Avg3(B, C, D));
        }

        public static void LD4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Down-Left
            byte A = yuv[offset - WebPConstants.Bps];
            byte B = yuv[offset + 1 - WebPConstants.Bps];
            byte C = yuv[offset + 2 - WebPConstants.Bps];
            byte D = yuv[offset + 3 - WebPConstants.Bps];
            byte E = yuv[offset + 4 - WebPConstants.Bps];
            byte F = yuv[offset + 5 - WebPConstants.Bps];
            byte G = yuv[offset + 6 - WebPConstants.Bps];
            byte H = yuv[offset + 7 - WebPConstants.Bps];

            Dst(dst, 0, 0, Avg3(A, B, C));
            byte bcd = Avg3(B, C, D);
            Dst(dst, 1, 0, bcd);
            Dst(dst, 0, 1, bcd);
            byte cde = Avg3(C, D, E);
            Dst(dst, 2, 0, cde);
            Dst(dst, 1, 1, cde);
            Dst(dst, 0, 2, cde);
            byte def = Avg3(D, E, F);
            Dst(dst, 3, 0, def);
            Dst(dst, 2, 1, def);
            Dst(dst, 1, 2, def);
            Dst(dst, 0, 3, def);
            byte efg = Avg3(E, F, G);
            Dst(dst, 3, 1, efg);
            Dst(dst, 2, 2, efg);
            Dst(dst, 1, 3, efg);
            byte fgh = Avg3(F, G, H);
            Dst(dst, 3, 2, fgh);
            Dst(dst, 2, 3, fgh);
            Dst(dst, 3, 3, Avg3(G, H, H));
        }

        public static void VL4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Vertical-Left
            byte A = yuv[offset - WebPConstants.Bps];
            byte B = yuv[offset + 1 - WebPConstants.Bps];
            byte C = yuv[offset + 2 - WebPConstants.Bps];
            byte D = yuv[offset + 3 - WebPConstants.Bps];
            byte E = yuv[offset + 4 - WebPConstants.Bps];
            byte F = yuv[offset + 5 - WebPConstants.Bps];
            byte G = yuv[offset + 6 - WebPConstants.Bps];
            byte H = yuv[offset + 7 - WebPConstants.Bps];

            Dst(dst, 0, 0, Avg2(A, B));
            byte bc = Avg2(B, C);
            Dst(dst, 1, 0, bc);
            Dst(dst, 0, 2, bc);
            byte cd = Avg2(C, D);
            Dst(dst, 2, 0, cd);
            Dst(dst, 1, 2, cd);
            byte de = Avg2(D, E);
            Dst(dst, 3, 0, de);
            Dst(dst, 2, 2, de);
            Dst(dst, 0, 1, Avg3(A, B, C));
            byte bcd = Avg3(B, C, D);
            Dst(dst, 1, 1, bcd);
            Dst(dst, 0, 3, bcd);
            byte cde = Avg3(C, D, E);
            Dst(dst, 2, 1, cde);
            Dst(dst, 1, 3, cde);
            byte def = Avg3(D, E, F);
            Dst(dst, 3, 1, def);
            Dst(dst, 2, 3, def);
            Dst(dst, 3, 2, Avg3(E, F, G));
            Dst(dst, 3, 3, Avg3(F, G, H));
        }

        public static void HD4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Horizontal-Down
            byte I = yuv[offset - 1];
            byte J = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte K = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte L = yuv[offset - 1 + (3 * WebPConstants.Bps)];
            byte X = yuv[offset - 1 - WebPConstants.Bps];
            byte A = yuv[offset - WebPConstants.Bps];
            byte B = yuv[offset + 1 - WebPConstants.Bps];
            byte C = yuv[offset + 2 - WebPConstants.Bps];

            byte ix = Avg2(I, X);
            Dst(dst, 0, 0, ix);
            Dst(dst, 2, 1, ix);
            byte ji = Avg2(J, I);
            Dst(dst, 0, 1, ji);
            Dst(dst, 2, 2, ji);
            byte kj = Avg2(K, J);
            Dst(dst, 0, 2, kj);
            Dst(dst, 2, 3, kj);
            Dst(dst, 0, 3, Avg2(L, K));
            Dst(dst, 3, 0, Avg3(A, B, C));
            Dst(dst, 2, 0, Avg3(X, A, B));
            byte ixa = Avg3(I, X, A);
            Dst(dst, 1, 0, ixa);
            Dst(dst, 3, 1, ixa);
            byte jix = Avg3(J, I, X);
            Dst(dst, 1, 1, jix);
            Dst(dst, 3, 2, jix);
            byte kji = Avg3(K, J, I);
            Dst(dst, 1, 2, kji);
            Dst(dst, 3, 3, kji);
            Dst(dst, 1, 3, Avg3(L, K, J));
        }

        public static void HU4_C(Span<byte> dst, byte[] yuv, int offset)
        {
            // Horizontal-Up
            byte I = yuv[offset - 1];
            byte J = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte K = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte L = yuv[offset - 1 + (3 * WebPConstants.Bps)];

            Dst(dst, 0, 0, Avg2(I, J));
            byte jk = Avg2(J, K);
            Dst(dst, 2, 0, jk);
            Dst(dst, 0, 1, jk);
            byte kl = Avg2(K, L);
            Dst(dst, 2, 1, kl);
            Dst(dst, 0, 2, kl);
            Dst(dst, 1, 0, Avg3(I, J, K));
            byte jkl = Avg3(J, K, L);
            Dst(dst, 3, 0, jkl);
            Dst(dst, 1, 1, jkl);
            byte kll = Avg3(K, L, L);
            Dst(dst, 3, 1, kll);
            Dst(dst, 1, 2, kll);
            Dst(dst, 3, 2, L);
            Dst(dst, 2, 2, L);
            Dst(dst, 0, 3, L);
            Dst(dst, 1, 3, L);
            Dst(dst, 2, 3, L);
            Dst(dst, 3, 3, L);
        }

        public static void Transform(Span<short> src, Span<byte> dst, bool doTwo)
        {
            TransformOne(src, dst);
            if (doTwo)
            {
                TransformOne(src.Slice(16), dst.Slice(4));
            }
        }

        public static void TransformOne(Span<short> src, Span<byte> dst)
        {
            var tmp = new int[4 * 4];
            int tmpOffset = 0;
            int srcOffset = 0;
            for (int i = 0; i < 4; ++i)
            {
                // vertical pass
                int a = src[srcOffset] + src[srcOffset + 8];
                int b = src[srcOffset] - src[srcOffset + 8];
                int c = Mul2(src[srcOffset + 4]) - Mul1(src[srcOffset + 12]);
                int d = Mul1(src[srcOffset + 4]) + Mul2(src[srcOffset + 12]);
                tmp[tmpOffset] = a + d;
                tmp[tmpOffset + 1] = b + c;
                tmp[tmpOffset + 2] = b - c;
                tmp[tmpOffset + 3] = a - d;
                tmpOffset += 4;
                srcOffset++;
            }

            // Each pass is expanding the dynamic range by ~3.85 (upper bound).
            // The exact value is (2. + (20091 + 35468) / 65536).
            // After the second pass, maximum interval is [-3794, 3794], assuming
            // an input in [-2048, 2047] interval. We then need to add a dst value in the [0, 255] range.
            // In the worst case scenario, the input to clip_8b() can be as large as [-60713, 60968].
            tmpOffset = 0;
            for (int i = 0; i < 4; ++i)
            {
                // horizontal pass
                int dc = tmp[tmpOffset] + 4;
                int a = dc + tmp[tmpOffset + 8];
                int b = dc - tmp[tmpOffset + 8];
                int c = Mul2(tmp[tmpOffset + 4]) - Mul1(tmp[tmpOffset + 12]);
                int d = Mul1(tmp[tmpOffset + 4]) + Mul2(tmp[tmpOffset + 12]);
                Store(dst, 0, 0, a + d);
                Store(dst, 1, 0, b + c);
                Store(dst, 2, 0, b - c);
                Store(dst, 3, 0, a - d);
                tmpOffset++;
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        public static void TransformDc(Span<short> src, Span<byte> dst)
        {
            int dc = src[0] + 4;
            for (int j = 0; j < 4; ++j)
            {
                for (int i = 0; i < 4; ++i)
                {
                    Store(dst, i, j, dc);
                }
            }
        }

        // Simplified transform when only src[0], src[1] and src[4] are non-zero
        public static void TransformAc3(Span<short> src, Span<byte> dst)
        {
            int a = src[0] + 4;
            int c4 = Mul2(src[4]);
            int d4 = Mul1(src[4]);
            int c1 = Mul2(src[1]);
            int d1 = Mul1(src[1]);
            Store2(dst, 0, a + d4, d1, c1);
            Store2(dst, 1, a + c4, d1, c1);
            Store2(dst, 2, a - c4, d1, c1);
            Store2(dst, 3, a - d4, d1, c1);
        }

        public static void TransformUv(Span<short> src, Span<byte> dst)
        {
            Transform(src.Slice(0 * 16), dst, true);
            Transform(src.Slice(2 * 16), dst.Slice(4 * WebPConstants.Bps), true);
        }

        public static void TransformDcuv(Span<short> src, Span<byte> dst)
        {
            if (src[0 * 16] != 0)
            {
                TransformDc(src.Slice(0 * 16), dst);
            }

            if (src[1 * 16] != 0)
            {
                TransformDc(src.Slice(1 * 16), dst.Slice(4));
            }

            if (src[2 * 16] != 0)
            {
                TransformDc(src.Slice(2 * 16), dst.Slice(4 * WebPConstants.Bps));
            }

            if (src[3 * 16] != 0)
            {
                TransformDc(src.Slice(3 * 16), dst.Slice((4 * WebPConstants.Bps) + 4));
            }
        }

        private static void TrueMotion(Span<byte> dst, byte[] yuv, int offset, int size)
        {
            // For information about how true motion works, see rfc6386, page 52. ff and section 20.14.
            int topOffset = offset - WebPConstants.Bps;
            Span<byte> top = yuv.AsSpan(topOffset);
            byte p = yuv[topOffset - 1];
            int leftOffset = offset - 1;
            byte left = yuv[leftOffset];
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    dst[x] = (byte)Clamp255(left + top[x] - p);
                }

                leftOffset += WebPConstants.Bps;
                left = yuv[leftOffset];
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        // Simple In-loop filtering (Paragraph 15.2)
        public static void SimpleVFilter16(byte[] p, int offset, int stride, int thresh)
        {
            int thresh2 = (2 * thresh) + 1;
            for (int i = 0; i < 16; ++i)
            {
                if (NeedsFilter(p, offset + i, stride, thresh2))
                {
                    DoFilter2(p, offset + i, stride);
                }
            }
        }

        public static void SimpleHFilter16(byte[] p, int offset, int stride, int thresh)
        {
            int thresh2 = (2 * thresh) + 1;
            for (int i = 0; i < 16; ++i)
            {
                if (NeedsFilter(p, offset + (i * stride), 1, thresh2))
                {
                    DoFilter2(p, offset + (i * stride), 1);
                }
            }
        }

        public static void SimpleVFilter16i(byte[] p, int offset, int stride, int thresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4 * stride;
                SimpleVFilter16(p, offset,  stride, thresh);
            }
        }

        public static void SimpleHFilter16i(byte[] p, int offset, int stride, int thresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += stride;
                SimpleHFilter16(p, offset, stride, thresh);
            }
        }

        public static void VFilter16(byte[] p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(p, offset, stride, 1, 16, thresh, ithresh, hevThresh);
        }

        public static void HFilter16(byte[] p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(p, offset, 1, stride, 16, thresh, ithresh, hevThresh);
        }

        public static void VFilter16i(byte[] p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4 * stride;
                FilterLoop24(p, offset, stride, 1, 16, thresh, ithresh, hevThresh);
            }
        }

        public static void HFilter16i(byte[] p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4;
                FilterLoop24(p, offset, 1, stride, 16, thresh, ithresh, hevThresh);
            }
        }

        // 8-pixels wide variant, for chroma filtering.
        public static void VFilter8(byte[] u, byte[] v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(u, offset, stride, 1, 8, thresh, ithresh, hevThresh);
            FilterLoop26(v, offset, stride, 1, 8, thresh, ithresh, hevThresh);
        }

        public static void HFilter8(byte[] u, byte[] v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(u, offset, 1, stride, 8, thresh, ithresh, hevThresh);
            FilterLoop26(v, offset, 1, stride, 8, thresh, ithresh, hevThresh);
        }

        public static void VFilter8i(byte[] u, byte[] v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop24(u, offset + (4 * stride), stride, 1, 8, thresh, ithresh, hevThresh);
            FilterLoop24(v, offset + (4 * stride), stride, 1, 8, thresh, ithresh, hevThresh);
        }

        public static void HFilter8i(byte[] u, byte[] v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop24(u, offset + 4, 1, stride, 8, thresh, ithresh, hevThresh);
            FilterLoop24(v, offset + 4, 1, stride, 8, thresh, ithresh, hevThresh);
        }

        public static uint LoadUv(byte u, byte v)
        {
            // We process u and v together stashed into 32bit(16bit each).
            return (uint)(u | (v << 16));
        }

        public static void YuvToBgr(int y, int u, int v, Span<byte> bgr)
        {
            bgr[0] = (byte)YuvToB(y, u);
            bgr[1] = (byte)YuvToG(y, u, v);
            bgr[2] = (byte)YuvToR(y, v);
        }

        public static int YuvToR(int y, int v)
        {
            return Clip8(MultHi(y, 19077) + MultHi(v, 26149) - 14234);
        }

        public static int YuvToG(int y, int u, int v)
        {
            return Clip8(MultHi(y, 19077) - MultHi(u, 6419) - MultHi(v, 13320) + 8708);
        }

        public static int YuvToB(int y, int u)
        {
            return Clip8(MultHi(y, 19077) + MultHi(u, 33050) - 17685);
        }

        public static void Memset(Span<uint> dst, uint value, int startIdx, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dst[startIdx + i] = value;
            }
        }

        // Complex In-loop filtering (Paragraph 15.3)
        private static void FilterLoop24(
            byte[] p,
            int offset,
            int hStride,
            int vStride,
            int size,
            int thresh,
            int ithresh,
            int hevThresh)
        {
            int thresh2 = (2 * thresh) + 1;
            while (size-- > 0)
            {
                if (NeedsFilter2(p, offset,  hStride, thresh2, ithresh))
                {
                    if (Hev(p, offset, hStride, hevThresh))
                    {
                        DoFilter2(p, offset, hStride);
                    }
                    else
                    {
                        DoFilter4(p, offset, hStride);
                    }
                }

                offset += vStride;
            }
        }

        private static void FilterLoop26(
            byte[] p,
            int offset,
            int hStride,
            int vStride,
            int size,
            int thresh,
            int ithresh,
            int hevThresh)
        {
            int thresh2 = (2 * thresh) + 1;
            while (size-- > 0)
            {
                if (NeedsFilter2(p, offset, hStride, thresh2, ithresh))
                {
                    if (Hev(p, offset, hStride, hevThresh))
                    {
                        DoFilter2(p, offset, hStride);
                    }
                    else
                    {
                        DoFilter6(p, offset, hStride);
                    }
                }

                offset += vStride;
            }
        }

        private static void DoFilter2(byte[] p, int offset, int step)
        {
            // 4 pixels in, 2 pixels out
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int a = (3 * (q0 - p0)) + Vp8LookupTables.Sclip1(p1 - q1);
            int a1 = Vp8LookupTables.Sclip2((a + 4) >> 3);
            int a2 = Vp8LookupTables.Sclip2((a + 3) >> 3);
            p[offset - step] = Vp8LookupTables.Clip1(p0 + a2);
            p[offset] = Vp8LookupTables.Clip1(q0 - a1);
        }

        private static void DoFilter4(byte[] p, int offset, int step)
        {
            // 4 pixels in, 4 pixels out
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int a = 3 * (q0 - p0);
            int a1 = Vp8LookupTables.Sclip2((a + 4) >> 3);
            int a2 = Vp8LookupTables.Sclip2((a + 3) >> 3);
            int a3 = (a1 + 1) >> 1;
            p[offset - (2 * step)] = Vp8LookupTables.Clip1(p1 + a3);
            p[offset - step] = Vp8LookupTables.Clip1(p0 + a2);
            p[offset] = Vp8LookupTables.Clip1(q0 - a1);
            p[offset + step] = Vp8LookupTables.Clip1(q1 - a3);
        }

        private static void DoFilter6(byte[] p, int offset, int step)
        {
            // 6 pixels in, 6 pixels out
            int p2 = p[offset - (3 * step)];
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int q2 = p[offset + (2 * step)];
            int a = Vp8LookupTables.Clip1((3 * (q0 - p0)) + Vp8LookupTables.Clip1(p1 - q1));

            // a is in [-128,127], a1 in [-27,27], a2 in [-18,18] and a3 in [-9,9]
            int a1 = ((27 * a) + 63) >> 7;  // eq. to ((3 * a + 7) * 9) >> 7
            int a2 = ((18 * a) + 63) >> 7;  // eq. to ((2 * a + 7) * 9) >> 7
            int a3 = ((9 * a) + 63) >> 7;  // eq. to ((1 * a + 7) * 9) >> 7
            p[offset - (3 * step)] = Vp8LookupTables.Clip1(p2 + a3);
            p[offset - (2 * step)] = Vp8LookupTables.Clip1(p1 + a2);
            p[offset - step] = Vp8LookupTables.Clip1(p0 + a1);
            p[offset] = Vp8LookupTables.Clip1(q0 - a1);
            p[offset + step] = Vp8LookupTables.Clip1(q1 - a2);
            p[offset + (2 * step)] = Vp8LookupTables.Clip1(q2 - a3);
        }

        private static bool NeedsFilter(byte[] p, int offset, int step, int thresh)
        {
            int p1 = p[offset + (-2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            return (Vp8LookupTables.Abs0(p1 - p0) > thresh) || (Vp8LookupTables.Abs0(q1 - q0) > thresh);
        }

        private static bool NeedsFilter2(byte[] p, int offset, int step, int t, int it)
        {
            int p3 = p[offset - (4 * step)];
            int p2 = p[offset - (3 * step)];
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int q2 = p[offset + (2 * step)];
            int q3 = p[offset + (3 * step)];
            if (((4 * Vp8LookupTables.Abs0(p0 - q0)) + Vp8LookupTables.Abs0(p1 - q1)) > t)
            {
                return false;
            }

            return Vp8LookupTables.Abs0(p3 - p2) <= it && Vp8LookupTables.Abs0(p2 - p1) <= it &&
                   Vp8LookupTables.Abs0(p1 - p0) <= it && Vp8LookupTables.Abs0(q3 - q2) <= it &&
                   Vp8LookupTables.Abs0(q2 - q1) <= it && Vp8LookupTables.Abs0(q1 - q0) <= it;
        }

        private static bool Hev(byte[] p, int offset, int step, int thresh)
        {
            int p1 = p[offset -(2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            return (Vp8LookupTables.Abs0(p1 - p0) > thresh) || (Vp8LookupTables.Abs0(q1 - q0) > thresh);
        }

        private static int MultHi(int v, int coeff)
        {
            return (v * coeff) >> 8;
        }

        private static void Store(Span<byte> dst, int x, int y, int v)
        {
            dst[x + (y * WebPConstants.Bps)] = Clip8B(dst[x + (y * WebPConstants.Bps)] + (v >> 3));
        }

        private static void Store2(Span<byte> dst, int y, int dc, int d, int c)
        {
            Store(dst, 0, y, dc + d);
            Store(dst, 1, y, dc + c);
            Store(dst, 2, y, dc - c);
            Store(dst, 3, y, dc - d);
        }

        private static int Mul1(int a)
        {
            return ((a * 20091) >> 16) + a;
        }

        private static int Mul2(int a)
        {
            return (a * 35468) >> 16;
        }

        private static byte Clip8B(int v)
        {
            return (byte)((v & ~0xff) is 0 ? v : (v < 0) ? 0 : 255);
        }

        private static byte Clip8(int v)
        {
            int yuvMask = (256 << 6) - 1;
            return (byte)(((v & ~yuvMask) is 0) ? (v >> 6) : (v < 0) ? 0 : 255);
        }

        private static void Put8x8uv(byte value, Span<byte> dst)
        {
            for (int j = 0; j < 8; ++j)
            {
                // memset(dst + j * BPS, value, 8);
                Memset(dst, value, j * WebPConstants.Bps, 8);
            }
        }

        private static void Memset(Span<byte> dst, byte value, int startIdx, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dst[startIdx + i] = value;
            }
        }

        private static byte Avg2(byte a, byte b)
        {
            return (byte)((a + b + 1) >> 1);
        }

        private static byte Avg3(byte a, byte b, byte c)
        {
            return (byte)((a + (2 * b) + c + 2) >> 2);
        }

        private static void Dst(Span<byte> dst, int x, int y, byte v)
        {
            dst[x + (y * WebPConstants.Bps)] = v;
        }

        private static int Clamp255(int x)
        {
            return x < 0 ? 0 : (x > 255 ? 255 : x);
        }
    }
}
