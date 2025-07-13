/*
* 
*  DdsImage.cs - DDS Texture File Reading (Uncompressed, DXT1/2/3/4/5, V8U8) and Writing (Uncompressed Only)
*  
*  By Shendare (Jon D. Jackson)
* 
*  Rebuilt from Microsoft DDS documentation with the help of the DDSImage.cs reading class from
*  Lorenzo Consolaro, under the MIT License.  https://code.google.com/p/kprojects/ 
* 
*  Portions of this code not covered by another author's or entity's copyright are released under
*  the Creative Commons Zero (CC0) public domain license.
*  
*  To the extent possible under law, Shendare (Jon D. Jackson) has waived all copyright and
*  related or neighboring rights to this DdsImage class. This work is published from: The United States. 
*  
*  You may copy, modify, and distribute the work, even for commercial purposes, without asking permission.
* 
*  For more information, read the CC0 summary and full legal text here:
*  
*  https://creativecommons.org/publicdomain/zero/1.0/
* 
*/

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace FreeRealmsMapMaker.Dds;

[SupportedOSPlatform("windows")]
public class DdsImage
{
    #region Constants and Bitflags

    private const uint MAGIC_NUMBER = 0x20534444;

    private const uint DDPF_ALPHAPIXELS = 0x00000001;
    private const uint DDPF_ALPHA = 0x00000002; // Alpha channel only. Deprecated.
    private const uint DDPF_FOURCC = 0x00000004;
    private const uint DDPF_RGB = 0x00000040;
    private const uint DDPF_YUV = 0x00000200;
    private const uint DDPF_LUMINANCE = 0x00020000;

    private const int DDSD_CAPS = 0x00000001;
    private const int DDSD_HEIGHT = 0x00000002;
    private const int DDSD_WIDTH = 0x00000004;
    private const int DDSD_PITCH = 0x00000008;
    private const int DDSD_PIXELFORMAT = 0x00001000;
    private const int DDSD_MIPMAPCOUNT = 0x00020000;
    private const int DDSD_LINEARSIZE = 0x00080000;
    private const int DDSD_DEPTH = 0x00800000;

    private const int DDSCAPS_COMPLEX = 0x00000008;
    private const int DDSCAPS_TEXTURE = 0x00001000;
    private const int DDSCAPS_MIPMAP = 0x00400000;

    private const int DDSCAPS2_CUBEMAP = 0x00000200;
    private const int DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
    private const int DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
    private const int DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
    private const int DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
    private const int DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
    private const int DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
    private const int DDSCAPS2_VOLUME = 0x00200000;

    private const uint FOURCC_DXT1 = 0x31545844;
    private const uint FOURCC_DXT2 = 0x32545844;
    private const uint FOURCC_DXT3 = 0x33545844;
    private const uint FOURCC_DXT4 = 0x34545844;
    private const uint FOURCC_DXT5 = 0x35545844;
    private const uint FOURCC_DX10 = 0x30315844;
    private const uint FOURCC_V8U8 = 0X38553856; // Only used internally

    #endregion

    public DDS_HEADER Header;
    //public DDS_HEADER_DXT10 Header10;
    public DDS_PIXELFORMAT PixelFormat;
    public Bitmap[] Images;
    public int MipMapCount;
    public CompressionMode Format;

    public string FormatName => Format switch
    {
        CompressionMode.A1R5G5B5 => "ARGB16",
        CompressionMode.R5G6B5 => "RGB16",
        _ => Format.ToString(),
    };

    public DdsImage(string path) : this(File.OpenRead(path))
    {
    }

    public DdsImage(byte[] data) : this(new MemoryStream(data))
    {
    }

    public DdsImage(Stream stream)
    {
        using BinaryReader data = new(stream);

        if (data.ReadInt32() != MAGIC_NUMBER)
        {
            throw new IOException($"{nameof(DdsImage)}({nameof(Stream)}) requires a .dds texture stream.");
        }

        Format = CompressionMode.Unknown;

        Header.dwSize = data.ReadInt32();
        Header.dwFlags = data.ReadInt32();
        Header.dwHeight = data.ReadInt32();
        Header.dwWidth = data.ReadInt32();
        Header.dwPitchOrLinearSize = data.ReadInt32();
        Header.dwDepth = data.ReadInt32();
        Header.dwMipMapCount = data.ReadInt32();

        // Unused Reserved1 Fields
        data.ReadBytes(11 * sizeof(int));

        // Image Pixel Format
        PixelFormat.dwSize = data.ReadUInt32();
        PixelFormat.dwFlags = data.ReadUInt32();
        PixelFormat.dwFourCC = data.ReadUInt32();
        PixelFormat.dwRGBBitCount = data.ReadUInt32();
        PixelFormat.dwRBitMask = data.ReadUInt32();
        PixelFormat.dwGBitMask = data.ReadUInt32();
        PixelFormat.dwBBitMask = data.ReadUInt32();
        PixelFormat.dwABitMask = data.ReadUInt32();

        Header.dwCaps = data.ReadInt32();
        Header.dwCaps2 = data.ReadInt32();
        Header.dwCaps3 = data.ReadInt32();
        Header.dwCaps4 = data.ReadInt32();
        Header.dwReserved2 = data.ReadInt32();

        if ((PixelFormat.dwFlags & DDPF_FOURCC) != 0)
        {
            switch (PixelFormat.dwFourCC)
            {
                case FOURCC_DX10:
                    Format = CompressionMode.DX10;
                    throw new IOException("DX10 textures not supported at this time.");
                case FOURCC_DXT1:
                    Format = CompressionMode.DXT1;
                    break;
                case FOURCC_DXT2:
                    Format = CompressionMode.DXT2;
                    break;
                case FOURCC_DXT3:
                    Format = CompressionMode.DXT3;
                    break;
                case FOURCC_DXT4:
                    Format = CompressionMode.DXT4;
                    break;
                case FOURCC_DXT5:
                    Format = CompressionMode.DXT5;
                    break;
                default:
                    switch (PixelFormat.dwFourCC)
                    {
                        default:
                            break;
                    }
                    throw new IOException("Unsupported compression format.");
            }
        }

        if ((PixelFormat.dwFlags & DDPF_FOURCC) == 0)
        {
            // Uncompressed. How many BPP?

            bool supportedBpp = false;

            switch (PixelFormat.dwRGBBitCount)
            {
                case 16:
                    if (PixelFormat.dwABitMask == 0)
                    {
                        Format = CompressionMode.R5G6B5;
                    }
                    else
                    {
                        Format = CompressionMode.A1R5G5B5;
                    }
                    supportedBpp = true;
                    break;
                case 24:
                    Format = CompressionMode.RGB24;
                    supportedBpp = true;
                    break;
                case 32:
                    Format = CompressionMode.RGB32;
                    supportedBpp = true;
                    break;
            }

            if (!supportedBpp)
            {
                throw new Exception("Only 16, 24, and 32-bit pixel formats are supported for uncompressed textures.");
            }
        }

        MipMapCount = 1;

        if ((Header.dwFlags & DDSD_MIPMAPCOUNT) != 0)
        {
            MipMapCount = Header.dwMipMapCount == 0 ? 1 : Header.dwMipMapCount;
        }

        Images = new Bitmap[MipMapCount];

        int imageSize;
        int w = Header.dwWidth < 0 ? -Header.dwWidth : Header.dwWidth;
        int h = Header.dwHeight < 0 ? -Header.dwHeight : Header.dwHeight;

        // DDS Documentation recommends ignoring the dwLinearOrPitchSize value and calculating on your own.
        if ((PixelFormat.dwFlags & DDPF_RGB) != 0)
        {
            // Linear Size
            imageSize = w * h * ((int)PixelFormat.dwRGBBitCount + 7) >> 3;
        }
        else
        {
            // Compressed
            imageSize = (w + 3 >> 2) * (h + 3 >> 2);

            switch (PixelFormat.dwFourCC)
            {
                case FOURCC_DXT1:
                    imageSize <<= 3; // 64 bits color per block
                    break;
                case FOURCC_DXT2:
                case FOURCC_DXT3:
                    imageSize <<= 4; // 64 bits alpha + 64 bits color per block
                    break;
                case FOURCC_DXT4:
                case FOURCC_DXT5:
                    imageSize <<= 4; // 64 bits alpha + 64 bits color per block
                    break;
            }
        }

        byte[] imageBits;

        for (int level = 0; level < MipMapCount; level++)
        {
            try
            {
                imageBits = data.ReadBytes(imageSize >> (level << 1));

                int w2 = w >> level;
                int h2 = h >> level;

                uint compressionMode = PixelFormat.dwFourCC;

                if ((PixelFormat.dwFlags & DDPF_RGB) != 0)
                {
                    compressionMode = (uint)Format;
                }
                else if ((PixelFormat.dwFlags & DDPF_FOURCC) == 0 &&
                          PixelFormat.dwRGBBitCount == 16 &&
                          PixelFormat.dwRBitMask == 0x00FF &&
                          PixelFormat.dwGBitMask == 0xFF00 &&
                          PixelFormat.dwBBitMask == 0x0000 &&
                          PixelFormat.dwABitMask == 0x0000)
                {
                    Format = CompressionMode.V8U8;
                    compressionMode = FOURCC_V8U8;
                }

                Images[level] = Decompress.Image(imageBits, w2, h2, compressionMode);
            }
            catch
            {
                // Unexpected end of file. Perhaps mipmaps weren't fully written to file.
                // We'll at least provide them with what we've extracted so far.

                MipMapCount = level;

                if (level == 0)
                {
                    throw new IOException("Unable to read pixel data.");
                }
                else
                {
                    Array.Resize(ref Images, level);
                }
            }
        }
    }

    private class Decompress
    {
        public static Bitmap Image(byte[] data, int w, int h, uint compressionMode)
        {
            Bitmap img = new(w < 4 ? 4 : w, h < 4 ? 4 : h);

            switch (compressionMode)
            {
                case 15:
                case 16:
                case 24:
                case 32:
                    return Linear(data, w, h, compressionMode);
            }

            // https://msdn.microsoft.com/en-us/library/bb147243%28v=vs.85%29.aspx

            // Gain direct access to the surface's bits
            BitmapData bits = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                           ImageLockMode.WriteOnly,
                                           System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            nint bitPtr = bits.Scan0;

            // Convert byte[] data into 16-bit ushorts per Microsoft design/documentation
            ushort[] bpp16 = new ushort[data.Length >> 1];
            Buffer.BlockCopy(data, 0, bpp16, 0, data.Length);

            // Checking for negative stride per documentation just to be safe,
            // but I don't think bottom-up format is supported with DXT1.
            // Converting from bytes to ushorts for bpp16
            int stride = (bits.Stride < 0 ? -bits.Stride : bits.Stride) >> 2;

            // Our actual pixel data as it is decompressed
            int[]? pixels = new int[stride * bits.Height];

            // Decompress the blocks
            switch (compressionMode)
            {
                case FOURCC_DXT1:
                    DXT1(bpp16, pixels, w, h, stride);
                    break;

                case FOURCC_DXT2:
                case FOURCC_DXT3:
                    DXT3(bpp16, pixels, w, h, stride);
                    break;

                case FOURCC_DXT4:
                case FOURCC_DXT5:
                    DXT5(bpp16, pixels, w, h, stride);
                    break;

                case FOURCC_V8U8:
                    V8U8(bpp16, pixels, w, h, stride);
                    break;

                default:
                    pixels = null;
                    break;
            }

            // Copy our decompressed bits back into the surface
            if (pixels != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitPtr, stride * bits.Height);
            }

            // We're done!
            img.UnlockBits(bits);

            if (pixels == null)
            {
                throw new IOException(string.Format("DDS compression Mode '{0}{1}{2}{3}' not supported.",
                                                    (char)(compressionMode & 0xFF),
                                                    (char)(compressionMode >> 8 & 0xFF),
                                                    (char)(compressionMode >> 16 & 0xFF),
                                                    (char)(compressionMode >> 24 & 0xFF)));
            }

            return img;
        }

        private static void DXT1(ushort[] data, int[] pixels, int w, int h, int stride)
        {
            uint[] color = new uint[4];
            int pos = 0;
            int stride2 = stride - 4;

            for (int y = 0; y < h; y += 4)
            {
                for (int x = 0; x < w; x += 4)
                {
                    ushort c1 = data[pos++];
                    ushort c2 = data[pos++];

                    bool isAlpha = c1 < c2;

                    uint r1 = (byte)(c1 >> 11 & 0x1F);
                    uint g1 = (byte)((c1 & 0x07E0) >> 5);
                    uint b1 = (byte)(c1 & 0x001F);

                    uint r2 = (byte)(c2 >> 11 & 0x1F);
                    uint g2 = (byte)((c2 & 0x07E0) >> 5);
                    uint b2 = (byte)(c2 & 0x001F);

                    r1 = (r1 << 3) + (r1 >> 2);
                    g1 = (g1 << 2) + (g1 >> 4);
                    b1 = (b1 << 3) + (b1 >> 2);

                    r2 = (r2 << 3) + (r2 >> 2);
                    g2 = (g2 << 2) + (g2 >> 4);
                    b2 = (b2 << 3) + (b2 >> 2);

                    uint a = unchecked((uint)(0xFF << 24));

                    if (isAlpha)
                    {
                        color[0] = a | r1 << 16 | g1 << 8 | b1;
                        color[1] = a | r2 << 16 | g2 << 8 | b2;
                        color[2] = a | r1 + r2 >> 1 << 16 | g1 + g2 >> 1 << 8 | b1 + b2 >> 1;
                        color[3] = 0x00000000; // Transparent pixel
                    }
                    else
                    {
                        color[0] = a | r1 << 16 | g1 << 8 | b1;
                        color[1] = a | r2 << 16 | g2 << 8 | b2;
                        color[2] = a | (r2 * 3 + r1 * 6) / 9 << 16 | (g2 * 3 + g1 * 6) / 9 << 8 | (b2 * 3 + b1 * 6) / 9;
                        color[3] = a | (r1 * 3 + r2 * 6) / 9 << 16 | (g1 * 3 + g2 * 6) / 9 << 8 | (b1 * 3 + b2 * 6) / 9;
                    }

                    int pixel = y * stride + x;

                    ushort code = data[pos++];

                    pixels[pixel++] = unchecked((int)color[code & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] = unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 14 & 0x03]);
                    pixel += stride2;

                    code = data[pos++];

                    pixels[pixel++] = unchecked((int)color[code & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] = unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] = unchecked((int)color[code >> 14 & 0x03]);
                }
            }
        }

        private static void DXT3(ushort[] data, int[] pixels, int w, int h, int stride)
        {
            ushort[] alpha = new ushort[4];
            int pos = 0;
            int stride2 = stride - 4;

            for (int y = 0; y < h; y += 4)
            {
                for (int x = 0; x < w; x += 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        alpha[i] = data[pos++];
                    }

                    int pixel = y * stride + x;

                    for (int i = 0; i < 4; i++)
                    {
                        pixels[pixel++] = (alpha[i] >> 0 & 0x000F) * 255 / 15 << 24;
                        pixels[pixel++] = (alpha[i] >> 4 & 0x000F) * 255 / 15 << 24;
                        pixels[pixel++] = (alpha[i] >> 8 & 0x000F) * 255 / 15 << 24;
                        pixels[pixel++] = (alpha[i] >> 12 & 0x000F) * 255 / 15 << 24;

                        pixel += stride2;
                    }

                    uint[] color = new uint[4];

                    ushort c1 = data[pos++];
                    ushort c2 = data[pos++];

                    uint r1 = (byte)(c1 >> 11 & 0x1F);
                    uint g1 = (byte)((c1 & 0x07E0) >> 5);
                    uint b1 = (byte)(c1 & 0x001F);

                    uint r2 = (byte)(c2 >> 11 & 0x1F);
                    uint g2 = (byte)((c2 & 0x07E0) >> 5);
                    uint b2 = (byte)(c2 & 0x001F);

                    r1 = (r1 << 3) + (r1 >> 2);
                    g1 = (g1 << 2) + (g1 >> 4);
                    b1 = (b1 << 3) + (b1 >> 2);

                    r2 = (r2 << 3) + (r2 >> 2);
                    g2 = (g2 << 2) + (g2 >> 4);
                    b2 = (b2 << 3) + (b2 >> 2);

                    color[0] = r1 << 16 | g1 << 8 | b1;
                    color[1] = r2 << 16 | g2 << 8 | b2;
                    color[2] = (r2 * 3 + r1 * 6) / 9 << 16 | (g2 * 3 + g1 * 6) / 9 << 8 | (b2 * 3 + b1 * 6) / 9;
                    color[3] = (r1 * 3 + r2 * 6) / 9 << 16 | (g1 * 3 + g2 * 6) / 9 << 8 | (b1 * 3 + b2 * 6) / 9;

                    pixel = y * stride + x;

                    ushort code = data[pos++];

                    pixels[pixel++] |= unchecked((int)color[code & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] |= unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 14 & 0x03]);
                    pixel += stride2;

                    code = data[pos++];

                    pixels[pixel++] |= unchecked((int)color[code & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] |= unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 14 & 0x03]);
                }
            }
        }

        private static void DXT5(ushort[] data, int[] pixels, int w, int h, int stride)
        {
            uint[] color = new uint[4];
            ushort[] alpha = new ushort[8];
            ushort[] alphaBits = new ushort[3];
            ushort alphaCode;
            int pos = 0;
            int stride2 = stride - 4;
            ushort code;

            for (int y = 0; y < h; y += 4)
            {
                for (int x = 0; x < w; x += 4)
                {
                    alpha[0] = (ushort)(data[pos] & 0xFF);
                    alpha[1] = (ushort)(data[pos++] >> 8);

                    if (alpha[0] > alpha[1])
                    {
                        // 8 alpha block
                        alpha[2] = (ushort)((6 * alpha[0] + 1 * alpha[1] + 3) / 7);
                        alpha[3] = (ushort)((5 * alpha[0] + 2 * alpha[1] + 3) / 7);
                        alpha[4] = (ushort)((4 * alpha[0] + 3 * alpha[1] + 3) / 7);
                        alpha[5] = (ushort)((3 * alpha[0] + 4 * alpha[1] + 3) / 7);
                        alpha[6] = (ushort)((2 * alpha[0] + 5 * alpha[1] + 3) / 7);
                        alpha[7] = (ushort)((1 * alpha[0] + 6 * alpha[1] + 3) / 7);
                    }
                    else
                    {
                        // 6 alpha block
                        alpha[2] = (ushort)((4 * alpha[0] + 1 * alpha[1] + 2) / 5);
                        alpha[3] = (ushort)((3 * alpha[0] + 2 * alpha[1] + 2) / 5);
                        alpha[4] = (ushort)((2 * alpha[0] + 3 * alpha[1] + 2) / 5);
                        alpha[5] = (ushort)((1 * alpha[0] + 4 * alpha[1] + 2) / 5);
                        alpha[6] = 0;
                        alpha[7] = 0xFF;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        alphaBits[i] = data[pos++];
                    }

                    int pixel = y * stride + x;

                    alphaCode = alphaBits[0];
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24;
                    pixel += stride2;

                    alphaCode = (ushort)(alphaBits[0] >> 12 | alphaBits[1] << 4);
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24;
                    pixel += stride2;

                    alphaCode = (ushort)(alphaBits[1] >> 8 | alphaBits[2] << 8);
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24;
                    pixel += stride2;

                    alphaCode = (ushort)(alphaBits[2] >> 4);
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24; alphaCode >>= 3;
                    pixels[pixel++] = alpha[alphaCode & 0x0007] << 24;

                    ushort c1 = data[pos++];
                    ushort c2 = data[pos++];

                    uint r1 = (byte)(c1 >> 11 & 0x1F);
                    uint g1 = (byte)((c1 & 0x07E0) >> 5);
                    uint b1 = (byte)(c1 & 0x001F);

                    uint r2 = (byte)(c2 >> 11 & 0x1F);
                    uint g2 = (byte)((c2 & 0x07E0) >> 5);
                    uint b2 = (byte)(c2 & 0x001F);

                    r1 = (r1 << 3) + (r1 >> 2);
                    g1 = (g1 << 2) + (g1 >> 4);
                    b1 = (b1 << 3) + (b1 >> 2);

                    r2 = (r2 << 3) + (r2 >> 2);
                    g2 = (g2 << 2) + (g2 >> 4);
                    b2 = (b2 << 3) + (b2 >> 2);

                    color[0] = r1 << 16 | g1 << 8 | b1;
                    color[1] = r2 << 16 | g2 << 8 | b2;
                    color[2] = (r2 * 1 + r1 * 2) / 3 << 16 | (g2 * 1 + g1 * 2) / 3 << 8 | (b2 * 1 + b1 * 2) / 3;
                    color[3] = (r1 * 1 + r2 * 2) / 3 << 16 | (g1 * 1 + g2 * 2) / 3 << 8 | (b1 * 1 + b2 * 2) / 3;

                    pixel = y * stride + x;

                    code = data[pos++];

                    pixels[pixel++] |= unchecked((int)color[code >> 0 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] |= unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 14 & 0x03]);
                    pixel += stride2;

                    code = data[pos++];

                    pixels[pixel++] |= unchecked((int)color[code >> 0 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 2 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 4 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 6 & 0x03]);
                    pixel += stride2;

                    pixels[pixel++] |= unchecked((int)color[code >> 8 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 10 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 12 & 0x03]);
                    pixels[pixel++] |= unchecked((int)color[code >> 14 & 0x03]);
                }
            }
        }

        private static void V8U8(ushort[] data, int[] pixels, int w, int h, int stride)
        {
            int pos = 0;

            for (int y = 0; y < h; y++)
            {
                int pixel = y * stride;

                for (int x = 0; x < w; x++)
                {
                    pixels[pixel++] = unchecked((int)(data[pos++] ^ 0xFFFFFFFF));
                }
            }
        }

        private static Bitmap Linear(byte[] data, int w, int h, uint bpp)
        {
            Bitmap img = new(w, h);

            int a, r, g, b, c, pos;

            BitmapData bits = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                           ImageLockMode.WriteOnly,
                                           System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            nint bitPtr = bits.Scan0;

            int stride = (bits.Stride < 0 ? -bits.Stride : bits.Stride) >> 2;
            int[] pixels = new int[stride * bits.Height];

            switch (bpp)
            {
                case (uint)CompressionMode.A1R5G5B5:
                    pos = 0;
                    for (int y = 0; y < h; y++)
                    {
                        int xy = y * (bits.Stride >> 2);

                        for (int x = 0; x < w; x++)
                        {
                            c = data[pos++];
                            c |= data[pos++] << 8;

                            a = (c & 0x8000) == 0 ? 0 : 0xFF;
                            r = ((c & 0x7C00) >> 10) * 255 / 31;
                            g = ((c & 0x03E0) >> 5) * 255 / 31;
                            b = (c & 0x001F) * 255 / 31;

                            pixels[xy++] = a << 24 | r << 16 | g << 8 | b;
                        }
                    }
                    break;
                case (uint)CompressionMode.R5G6B5:
                    pos = 0;
                    a = 0xFF << 24;

                    for (int y = 0; y < h; y++)
                    {
                        int xy = y * (bits.Stride >> 2);

                        for (int x = 0; x < w; x++)
                        {
                            c = data[pos++];
                            c |= data[pos++] << 8;

                            r = ((c & 0xF800) >> 11) * 255 / 31;
                            g = ((c & 0x07E0) >> 5) * 255 / 63;
                            b = (c & 0x001F) * 255 / 31;

                            pixels[xy++] = a | r << 16 | g << 8 | b;
                        }
                    }
                    break;
                case (uint)CompressionMode.RGB24:
                    a = 0xFF << 24;

                    using (BinaryReader reader = new(new MemoryStream(data)))
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);

                        for (int y = 0; y < img.Height; y++)
                        {
                            int xy = (bits.Stride < 0 ? img.Height - y : y) * stride;

                            for (int x = 0; x < img.Width; x++)
                            {
                                b = reader.ReadByte();
                                g = reader.ReadByte();
                                r = reader.ReadByte();

                                pixels[xy++] = a | r << 16 | g << 8 | b;
                            }
                        }
                    }
                    break;
                case (uint)CompressionMode.RGB32:
                    int[] bpp32 = new int[data.Length >> 2];
                    Buffer.BlockCopy(data, 0, bpp32, 0, data.Length);

                    if (stride == img.Width && bits.Stride > 0)
                    {
                        // Cohesive block of pixel data. No need to go row by row.
                        Array.Copy(bpp32, pixels, pixels.Length);
                    }
                    else
                    {
                        for (int y = 0; y < img.Height; y++)
                        {
                            // if Stride < 0, image is stored from the bottom up, so we have to invert our y
                            int xy1 = (bits.Stride < 0 ? img.Height - y : y) * stride;
                            int xy2 = y * w;

                            Array.Copy(bpp32, xy2, pixels, xy1, stride);
                        }
                    }
                    break;
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitPtr, stride * bits.Height);

            img.UnlockBits(bits);

            return img;
        }
    }

    public static bool Save(DdsImage image, string filename, CompressionMode format)
    {
        try
        {
            return Save(image.Images[0], filename, format);
        }
        catch
        {
            return false;
        }
    }

    public static bool Save(DdsImage image, Stream stream, CompressionMode format)
    {
        try
        {
            return Save(image.Images[0], stream, format);
        }
        catch
        {
            return false;
        }
    }

    public static bool Save(Bitmap picture, string filename, CompressionMode format)
    {
        try
        {
            using FileStream stream = File.OpenWrite(filename);
            return Save(picture, stream, format);
        }
        catch
        {
            return false;
        }
    }

    public static bool Save(Bitmap picture, Stream stream, CompressionMode cFormat)
    {
        if (picture == null || stream == null)
        {
            return false;
        }

        switch (cFormat)
        {
            case CompressionMode.A1R5G5B5:
                break;
            case CompressionMode.R5G6B5:
                break;
            case CompressionMode.RGB24:
                break;
            case CompressionMode.RGB32:
                break;
            default:
                return false;
        }

        List<Bitmap> mipMaps = [picture];

        try
        {
            while (true)
            {
                int w = picture.Width >> mipMaps.Count;
                int h = picture.Height >> mipMaps.Count;

                if (w < 4 || h < 4)
                {
                    break;
                }

                Bitmap map = new(w, h);

                using (Graphics blitter = Graphics.FromImage(map))
                {
                    blitter.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    using ImageAttributes wrapMode = new();
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);

                    blitter.DrawImage(picture,
                                      new Rectangle(0, 0, w, h),
                                      0,
                                      0,
                                      picture.Width,
                                      picture.Height,
                                      GraphicsUnit.Pixel,
                                      wrapMode);
                }

                mipMaps.Add(map);
            }

            DDS_HEADER header;
            DDS_PIXELFORMAT format;

            using BinaryWriter writer = new(stream);
            writer.Write(0x20534444); // Magic Number ("DDS ")

            uint hasAlpha = (picture.PixelFormat & System.Drawing.Imaging.PixelFormat.Alpha) != 0 ? 1u : 0u;

            format.dwSize = 32;
            format.dwFlags = DDPF_RGB | DDPF_ALPHAPIXELS * hasAlpha;
            format.dwFourCC = 0;

            switch (cFormat)
            {
                case CompressionMode.R5G6B5:
                    format.dwRGBBitCount = 16;
                    format.dwABitMask = 0x0000;
                    format.dwRBitMask = 0xF800;
                    format.dwGBitMask = 0x07E0;
                    format.dwBBitMask = 0x001F;
                    break;
                case CompressionMode.A1R5G5B5:
                    format.dwRGBBitCount = 16;
                    format.dwABitMask = 0x8000;
                    format.dwRBitMask = 0x7C00;
                    format.dwGBitMask = 0x03E0;
                    format.dwBBitMask = 0x001F;
                    break;
                case CompressionMode.RGB24:
                    format.dwRGBBitCount = 24;
                    format.dwABitMask = 0x00000000;
                    format.dwRBitMask = 0x00ff0000;
                    format.dwGBitMask = 0x0000ff00;
                    format.dwBBitMask = 0x000000ff;
                    break;
                case CompressionMode.RGB32:
                default:
                    format.dwRGBBitCount = 32;
                    format.dwABitMask = 0xff000000 * hasAlpha;
                    format.dwRBitMask = 0x00ff0000;
                    format.dwGBitMask = 0x0000ff00;
                    format.dwBBitMask = 0x000000ff;
                    break;
            }

            header.dwSize = 124;
            header.dwFlags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_MIPMAPCOUNT | DDSD_PITCH;
            header.dwHeight = picture.Height;
            header.dwWidth = picture.Width;
            header.dwPitchOrLinearSize = (int)(header.dwWidth * header.dwHeight * (format.dwRGBBitCount >> 3));
            header.dwDepth = 0;
            header.dwMipMapCount = mipMaps.Count;
            header.dwCaps = DDSCAPS_COMPLEX | DDSCAPS_TEXTURE | DDSCAPS_MIPMAP;
            header.dwCaps2 = 0;
            header.dwCaps3 = 0;
            header.dwCaps4 = 0;
            header.dwReserved2 = 0;

            writer.Write(header.dwSize);
            writer.Write(header.dwFlags);
            writer.Write(header.dwHeight);
            writer.Write(header.dwWidth);
            writer.Write(header.dwPitchOrLinearSize);
            writer.Write(header.dwDepth);
            writer.Write(header.dwMipMapCount);

            for (int i = 0; i < 11; i++)
            {
                writer.Write((uint)0);
            }

            writer.Write(format.dwSize);
            writer.Write(format.dwFlags);
            writer.Write(format.dwFourCC);
            writer.Write(format.dwRGBBitCount);
            writer.Write(format.dwRBitMask);
            writer.Write(format.dwGBitMask);
            writer.Write(format.dwBBitMask);
            writer.Write(format.dwABitMask);

            writer.Write(header.dwCaps);
            writer.Write(header.dwCaps2);
            writer.Write(header.dwCaps3);
            writer.Write(header.dwCaps4);
            writer.Write(header.dwReserved2);

            foreach (Bitmap surface in mipMaps)
            {
                BitmapData bits = surface.LockBits(new Rectangle(0, 0, surface.Width, surface.Height),
                                                   ImageLockMode.ReadOnly,
                                                   System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                nint bitPtr = bits.Scan0;
                // Not dividing by 4 this time because we're working with a byte array for the BinaryWriter's sake.
                int stride = bits.Stride;
                byte[] pixels = new byte[stride * bits.Height];
                System.Runtime.InteropServices.Marshal.Copy(bitPtr, pixels, 0, stride * bits.Height);
                surface.UnlockBits(bits);

                int a, r, g, b, y, x, xy;

                switch (cFormat)
                {
                    case CompressionMode.A1R5G5B5:
                        switch (bits.PixelFormat)
                        {
                            case System.Drawing.Imaging.PixelFormat.Format24bppRgb: // R8G8B8 -> A1R5G5B5
                                a = 0x8000;
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = (pixels[xy++] + 4) * 31 / 255;
                                        g = (pixels[xy++] + 4) * 31 / 255;
                                        r = (pixels[xy++] + 4) * 31 / 255;

                                        writer.Write((ushort)(a | r << 10 | g << 5 | b));
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppArgb: // A8R8G8B8 -> A1R5G5B5
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = (pixels[xy++] + 4) * 31 / 255;
                                        g = (pixels[xy++] + 4) * 31 / 255;
                                        r = (pixels[xy++] + 4) * 31 / 255;
                                        a = (pixels[xy++] & 0x80) << 8;

                                        writer.Write((ushort)(a | r << 10 | g << 5 | b));
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppRgb: // X8R8G8B8 -> A1R5G5B5
                                a = 0x8000;

                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = (pixels[xy++] + 4) * 31 / 255;
                                        g = (pixels[xy++] + 4) * 31 / 255;
                                        r = (pixels[xy++] + 4) * 31 / 255;
                                        xy++;

                                        writer.Write((ushort)(a | r << 10 | g << 5 | b));
                                    }
                                }
                                break;
                        }
                        break;
                    case CompressionMode.R5G6B5:
                        switch (bits.PixelFormat)
                        {
                            case System.Drawing.Imaging.PixelFormat.Format24bppRgb: // R8G8B8 -> R5G6B5
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = (pixels[xy++] + 4) * 31 / 255;
                                        g = (pixels[xy++] + 2) * 63 / 255;
                                        r = (pixels[xy++] + 4) * 31 / 255;

                                        writer.Write((ushort)(r << 11 | g << 5 | b));
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppArgb: // A8R8G8B8 -> R5G6B5
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {

                                        b = pixels[xy++];
                                        g = pixels[xy++];
                                        r = pixels[xy++];

                                        if ((pixels[xy++] & 0x80) == 0)
                                        {
                                            writer.Write((ushort)0);
                                        }
                                        else
                                        {
                                            b = (b + 4) * 31 / 255;
                                            g = (g + 2) * 63 / 255;
                                            r = (r + 4) * 31 / 255;

                                            writer.Write((ushort)(r << 11 | g << 5 | b));
                                        }
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppRgb: // X8R8G8B8 -> R5G6B5
                                a = 0x8000;

                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = (pixels[xy++] + 4) * 31 / 255;
                                        g = (pixels[xy++] + 2) * 63 / 255;
                                        r = (pixels[xy++] + 4) * 31 / 255;

                                        xy++;

                                        writer.Write((ushort)(a | r << 11 | g << 5 | b));
                                    }
                                }
                                break;
                        }
                        break;
                    case CompressionMode.RGB24:
                        switch (bits.PixelFormat)
                        {
                            case System.Drawing.Imaging.PixelFormat.Format24bppRgb: // R8G8B8
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppArgb: // A8R8G8B8 -> R8G8B8
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = pixels[xy++];
                                        g = pixels[xy++];
                                        r = pixels[xy++];

                                        if ((pixels[xy++] & 0x80) == 0)
                                        {
                                            writer.Write((byte)0);
                                            writer.Write((short)0);
                                        }
                                        else
                                        {
                                            writer.Write((byte)b);
                                            writer.Write((byte)g);
                                            writer.Write((byte)r);
                                        }
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppRgb: // X8R8G8B8 -> R8G8B8
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        b = pixels[xy++];
                                        g = pixels[xy++];
                                        r = pixels[xy++];
                                        xy++;

                                        writer.Write((byte)b);
                                        writer.Write((byte)g);
                                        writer.Write((byte)r);
                                    }
                                }
                                break;
                        }
                        break;
                    case CompressionMode.RGB32:
                        switch (bits.PixelFormat)
                        {
                            case System.Drawing.Imaging.PixelFormat.Format24bppRgb: // R8G8B8 -> A8R8G8B8
                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                        writer.Write((byte)0xFF);
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppArgb: // A8R8G8B8
                                if (stride == surface.Width * 4 && bits.Stride > 0)
                                {
                                    // Cohesive block of pixel data, top to bottom. No need to go row by row.
                                    writer.Write(pixels);
                                }
                                else
                                {
                                    for (y = 0; y < surface.Height; y++)
                                    {
                                        // if Stride < 0, image is stored from the bottom up, so we have to invert our y
                                        int xy1 = (bits.Stride < 0 ? surface.Height - y : y) * stride;
                                        writer.Write(pixels, xy1, stride);
                                    }
                                }
                                break;
                            case System.Drawing.Imaging.PixelFormat.Format32bppRgb: // X8R8G8B8 -> A8R8G8B8
                                a = 0xFF << 24;

                                for (y = 0; y < surface.Height; y++)
                                {
                                    xy = y * bits.Stride;

                                    for (x = 0; x < surface.Width; x++)
                                    {
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                        writer.Write(pixels[xy++]);
                                        writer.Write((byte)0xFF);
                                        xy++;
                                    }
                                }
                                break;
                        }
                        break;
                }
            }
        }
        catch
        {
            stream.Close();
            return false;
        }

        stream.Close();
        return true;
    }
}
