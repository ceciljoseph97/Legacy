// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Helpers/TspGifExporter.cs
using System.IO;

namespace NeuroSim.UI.Helpers;

/// <summary>
/// Writes a looping GIF89a of TSP route evolution.
/// Completely self-contained: no WPF, no external packages.
/// Frame pixels are rasterised via Bresenham line/circle drawing.
/// LZW compression is implemented from scratch (GIF variant, LSB-first).
/// </summary>
public static class TspGifExporter
{
    // ── 8-colour palette (R,G,B) ─────────────────────────────────────────
    private static readonly (byte R, byte G, byte B)[] Pal =
    [
        (0x09, 0x09, 0x0F),  // 0  background
        (0xA7, 0x8B, 0xFA),  // 1  route edge (purple)
        (0x22, 0xC5, 0x5E),  // 2  city fill (green)
        (0xF5, 0x9E, 0x0B),  // 3  city border (amber)
        (0x1E, 0x1E, 0x2E),  // 4  grid dim
        (0x64, 0x74, 0x8B),  // 5  label grey
        (0xEF, 0x44, 0x44),  // 6  accent red
        (0xFF, 0xFF, 0xFF),  // 7  white
    ];
    private const int MinCodeSize = 3;  // 2^3 = 8 palette entries

    // ── Public entry point ────────────────────────────────────────────────

    /// <param name="frameDelayMs">Display time per frame in milliseconds.</param>
    /// <param name="size">Canvas size in pixels (square).</param>
    public static void Save(
        string path,
        IReadOnlyList<int[]> routeSnapshots,
        IReadOnlyList<(string Name, double X, double Y)> cities,
        int frameDelayMs = 120,
        int size = 420)
    {
        if (routeSnapshots.Count == 0) return;

        // Pre-compute pixel positions for each city (they don't move between frames)
        var pos = ComputePositions(cities, size);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        WriteHeader(fs, size);
        WriteNetscapeLoop(fs);

        int delayCs = Math.Max(1, frameDelayMs / 10);
        foreach (var tour in routeSnapshots)
        {
            var pixels = RasteriseFrame(tour, pos, cities.Count, size);
            WriteGraphicControl(fs, (ushort)delayCs);
            WriteImageDescriptor(fs, size);
            WriteLzw(fs, pixels);
        }
        fs.WriteByte(0x3B); // GIF trailer
    }

    // ── Coordinate pre-computation ────────────────────────────────────────

    private static (int x, int y)[] ComputePositions(
        IReadOnlyList<(string Name, double X, double Y)> cities, int size)
    {
        const int pad = 24;
        double usable = size - 2.0 * pad;

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        foreach (var (_, cx, cy) in cities)
        {
            if (cx < minX) minX = cx; if (cx > maxX) maxX = cx;
            if (cy < minY) minY = cy; if (cy > maxY) maxY = cy;
        }
        double scale = Math.Max(Math.Max(maxX - minX, maxY - minY), 1e-9);

        var pos = new (int x, int y)[cities.Count];
        for (int i = 0; i < cities.Count; i++)
        {
            var (_, cx, cy) = cities[i];
            pos[i] = (
                (int)Math.Clamp(pad + (cx - minX) / scale * usable, 0, size - 1),
                (int)Math.Clamp(pad + (1.0 - (cy - minY) / scale) * usable, 0, size - 1)
            );
        }
        return pos;
    }

    // ── Frame rasterisation (no WPF, pure C#) ────────────────────────────

    private static byte[] RasteriseFrame(int[] tour, (int x, int y)[] pos, int nCities, int size)
    {
        var px = new byte[size * size]; // all 0 = background

        // Route edges (colour index 1)
        if (tour.Length > 1)
            for (int i = 0; i < tour.Length; i++)
            {
                var (ax, ay) = pos[tour[i]];
                var (bx, by) = pos[tour[(i + 1) % tour.Length]];
                BresenhamLine(px, size, ax, ay, bx, by, 1);
            }

        // City dots — amber border (3), green fill (2)
        for (int i = 0; i < nCities; i++)
        {
            var (cx, cy) = pos[i];
            FilledCircle(px, size, cx, cy, 5, 3);   // border ring
            FilledCircle(px, size, cx, cy, 3, 2);   // fill centre
        }

        return px;
    }

    // ── Drawing primitives ────────────────────────────────────────────────

    private static void BresenhamLine(byte[] px, int size, int x0, int y0, int x1, int y1, byte col)
    {
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            SetPx(px, size, x0, y0, col);
            // Widen line by 1 pixel for visibility
            SetPx(px, size, x0 + 1, y0,     col);
            SetPx(px, size, x0,     y0 + 1, col);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    private static void FilledCircle(byte[] px, int size, int cx, int cy, int r, byte col)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r)
                    SetPx(px, size, cx + dx, cy + dy, col);
    }

    private static void SetPx(byte[] px, int size, int x, int y, byte col)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
            px[y * size + x] = col;
    }

    // ── GIF binary writing ────────────────────────────────────────────────

    private static void WriteHeader(Stream s, int size)
    {
        s.Write("GIF89a"u8);
        // Logical Screen Descriptor
        WriteU16(s, (ushort)size);
        WriteU16(s, (ushort)size);
        // Packed: GCT=1 | colRes=2(3-bit) | sort=0 | GCT size=2 (=8 entries)
        s.WriteByte(0b1_010_0_010);
        s.WriteByte(0);  // bg colour index
        s.WriteByte(0);  // pixel aspect ratio
        // Global Colour Table
        foreach (var (r, g, b) in Pal) { s.WriteByte(r); s.WriteByte(g); s.WriteByte(b); }
    }

    private static void WriteNetscapeLoop(Stream s)
    {
        s.WriteByte(0x21); s.WriteByte(0xFF); s.WriteByte(0x0B);
        s.Write("NETSCAPE2.0"u8);
        s.WriteByte(0x03); s.WriteByte(0x01);
        WriteU16(s, 0);   // loop count 0 = infinite
        s.WriteByte(0x00);
    }

    private static void WriteGraphicControl(Stream s, ushort delayCs)
    {
        s.WriteByte(0x21); s.WriteByte(0xF9); s.WriteByte(0x04);
        s.WriteByte(0b000_010_0_0);  // dispose=restore-to-bg
        WriteU16(s, delayCs);
        s.WriteByte(0);  // transparent index (unused)
        s.WriteByte(0);  // block terminator
    }

    private static void WriteImageDescriptor(Stream s, int size)
    {
        s.WriteByte(0x2C);
        WriteU16(s, 0); WriteU16(s, 0);          // left, top
        WriteU16(s, (ushort)size); WriteU16(s, (ushort)size);
        s.WriteByte(0x00);  // no local CT, not interlaced
    }

    private static void WriteLzw(Stream s, byte[] pixels)
    {
        s.WriteByte(MinCodeSize);
        var compressed = LzwCompress(pixels);
        int pos = 0;
        while (pos < compressed.Count)
        {
            int chunk = Math.Min(255, compressed.Count - pos);
            s.WriteByte((byte)chunk);
            for (int i = 0; i < chunk; i++) s.WriteByte(compressed[pos + i]);
            pos += chunk;
        }
        s.WriteByte(0x00);
    }

    // ── LZW encoder (GIF variant — LSB-first bit packing) ─────────────────

    private static List<byte> LzwCompress(byte[] pixels)
    {
        const int clearCode = 1 << MinCodeSize;   // 8
        const int eoiCode   = clearCode + 1;       // 9

        int codeSize = MinCodeSize + 1;             // starts at 4
        int nextCode = eoiCode + 1;                 // first new code = 10
        var table    = new Dictionary<(int, byte), int>();

        ulong bitBuf = 0;
        int   bitCnt = 0;
        var   output = new List<byte>(pixels.Length / 4);

        void Emit(int code)
        {
            bitBuf |= (ulong)code << bitCnt;
            bitCnt += codeSize;
            while (bitCnt >= 8) { output.Add((byte)(bitBuf & 0xFF)); bitBuf >>= 8; bitCnt -= 8; }
        }

        void Reset()
        {
            table.Clear();
            codeSize = MinCodeSize + 1;
            nextCode = eoiCode + 1;
        }

        Emit(clearCode);
        Reset();

        int prefix = pixels[0];

        for (int i = 1; i < pixels.Length; i++)
        {
            byte sym = pixels[i];
            if (table.TryGetValue((prefix, sym), out int found))
            {
                prefix = found;
            }
            else
            {
                Emit(prefix);
                if (nextCode < 4096)
                {
                    table[(prefix, sym)] = nextCode++;
                    // ">=" is correct: increase BEFORE the new code is emitted
                    if (nextCode >= (1 << codeSize) && codeSize < 12)
                        codeSize++;
                }
                else
                {
                    // Table full — emit clear and restart
                    Emit(clearCode);
                    Reset();
                }
                prefix = sym;
            }
        }

        Emit(prefix);
        Emit(eoiCode);
        // Flush remaining bits (at most 7)
        if (bitCnt > 0) output.Add((byte)(bitBuf & 0xFF));
        return output;
    }

    private static void WriteU16(Stream s, ushort v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)(v >> 8));
    }
}
