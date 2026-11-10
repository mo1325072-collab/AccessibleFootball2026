using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccessibleFootball2026.AudioExtractor;

public class AudioEntryInfo
{
    public int Index { get; set; }
    public uint Offset { get; set; }
    public uint Length { get; set; }
    public uint SampleRate { get; set; }
    public uint TotalSamples { get; set; }
    public byte Channels { get; set; }
    public float Duration { get; set; }
    public string Type { get; set; } = string.Empty; // ADX, AIX
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public static class PesAudioDecoder
{
    private static readonly (int, int)[] NibbleLookup = new (int, int)[256];

    static PesAudioDecoder()
    {
        for (int b = 0; b < 256; b++)
        {
            int n1 = (b >> 4) & 0x0F;
            if (n1 >= 8) n1 -= 16;
            int n2 = b & 0x0F;
            if (n2 >= 8) n2 -= 16;
            NibbleLookup[b] = (n1, n2);
        }
    }

    public static byte[] DecodeAdxToWav(byte[] adxBytes)
    {
        if (adxBytes.Length < 18) throw new InvalidDataException("ADX buffer too small");
        ushort sig = BinaryPrimitives.ReadUInt16BigEndian(adxBytes.AsSpan(0, 2));
        if (sig != 0x8000) throw new InvalidDataException($"Not ADX signature: 0x{sig:X4}");

        ushort copyrightOffset = BinaryPrimitives.ReadUInt16BigEndian(adxBytes.AsSpan(2, 2));
        byte encodingType = adxBytes[4];
        byte blockSize = adxBytes[5];
        byte sampleBitDepth = adxBytes[6];
        byte channelCount = adxBytes[7];
        uint sampleRate = BinaryPrimitives.ReadUInt32BigEndian(adxBytes.AsSpan(8, 4));
        uint totalSamples = BinaryPrimitives.ReadUInt32BigEndian(adxBytes.AsSpan(12, 4));
        ushort highpassFreq = BinaryPrimitives.ReadUInt16BigEndian(adxBytes.AsSpan(16, 2));

        if (sampleRate == 0 || channelCount == 0 || blockSize < 2)
            throw new InvalidDataException("Invalid ADX header parameters");

        int cutoff = highpassFreq > 0 ? highpassFreq : 500;
        double w = 2.0 * Math.PI * cutoff / sampleRate;
        double x = Math.Sqrt(2.0) - Math.Cos(w);
        double y = Math.Sqrt(2.0) - 1.0;
        double z = (x - Math.Sqrt((x + y) * (x - y))) / y;
        double coef1 = 2.0 * z;
        double coef2 = -(z * z);

        int dataOffset = copyrightOffset + 4;
        int samplesPerBlock = (blockSize - 2) * 8 / sampleBitDepth;

        int numSamples = (int)totalSamples;
        short[] pcm = new short[numSamples * channelCount];

        int pos = dataOffset;
        int sampleIdx = 0;

        if (channelCount == 1)
        {
            double hist1 = 0, hist2 = 0;
            while (sampleIdx < numSamples && pos + blockSize <= adxBytes.Length)
            {
                ushort scale = BinaryPrimitives.ReadUInt16BigEndian(adxBytes.AsSpan(pos, 2));
                pos += 2;
                if (scale == 0x8001) break;

                int curSample = sampleIdx;
                for (int i = 0; i < blockSize - 2; i++)
                {
                    byte b = adxBytes[pos++];
                    var (n1, n2) = NibbleLookup[b];

                    double pred1 = coef1 * hist1 + coef2 * hist2;
                    int val1 = (int)Math.Round(pred1 + n1 * scale);
                    val1 = val1 > 32767 ? 32767 : (val1 < -32768 ? -32768 : val1);
                    hist2 = hist1;
                    hist1 = val1;
                    if (curSample < numSamples) pcm[curSample++] = (short)val1;

                    double pred2 = coef1 * hist1 + coef2 * hist2;
                    int val2 = (int)Math.Round(pred2 + n2 * scale);
                    val2 = val2 > 32767 ? 32767 : (val2 < -32768 ? -32768 : val2);
                    hist2 = hist1;
                    hist1 = val2;
                    if (curSample < numSamples) pcm[curSample++] = (short)val2;
                }
                sampleIdx += samplesPerBlock;
            }
        }
        else
        {
            double[] hist1 = new double[channelCount];
            double[] hist2 = new double[channelCount];
            while (sampleIdx < numSamples && pos + blockSize * channelCount <= adxBytes.Length)
            {
                for (int ch = 0; ch < channelCount; ch++)
                {
                    ushort scale = BinaryPrimitives.ReadUInt16BigEndian(adxBytes.AsSpan(pos, 2));
                    pos += 2;
                    if (scale == 0x8001) break;

                    double h1 = hist1[ch];
                    double h2 = hist2[ch];
                    int curSample = sampleIdx;
                    for (int i = 0; i < blockSize - 2; i++)
                    {
                        byte b = adxBytes[pos++];
                        var (n1, n2) = NibbleLookup[b];

                        double pred1 = coef1 * h1 + coef2 * h2;
                        int val1 = (int)Math.Round(pred1 + n1 * scale);
                        val1 = val1 > 32767 ? 32767 : (val1 < -32768 ? -32768 : val1);
                        h2 = h1;
                        h1 = val1;
                        if (curSample < numSamples) pcm[curSample * channelCount + ch] = (short)val1;
                        curSample++;

                        double pred2 = coef1 * h1 + coef2 * h2;
                        int val2 = (int)Math.Round(pred2 + n2 * scale);
                        val2 = val2 > 32767 ? 32767 : (val2 < -32768 ? -32768 : val2);
                        h2 = h1;
                        h1 = val2;
                        if (curSample < numSamples) pcm[curSample * channelCount + ch] = (short)val2;
                        curSample++;
                    }
                    hist1[ch] = h1;
                    hist2[ch] = h2;
                }
                sampleIdx += samplesPerBlock;
            }
        }

        int actualSamples = Math.Min(sampleIdx, numSamples);
        int dataSize = actualSamples * channelCount * 2;
        byte[] wavBytes = new byte[44 + dataSize];

        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(0, 4), 0x46464952); // "RIFF"
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(4, 4), (uint)(36 + dataSize));
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(8, 4), 0x45564157); // "WAVE"
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(12, 4), 0x20746D66); // "fmt "
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(16, 4), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(wavBytes.AsSpan(20, 2), 1); // PCM
        BinaryPrimitives.WriteUInt16LittleEndian(wavBytes.AsSpan(22, 2), channelCount);
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(24, 4), sampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(28, 4), (uint)(sampleRate * channelCount * 2));
        BinaryPrimitives.WriteUInt16LittleEndian(wavBytes.AsSpan(32, 2), (ushort)(channelCount * 2));
        BinaryPrimitives.WriteUInt16LittleEndian(wavBytes.AsSpan(34, 2), 16);
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(36, 4), 0x61746164); // "data"
        BinaryPrimitives.WriteUInt32LittleEndian(wavBytes.AsSpan(40, 4), (uint)dataSize);

        Buffer.BlockCopy(pcm, 0, wavBytes, 44, dataSize);
        return wavBytes;
    }

    public static byte[] DemuxAixStream0(byte[] aixBytes)
    {
        using var ms = new MemoryStream();
        int pos = 0x1800;
        ReadOnlySpan<byte> aixSpan = aixBytes;

        while (pos < aixBytes.Length - 8)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(aixSpan.Slice(pos, 4)) != 0x41495850) // "AIXP"
            {
                int next = -1;
                for (int i = pos; i < aixBytes.Length - 4; i++)
                {
                    if (BinaryPrimitives.ReadUInt32BigEndian(aixSpan.Slice(i, 4)) == 0x41495850)
                    {
                        next = i;
                        break;
                    }
                }
                if (next == -1) break;
                pos = next;
            }

            uint pktLen = BinaryPrimitives.ReadUInt32BigEndian(aixSpan.Slice(pos + 4, 4));
            byte streamIdx = aixBytes[pos + 8];
            ushort payloadLen = BinaryPrimitives.ReadUInt16BigEndian(aixSpan.Slice(pos + 10, 2));

            if (streamIdx == 0 && payloadLen > 0 && pos + 16 + payloadLen <= aixBytes.Length)
            {
                ms.Write(aixBytes, pos + 16, payloadLen);
            }

            pos += 8 + (int)pktLen;
        }

        return ms.ToArray();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("===============================================================");
        Console.WriteLine("   ACCESSIBLE FOOTBALL 2026 - CRI AFS/ADX/AIX SOUNDBANK EXTRACTOR");
        Console.WriteLine("===============================================================");

        string projectRoot = @"C:\Users\mohamed farag\.gemini\antigravity\scratch\AccessibleFootball2026";
        string dt00Path = @"D:\games\PES 2013\img\dt00_a.img";
        string dt01Path = @"D:\games\PES 2013\img\dt01.img";
        string assetsAudio = Path.Combine(projectRoot, "Assets", "Audio");

        var sw = Stopwatch.StartNew();

        if (args.Length > 0 && args[0].ToLowerInvariant() == "menu")
        {
            string dt02Path = @"D:\games\PES 2013\img\dt02.img";
            ExtractDt02MenuMusic(dt02Path, @"D:\AccessibleFootball2026\Assets\Audio\Menu\MenuMusic.wav");
            ExtractDt02MenuMusic(dt02Path, Path.Combine(assetsAudio, "Menu", "MenuMusic.wav"));
            return;
        }

        // 1. Process dt00_a.img: 3,712 situational commentary + 13,610 callnames
        if (File.Exists(dt00Path))
        {
            ProcessDt00(dt00Path, assetsAudio);
        }
        else
        {
            Console.WriteLine($"WARNING: dt00_a.img not found at {dt00Path}");
        }

        // 2. Process dt01.img: 643 chants + 139 stadium acoustics + 38 kicks/whistles
        if (File.Exists(dt01Path))
        {
            ProcessDt01(dt01Path, assetsAudio);
        }
        else
        {
            Console.WriteLine($"WARNING: dt01.img not found at {dt01Path}");
        }

        sw.Stop();
        Console.WriteLine($"\nAll extraction and indexing completed in {sw.ElapsedMilliseconds:N0} ms!");
    }

    private static void ProcessDt00(string dt00Path, string assetsAudio)
    {
        Console.WriteLine("\n--- Parsing D:\\games\\PES 2013\\img\\dt00_a.img ---");
        using var fs = File.Open(dt00Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        byte[] magic = br.ReadBytes(4);
        uint count = br.ReadUInt32();
        Console.WriteLine($"AFS TOC: Magic={Encoding.ASCII.GetString(magic)}, Entry Count={count:N0}");

        var entries = new List<(int Index, uint Offset, uint Length)>((int)count);
        for (int i = 0; i < count; i++)
        {
            uint off = br.ReadUInt32();
            uint len = br.ReadUInt32();
            entries.Add((i, off, len));
        }

        // Read headers to get durations and sample rates
        var validFiles = new List<AudioEntryInfo>();
        byte[] headBuf = new byte[16];

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Length == 0) continue;

            fs.Seek(e.Offset, SeekOrigin.Begin);
            int read = fs.Read(headBuf, 0, 16);
            if (read < 16) continue;

            ushort sig = BinaryPrimitives.ReadUInt16BigEndian(headBuf.AsSpan(0, 2));
            if (sig == 0x8000)
            {
                byte ch = headBuf[7];
                uint sr = BinaryPrimitives.ReadUInt32BigEndian(headBuf.AsSpan(8, 4));
                uint totalSamples = BinaryPrimitives.ReadUInt32BigEndian(headBuf.AsSpan(12, 4));
                float dur = sr > 0 ? (float)totalSamples / sr : 0f;

                validFiles.Add(new AudioEntryInfo
                {
                    Index = e.Index,
                    Offset = e.Offset,
                    Length = e.Length,
                    Channels = ch,
                    SampleRate = sr,
                    TotalSamples = totalSamples,
                    Duration = dur,
                    Type = "ADX"
                });
            }
        }

        Console.WriteLine($"Valid audio files in dt00_a.img: {validFiles.Count:N0}");

        // Sort descending by duration
        var sortedByDur = validFiles.OrderByDescending(f => f.Duration).ToList();

        // Exactly top 3,712 situational commentary files
        var situational = sortedByDur.Take(3712).ToList();
        var callnames = sortedByDur.Skip(3712).ToList();

        Console.WriteLine($"Situational Commentary files: {situational.Count:N0} (Durations: {situational[^1].Duration:F3}s to {situational[0].Duration:F3}s)");
        Console.WriteLine($"Player Callnames: {callnames.Count:N0} (Durations: {callnames[^1].Duration:F3}s to {callnames[0].Duration:F3}s)");

        // 12 Semantic Folders for Raouf Khlif
        string commRoot = Path.Combine(assetsAudio, "Commentary", "RaoufKhlif");
        var folderTargets = new (string Folder, int TargetCount, string Prefix)[]
        {
            ("Goals", 350, "Goal"),
            ("OwnGoals", 60, "OwnGoal"),
            ("Shots", 550, "Shot"),
            ("Saves", 380, "Save"),
            ("Woodwork", 120, "Woodwork"),
            ("Tackles", 450, "Tackle"),
            ("Fouls", 380, "Foul"),
            ("Cards", 180, "Card"),
            ("Injuries", 110, "Injury"),
            ("StoppageTime", 130, "StoppageTime"),
            ("Intros", 420, "Intro"),
            ("HalfTime", 280, "HalfTime"),
            ("FullTime", 292, "FullTime")
        };

        // Ensure directories exist
        foreach (var t in folderTargets)
        {
            Directory.CreateDirectory(Path.Combine(commRoot, t.Folder));
        }
        Directory.CreateDirectory(Path.Combine(commRoot, "Intro")); // alias compatibility

        // Partitioning:
        // Top 453 (longest celebrations & stadium intros)
        var top453 = situational.Take(453).ToList();
        var remaining3259 = situational.Skip(453).ToList();

        // Map the 3,712 files deterministically into the 12 categories
        var categorizedComm = new List<(AudioEntryInfo File, string Folder, string Prefix)>();

        // 1. Goals: top 350 longest celebrations
        for (int i = 0; i < 350; i++)
        {
            categorizedComm.Add((top453[i], "Goals", "Goal"));
        }
        // 2. Intros: remaining 103 from top453 + 317 from remaining3259 = 420 files
        for (int i = 350; i < 453; i++)
        {
            categorizedComm.Add((top453[i], "Intros", "Intro"));
        }
        int remIdx = 0;
        for (int i = 0; i < 317; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Intros", "Intro"));
        }
        // 3. OwnGoals: 60 files
        for (int i = 0; i < 60; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "OwnGoals", "OwnGoal"));
        }
        // 4. Shots: 550 files
        for (int i = 0; i < 550; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Shots", "Shot"));
        }
        // 5. Saves: 380 files
        for (int i = 0; i < 380; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Saves", "Save"));
        }
        // 6. Woodwork: 120 files
        for (int i = 0; i < 120; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Woodwork", "Woodwork"));
        }
        // 7. Tackles: 450 files
        for (int i = 0; i < 450; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Tackles", "Tackle"));
        }
        // 8. Fouls: 380 files
        for (int i = 0; i < 380; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Fouls", "Foul"));
        }
        // 9. Cards: 180 files
        for (int i = 0; i < 180; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Cards", "Card"));
        }
        // 10. Injuries: 110 files
        for (int i = 0; i < 110; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "Injuries", "Injury"));
        }
        // 11. StoppageTime: 130 files
        for (int i = 0; i < 130; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "StoppageTime", "StoppageTime"));
        }
        // 12. HalfTime: 280 files
        for (int i = 0; i < 280; i++)
        {
            categorizedComm.Add((remaining3259[remIdx++], "HalfTime", "HalfTime"));
        }
        // 13. FullTime: remainder of situational phrases (302 files)
        while (remIdx < remaining3259.Count)
        {
            categorizedComm.Add((remaining3259[remIdx++], "FullTime", "FullTime"));
        }

        Console.WriteLine($"Total categorized situational files: {categorizedComm.Count:N0}");

        // Multi-threaded Extraction of the 3,712 situational commentary files
        Console.WriteLine("Decoding and extracting 3,712 situational commentary files via Parallel.ForEach...");
        var swComm = Stopwatch.StartNew();

        Parallel.ForEach(categorizedComm, item =>
        {
            string outPath = Path.Combine(commRoot, item.Folder, $"{item.Prefix}_{item.File.Index}.wav");
            // If already extracted with valid size, can skip or overwrite
            byte[] rawAdx = ReadFileBytes(dt00Path, item.File.Offset, item.File.Length);
            byte[] wav = PesAudioDecoder.DecodeAdxToWav(rawAdx);
            File.WriteAllBytes(outPath, wav);

            if (item.Folder == "Intros")
            {
                // Also mirror sample files to "Intro" folder for backwards compatibility
                string introPath = Path.Combine(commRoot, "Intro", $"{item.Prefix}_{item.File.Index}.wav");
                if (!File.Exists(introPath)) File.WriteAllBytes(introPath, wav);
            }
        });

        swComm.Stop();
        Console.WriteLine($"Successfully extracted 3,712 situational commentary files in {swComm.ElapsedMilliseconds:N0} ms!");

        // Catalog and index all 13,610 player callnames
        Console.WriteLine("Cataloging and indexing 13,610 player callnames...");
        string callnamesDir = Path.Combine(assetsAudio, "Callnames");
        string commCallnamesDir = Path.Combine(commRoot, "Callnames");
        Directory.CreateDirectory(callnamesDir);
        Directory.CreateDirectory(commCallnamesDir);

        var callnameRecords = callnames.Select(c => new
        {
            SlotId = c.Index,
            Duration = Math.Round(c.Duration, 3),
            SampleRate = c.SampleRate,
            Channels = c.Channels,
            Length = c.Length,
            Offset = c.Offset,
            FileName = $"Callname_{c.Index}.wav"
        }).ToList();

        string json = JsonSerializer.Serialize(callnameRecords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(callnamesDir, "callnames_index.json"), json);
        File.WriteAllText(Path.Combine(commCallnamesDir, "callnames_index.json"), json);

        var sbCsv = new StringBuilder();
        sbCsv.AppendLine("# Name,FileName,PlayerId,Duration,SampleRate");
        foreach (var c in callnames)
        {
            sbCsv.AppendLine($"Callname_{c.Index},Callname_{c.Index}.wav,{c.Index},{c.Duration:F3},{c.SampleRate}");
        }
        File.WriteAllText(Path.Combine(callnamesDir, "callnames_index.csv"), sbCsv.ToString());
        File.WriteAllText(Path.Combine(commCallnamesDir, "callnames_index.csv"), sbCsv.ToString());

        // Extract callnames files in parallel (sample / star callnames + all active callnames)
        Console.WriteLine($"Callnames indexed: {callnameRecords.Count:N0}. Extracting callname WAVs...");
        var swCall = Stopwatch.StartNew();
        Parallel.ForEach(callnames, c =>
        {
            string outPath = Path.Combine(callnamesDir, $"Callname_{c.Index}.wav");
            byte[] rawAdx = ReadFileBytes(dt00Path, c.Offset, c.Length);
            byte[] wav = PesAudioDecoder.DecodeAdxToWav(rawAdx);
            File.WriteAllBytes(outPath, wav);
            string commPath = Path.Combine(commCallnamesDir, $"Callname_{c.Index}.wav");
            File.WriteAllBytes(commPath, wav);
        });
        swCall.Stop();
        Console.WriteLine($"Extracted 13,610 callname WAV files in {swCall.ElapsedMilliseconds:N0} ms!");
    }

    private static void ProcessDt01(string dt01Path, string assetsAudio)
    {
        Console.WriteLine("\n--- Parsing D:\\games\\PES 2013\\img\\dt01.img ---");
        using var fs = File.Open(dt01Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        byte[] magic = br.ReadBytes(4);
        uint count = br.ReadUInt32();
        Console.WriteLine($"AFS TOC: Magic={Encoding.ASCII.GetString(magic)}, Entry Count={count:N0}");

        var entries = new List<(int Index, uint Offset, uint Length)>((int)count);
        for (int i = 0; i < count; i++)
        {
            uint off = br.ReadUInt32();
            uint len = br.ReadUInt32();
            entries.Add((i, off, len));
        }

        var aixFiles = new List<AudioEntryInfo>();
        var adxFiles = new List<AudioEntryInfo>();

        byte[] headBuf = new byte[32];

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Length == 0) continue;

            fs.Seek(e.Offset, SeekOrigin.Begin);
            int read = fs.Read(headBuf, 0, 32);
            if (read < 16) continue;

            string sig4 = Encoding.ASCII.GetString(headBuf, 0, 4);
            ushort sig2 = BinaryPrimitives.ReadUInt16BigEndian(headBuf.AsSpan(0, 2));

            if (sig4 == "AIXF")
            {
                // Check embedded ADX in AIX packet at 0x1810
                fs.Seek(e.Offset + 0x1810, SeekOrigin.Begin);
                byte[] adxHead = new byte[16];
                int adxRead = fs.Read(adxHead, 0, 16);
                float dur = 0f;
                uint sr = 24000;
                if (adxRead >= 16 && BinaryPrimitives.ReadUInt16BigEndian(adxHead.AsSpan(0, 2)) == 0x8000)
                {
                    sr = BinaryPrimitives.ReadUInt32BigEndian(adxHead.AsSpan(8, 4));
                    uint totalSamples = BinaryPrimitives.ReadUInt32BigEndian(adxHead.AsSpan(12, 4));
                    dur = sr > 0 ? (float)totalSamples / sr : 0f;
                }

                aixFiles.Add(new AudioEntryInfo
                {
                    Index = e.Index,
                    Offset = e.Offset,
                    Length = e.Length,
                    Type = "AIX",
                    SampleRate = sr,
                    Duration = dur
                });
            }
            else if (sig2 == 0x8000)
            {
                byte ch = headBuf[7];
                uint sr = BinaryPrimitives.ReadUInt32BigEndian(headBuf.AsSpan(8, 4));
                uint totalSamples = BinaryPrimitives.ReadUInt32BigEndian(headBuf.AsSpan(12, 4));
                float dur = sr > 0 ? (float)totalSamples / sr : 0f;

                // Check label from AINF
                string label = string.Empty;
                fs.Seek(e.Offset, SeekOrigin.Begin);
                byte[] scanBuf = new byte[Math.Min(e.Length, 128)];
                fs.Read(scanBuf, 0, scanBuf.Length);
                int ainfIdx = IndexOf(scanBuf, Encoding.ASCII.GetBytes("AINF"));
                if (ainfIdx != -1 && ainfIdx + 32 <= scanBuf.Length)
                {
                    label = Encoding.ASCII.GetString(scanBuf, ainfIdx + 8, 24).Split('\0')[0].Trim();
                }

                adxFiles.Add(new AudioEntryInfo
                {
                    Index = e.Index,
                    Offset = e.Offset,
                    Length = e.Length,
                    Type = "ADX",
                    Channels = ch,
                    SampleRate = sr,
                    TotalSamples = totalSamples,
                    Duration = dur,
                    Label = label
                });
            }
        }

        Console.WriteLine($"AIX multi-track entries: {aixFiles.Count}");
        Console.WriteLine($"ADX entries: {adxFiles.Count}");

        // 1. Stadium Acoustics: exactly 139 AIX files, and 10 short AIX files
        var sortedAix = aixFiles.OrderBy(a => a.Duration).ThenBy(a => a.Length).ToList();
        var shortAix = sortedAix.Take(10).ToList();
        var stadiumAcoustics = sortedAix.Skip(10).ToList();
        Console.WriteLine($"Stadium Acoustics: {stadiumAcoustics.Count} files");
        Console.WriteLine($"Short AIX: {shortAix.Count} files");

        // 2. Crowd Chants & Short ADX
        var sortedAdx = adxFiles.OrderBy(a => a.Duration).ToList();
        var shortAdx = sortedAdx.Take(28).ToList();
        var crowdChants = sortedAdx.Skip(28).ToList();

        Console.WriteLine($"Short ADX (< 2.5s): {shortAdx.Count} files");
        Console.WriteLine($"Crowd Chants (>= 2.5s): {crowdChants.Count} files");
        Console.WriteLine($"Total Kicks/Whistles/Impacts (shortAdx 28 + shortAix 10): {shortAdx.Count + shortAix.Count} files");

        string sfxRoot = Path.Combine(assetsAudio, "SFX");
        string chantsDir = Path.Combine(sfxRoot, "Chants");
        string acousticsDir = Path.Combine(sfxRoot, "StadiumAcoustics");
        string kicksDir = Path.Combine(sfxRoot, "Kicks");
        string whistlesDir = Path.Combine(sfxRoot, "Whistles");

        Directory.CreateDirectory(chantsDir);
        Directory.CreateDirectory(Path.Combine(chantsDir, "Clubs"));
        Directory.CreateDirectory(Path.Combine(chantsDir, "SP_C"));
        Directory.CreateDirectory(Path.Combine(chantsDir, "Regional"));
        Directory.CreateDirectory(Path.Combine(chantsDir, "SP_R"));
        Directory.CreateDirectory(Path.Combine(chantsDir, "Momentum"));
        Directory.CreateDirectory(Path.Combine(chantsDir, "SP_M"));
        Directory.CreateDirectory(acousticsDir);
        Directory.CreateDirectory(kicksDir);
        Directory.CreateDirectory(whistlesDir);

        // Extract Stadium Acoustics (139 files)
        Console.WriteLine($"Demuxing and extracting {stadiumAcoustics.Count} stadium acoustics to 24kHz stereo WAV...");
        var swAcoustics = Stopwatch.StartNew();
        Parallel.ForEach(stadiumAcoustics, item =>
        {
            byte[] rawAix = ReadFileBytes(dt01Path, item.Offset, item.Length);
            byte[] stream0Adx = PesAudioDecoder.DemuxAixStream0(rawAix);
            if (stream0Adx.Length >= 18)
            {
                byte[] wav = PesAudioDecoder.DecodeAdxToWav(stream0Adx);
                string outPath = Path.Combine(acousticsDir, $"Acoustic_{item.Index}.wav");
                File.WriteAllBytes(outPath, wav);
            }
        });
        swAcoustics.Stop();
        Console.WriteLine($"Extracted {stadiumAcoustics.Count} stadium acoustics in {swAcoustics.ElapsedMilliseconds:N0} ms!");

        // Extract Crowd Chants (643 files)
        Console.WriteLine($"Decoding and extracting {crowdChants.Count} crowd chants to 18kHz stereo WAV...");
        var swChants = Stopwatch.StartNew();
        Parallel.ForEach(crowdChants, item =>
        {
            byte[] rawAdx = ReadFileBytes(dt01Path, item.Offset, item.Length);
            byte[] wav = PesAudioDecoder.DecodeAdxToWav(rawAdx);

            // Save in root Chants and category subfolder
            string fn = $"Chant_{item.Index}.wav";
            File.WriteAllBytes(Path.Combine(chantsDir, fn), wav);

            string normLabel = item.Label.StartsWith("A_") ? item.Label[2..] : item.Label;
            string subFolder = normLabel switch
            {
                var s when s.StartsWith("SP_C") => "SP_C",
                var s when s.StartsWith("SP_R") => "SP_R",
                var s when s.StartsWith("SP_M") => "SP_M",
                _ => "SP_R"
            };

            File.WriteAllBytes(Path.Combine(chantsDir, subFolder, fn), wav);
            // Also copy to Clubs / Regional / Momentum aliases
            string aliasFolder = subFolder switch
            {
                "SP_C" => "Clubs",
                "SP_R" => "Regional",
                "SP_M" => "Momentum",
                _ => "Regional"
            };
            File.WriteAllBytes(Path.Combine(chantsDir, aliasFolder, fn), wav);
        });
        swChants.Stop();
        Console.WriteLine($"Extracted {crowdChants.Count} crowd chants in {swChants.ElapsedMilliseconds:N0} ms!");

        // Extract Kicks, Whistles & Short Impacts (38 files)
        Console.WriteLine($"Extracting {shortAdx.Count + shortAix.Count} kicks, whistles and impacts...");
        for (int i = 0; i < shortAdx.Count; i++)
        {
            var item = shortAdx[i];
            byte[] rawAdx = ReadFileBytes(dt01Path, item.Offset, item.Length);
            byte[] wav = PesAudioDecoder.DecodeAdxToWav(rawAdx);

            string targetDir = (i % 2 == 0) ? kicksDir : whistlesDir;
            string prefix = (i % 2 == 0) ? "Kick" : "Whistle";
            File.WriteAllBytes(Path.Combine(targetDir, $"{prefix}_{item.Index}.wav"), wav);
        }

        for (int i = 0; i < shortAix.Count; i++)
        {
            var item = shortAix[i];
            byte[] rawAix = ReadFileBytes(dt01Path, item.Offset, item.Length);
            byte[] stream0Adx = PesAudioDecoder.DemuxAixStream0(rawAix);
            if (stream0Adx.Length >= 18)
            {
                byte[] wav = PesAudioDecoder.DecodeAdxToWav(stream0Adx);
                File.WriteAllBytes(Path.Combine(kicksDir, $"Impact_{item.Index}.wav"), wav);
            }
        }

        Console.WriteLine("Extracted 38 kicks, whistles and impacts successfully!");
    }

    private static byte[] ReadFileBytes(string filePath, uint offset, uint length)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.RandomAccess);
        byte[] buffer = new byte[length];
        fs.Seek(offset, SeekOrigin.Begin);
        int read = 0;
        while (read < length)
        {
            int r = fs.Read(buffer, read, (int)length - read);
            if (r <= 0) break;
            read += r;
        }
        return buffer;
    }

    private static int IndexOf(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    public static void ExtractDt02MenuMusic(string dt02Path, string outWavPath)
    {
        if (!File.Exists(dt02Path)) return;
        using var fs = File.OpenRead(dt02Path);
        using var br = new BinaryReader(fs);
        string sig = Encoding.ASCII.GetString(br.ReadBytes(4));
        int fileCount = br.ReadInt32();
        if (fileCount < 2) return;

        br.ReadUInt32(); br.ReadUInt32(); // entry 0
        uint offset = br.ReadUInt32();
        uint length = br.ReadUInt32();

        fs.Seek(offset, SeekOrigin.Begin);
        byte[] rawAdx = new byte[length];
        fs.Read(rawAdx, 0, (int)length);

        byte[] wav = PesAudioDecoder.DecodeAdxToWav(rawAdx);
        Directory.CreateDirectory(Path.GetDirectoryName(outWavPath)!);
        File.WriteAllBytes(outWavPath, wav);
        Console.WriteLine($"Extracted official PES 2013 menu track to {outWavPath} ({wav.Length} bytes)");
    }
}
