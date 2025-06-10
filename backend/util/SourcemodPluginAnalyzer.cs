using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace SCLauncher.backend.util;

/// <summary>Utility for analyzing compiled Sourcemod plugins.</summary>
/// <remarks>
/// Adapted from nosoop's read_sm_plugin.py gist, thanks for making a robust utility that still works after all the years!
/// </remarks>
public static class SourcemodPluginAnalyzer
{
    public class PluginAnalysisResult
    {
        public uint Magic { get; set; }
        public ushort Version { get; set; }
        public byte Compression { get; set; }
        public uint DiskSize { get; set; }
        public uint ImageSize { get; set; }
        public byte NumSections { get; set; }
        public uint StringTab { get; set; }
        public uint DataOffs { get; set; }
        public List<SectionInfo> Sections { get; set; } = [];
        public List<string> Publics { get; set; } = [];
        public List<string> Natives { get; set; } = [];
        public Dictionary<string, uint> Pubvars { get; set; } = new();
        public MyInfo MyInfo { get; set; } = new();
        public uint? FileVersion { get; set; }
        public string? CompileDate { get; set; }
        public string? CompileTime { get; set; }
        public string? CompileVersion { get; set; }
        public string? CodeHash { get; set; }
        public string? DataHash { get; set; }
        public string? PluginHash { get; set; }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"magic {Magic} version 0x{Version:x} compression {Compression} disk_size {DiskSize} image_size {ImageSize} num_sections {NumSections} stringtab {StringTab} dataoffs {DataOffs}");
            foreach (var sec in Sections)
                sb.AppendLine($"section_name {sec.Name} nameoffs {sec.NameOffset} dataoffs {sec.DataOffset} size {sec.Size}");
            foreach (var pub in Publics)
                sb.AppendLine($"publics: {pub}");
            foreach (var nat in Natives)
                sb.AppendLine($"native: {nat}");
            foreach (var kv in Pubvars)
                sb.AppendLine($"pubvars: {kv.Key}");
            if (MyInfo.Name != null)
                sb.AppendLine($"myinfo.name: {MyInfo.Name}");
            if (MyInfo.Description != null)
                sb.AppendLine($"myinfo.description: {MyInfo.Description}");
            if (MyInfo.Author != null)
                sb.AppendLine($"myinfo.author: {MyInfo.Author}");
            if (MyInfo.Version != null)
                sb.AppendLine($"myinfo.version: {MyInfo.Version}");
            if (MyInfo.Url != null)
                sb.AppendLine($"myinfo.url: {MyInfo.Url}");
            if (FileVersion.HasValue)
                sb.AppendLine($"file version: {FileVersion}");
            if (!string.IsNullOrEmpty(CompileDate))
                sb.AppendLine($"compile date: {CompileDate}");
            if (!string.IsNullOrEmpty(CompileTime))
                sb.AppendLine($"compile time: {CompileTime}");
            if (!string.IsNullOrEmpty(CompileVersion))
                sb.AppendLine($"compile version: {CompileVersion}");
            if (!string.IsNullOrEmpty(CodeHash))
                sb.AppendLine($"code hash: {CodeHash}");
            if (!string.IsNullOrEmpty(DataHash))
                sb.AppendLine($"data hash: {DataHash}");
            if (!string.IsNullOrEmpty(PluginHash))
                sb.AppendLine($"hash: {PluginHash}");
            return sb.ToString();
        }
    }

    public class SectionInfo
    {
        public string Name { get; init; } = string.Empty;
        public uint NameOffset { get; init; }
        public uint DataOffset { get; init; }
        public uint Size { get; init; }
    }
    
    public class MyInfo
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string? Url { get; set; }
    }

    public static PluginAnalysisResult Analyze(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

        var result = new PluginAnalysisResult();

        // Header: <IHBIIBII
        var header = Unpack(reader, "IHBIIBII");
        result.Magic = (uint)header[0];
        result.Version = (ushort)header[1];
        result.Compression = (byte)header[2];
        result.DiskSize = (uint)header[3];
        result.ImageSize = (uint)header[4];
        result.NumSections = (byte)header[5];
        result.StringTab = (uint)header[6];
        result.DataOffs = (uint)header[7];

        Stream contents;
        if (result.Compression != 0)
        {
            long offsHeader = reader.BaseStream.Position;
            var compHeader = Unpack(reader, "III");
            uint nameoffs = (uint)compHeader[0];
            uint compDataoffs = (uint)compHeader[1];
            uint compSize = (uint)compHeader[2];
            uint totalSize = result.DiskSize - compDataoffs;

            reader.BaseStream.Seek(compDataoffs, SeekOrigin.Begin);
            byte[] compressed = reader.ReadBytes((int)totalSize);
            byte[] data = DecompressZlib(compressed);

            reader.BaseStream.Seek(offsHeader, SeekOrigin.Begin);
            byte[] headerData = reader.ReadBytes((int)(compDataoffs - offsHeader));

            var all = new byte[headerData.Length + data.Length];
            Buffer.BlockCopy(headerData, 0, all, 0, headerData.Length);
            Buffer.BlockCopy(data, 0, all, headerData.Length, data.Length);
            contents = new MemoryStream(all);
        }
        else
        {
            contents = new MemoryStream(reader.ReadBytes((int)result.ImageSize));
        }

        using var sectionReader = new BinaryReader(contents, Encoding.UTF8, leaveOpen: true);

        // Read section list
        var section = new Dictionary<string, (MemoryStream, int)>();
        for (int i = 0; i < result.NumSections; i++)
        {
            sectionReader.BaseStream.Seek(i * 12, SeekOrigin.Begin);
            var sec = Unpack(sectionReader, "III");
            uint sectionNameoffs = (uint)sec[0];
            uint sectionDataoffs = (uint)sec[1];
            uint sectionSize = (uint)sec[2];

            string sectionName = ReadNullTermString(sectionReader, (result.NumSections * 12) + sectionNameoffs);

            result.Sections.Add(new SectionInfo
            {
                Name = sectionName,
                NameOffset = sectionNameoffs,
                DataOffset = sectionDataoffs,
                Size = sectionSize
            });

            sectionReader.BaseStream.Seek(sectionDataoffs - 24, SeekOrigin.Begin);
            byte[] secBytes = sectionReader.ReadBytes((int)sectionSize);
            section[sectionName] = (new MemoryStream(secBytes), (int)sectionSize);
        }

        // .data
        (MemoryStream dataBuffer, int dataSize) = section[".data"];
        using var dataReader = new BinaryReader(dataBuffer, Encoding.UTF8, leaveOpen: true);
        var dataHeader = Unpack(dataReader, "III");
        uint datasize = (uint)dataHeader[0];
        uint memsize = (uint)dataHeader[1];
        uint dataFileOffset = (uint)dataHeader[2];

        // .names
        (MemoryStream namesBuffer, int namesSize) = section[".names"];
        using var namesReader = new BinaryReader(namesBuffer, Encoding.UTF8, leaveOpen: true);

        // .publics
        (MemoryStream publicsBuffer, int publicsSize) = section[".publics"];
        using var publicsReader = new BinaryReader(publicsBuffer, Encoding.UTF8, leaveOpen: true);
        while (publicsReader.BaseStream.Position < publicsSize)
        {
            var pub = Unpack(publicsReader, "II");
            uint publicsAddr = (uint)pub[0];
            uint publicsNameOff = (uint)pub[1];
            string name = ReadNullTermString(namesReader, publicsNameOff);
            result.Publics.Add(name);
        }

        // .natives
        (MemoryStream nativesBuffer, int nativesSize) = section[".natives"];
        using var nativesReader = new BinaryReader(nativesBuffer, Encoding.UTF8, leaveOpen: true);
        for (int i = 0; i < nativesSize / 4; i++)
        {
            var nat = Unpack(nativesReader, "I");
            uint nativeNameOff = (uint)nat[0];
            string name = ReadNullTermString(namesReader, nativeNameOff);
            result.Natives.Add(name);
        }

        // .pubvars
        (MemoryStream pubvarsBuffer, int pubvarsSize) = section[".pubvars"];
        using var pubvarsReader = new BinaryReader(pubvarsBuffer, Encoding.UTF8, leaveOpen: true);
        while (pubvarsReader.BaseStream.Position < pubvarsSize)
        {
            var pubvar = Unpack(pubvarsReader, "II");
            uint pubvarAddr = (uint)pubvar[0];
            uint pubvarNameOff = (uint)pubvar[1];
            string pubvarName = ReadNullTermString(namesReader, pubvarNameOff);
            result.Pubvars[pubvarName] = pubvarAddr;
        }

        // plugin's data section
        dataBuffer.Seek(dataFileOffset, SeekOrigin.Begin);
        var pluginDataBytes = new byte[dataBuffer.Length - dataFileOffset];
        dataBuffer.ReadExactly(pluginDataBytes, 0, pluginDataBytes.Length);
        var pluginDataBuffer = new MemoryStream(pluginDataBytes);
        using var pluginDataReader = new BinaryReader(pluginDataBuffer, Encoding.UTF8, leaveOpen: true);

        // plugin info
        if (result.Pubvars.TryGetValue("myinfo", out uint myInfoOffs))
        {
            var myinfo = Unpack(pluginDataReader, "IIIII", myInfoOffs);
            result.MyInfo.Name = ReadNullTermString(pluginDataReader, (uint)myinfo[0]);
            result.MyInfo.Description = ReadNullTermString(pluginDataReader, (uint)myinfo[1]);
            result.MyInfo.Author = ReadNullTermString(pluginDataReader, (uint)myinfo[2]);
            result.MyInfo.Version = ReadNullTermString(pluginDataReader, (uint)myinfo[3]);
            result.MyInfo.Url = ReadNullTermString(pluginDataReader, (uint)myinfo[4]);
        }

        if (result.Pubvars.TryGetValue("__version", out uint versionOffs))
        {
            var versionCells = Unpack(pluginDataReader, "IIII", versionOffs);
            uint cellVersion = (uint)versionCells[0];
            uint cellSmvers = (uint)versionCells[1];
            uint cellDate = (uint)versionCells[2];
            uint cellTime = (uint)versionCells[3];

            result.FileVersion = cellVersion;
            if (cellVersion >= 4)
            {
                result.CompileDate = ReadNullTermString(pluginDataReader, cellDate);
                result.CompileTime = ReadNullTermString(pluginDataReader, cellTime);
            }
            if (cellVersion > 4)
            {
                result.CompileVersion = ReadNullTermString(pluginDataReader, cellSmvers);
            }
        }

        // .code
        (MemoryStream codeBuffer, int codeSize) = section[".code"];
        using var codeReader = new BinaryReader(codeBuffer, Encoding.UTF8, leaveOpen: true);
        var codeHeader = Unpack(codeReader, "IBBHII");
        uint offsCode = (uint)codeHeader[5];

        // hash .code section from offs_code
        using var md5 = MD5.Create();
        codeBuffer.Seek(offsCode, SeekOrigin.Begin);
        byte[] codeHash = md5.ComputeHash(codeBuffer);

        // hash .data section
        pluginDataBuffer.Seek(0, SeekOrigin.Begin);
        byte[] dataHash = md5.ComputeHash(pluginDataBuffer);

        // plugin hash = codeHash ^ dataHash
        byte[] pluginHash = new byte[codeHash.Length];
        for (int i = 0; i < codeHash.Length; i++)
            pluginHash[i] = (byte)(codeHash[i] ^ dataHash[i]);

        result.CodeHash = Convert.ToHexString(codeHash).ToLowerInvariant();
        result.DataHash = Convert.ToHexString(dataHash).ToLowerInvariant();
        result.PluginHash = Convert.ToHexString(pluginHash).ToLowerInvariant();

        return result;
    }

    // Helper: Unpack binary data from a stream using struct-like format
    private static object[] Unpack(BinaryReader reader, string fmt, long? offset = null)
    {
        if (offset.HasValue)
            reader.BaseStream.Seek(offset.Value, SeekOrigin.Begin);

        var result = new List<object>();
        int i = 0;
        while (i < fmt.Length)
        {
            char c = fmt[i];
            int count = 1;
            // Support for repeat counts (e.g., '4I')
            int j = i;
            while (j < fmt.Length && char.IsDigit(fmt[j]))
                j++;
            if (j > i)
            {
                count = int.Parse(fmt.Substring(i, j - i));
                c = fmt[j];
                i = j;
            }
            switch (c)
            {
                case 'I': // uint32
                    for (int k = 0; k < count; k++)
                        result.Add(reader.ReadUInt32());
                    break;
                case 'H': // uint16
                    for (int k = 0; k < count; k++)
                        result.Add(reader.ReadUInt16());
                    break;
                case 'B': // byte
                    for (int k = 0; k < count; k++)
                        result.Add(reader.ReadByte());
                    break;
                default:
                    throw new NotSupportedException($"Format char '{c}' not supported.");
            }
            i++;
        }
        return result.ToArray();
    }

    // Helper: Read a null-terminated string from a stream
    private static string ReadNullTermString(BinaryReader reader, long? offset = null, Encoding? encoding = null)
    {
        if (offset.HasValue)
            reader.BaseStream.Seek(offset.Value, SeekOrigin.Begin);

        var bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0)
            bytes.Add(b);

        return (encoding ?? Encoding.UTF8).GetString(bytes.ToArray());
    }

    private static byte[] DecompressZlib(byte[] data)
    {
        using var ms = new MemoryStream(data);
        ms.ReadByte(); // skip zlib header
        ms.ReadByte();
        using var ds = new DeflateStream(ms, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        ds.CopyTo(outMs);
        return outMs.ToArray();
    }
}