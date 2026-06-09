using System;
using System.IO;
using System.Text;

namespace LLamaCppLauncher.Services;

public class GgufMetadataService
{
    private const uint GGUF_MAGIC = 0x46475547;

    private const int TYPE_UINT8 = 0;
    private const int TYPE_INT8 = 1;
    private const int TYPE_UINT16 = 2;
    private const int TYPE_INT16 = 3;
    private const int TYPE_UINT32 = 4;
    private const int TYPE_INT32 = 5;
    private const int TYPE_FLOAT32 = 6;
    private const int TYPE_BOOL = 7;
    private const int TYPE_STRING = 8;
    private const int TYPE_UINT64 = 9;
    private const int TYPE_INT64 = 10;
    private const int TYPE_FLOAT64 = 11;
    private const int TYPE_ARRAY = 12;

    public (string architecture, ulong parameterCount, ulong contextLength) ReadMetadata(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var magic = reader.ReadUInt32();
            if (magic != GGUF_MAGIC)
                return (string.Empty, 0, 0);

            var version = reader.ReadUInt32();
            if (version < 2 || version > 3)
                return (string.Empty, 0, 0);

            var tensorCount = reader.ReadUInt64();
            var metadataKvCount = reader.ReadUInt64();

            string architecture = string.Empty;
            ulong parameterCount = 0;
            ulong contextLength = 0;

            for (ulong i = 0; i < metadataKvCount; i++)
            {
                var key = ReadString(reader);
                var valueType = reader.ReadUInt32();

                if (key == "general.architecture" && valueType == TYPE_STRING)
                {
                    architecture = ReadString(reader);
                }
                else if (key == "general.parameter_count" && valueType == TYPE_UINT64)
                {
                    parameterCount = reader.ReadUInt64();
                }
                else if (key.EndsWith(".context_length") && valueType == TYPE_UINT32)
                {
                    contextLength = reader.ReadUInt32();
                }
                else
                {
                    if (!SkipValue(reader, valueType))
                        break;
                }
            }

            return (architecture, parameterCount, contextLength);
        }
        catch
        {
            return (string.Empty, 0, 0);
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt64();
        if (length > 10_000_000)
            return string.Empty;
        var bytes = reader.ReadBytes((int)length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static bool SkipValue(BinaryReader reader, uint type)
    {
        switch (type)
        {
            case TYPE_UINT8:
            case TYPE_INT8:
            case TYPE_BOOL:
                reader.ReadByte();
                return true;
            case TYPE_UINT16:
            case TYPE_INT16:
                reader.ReadUInt16();
                return true;
            case TYPE_UINT32:
            case TYPE_INT32:
            case TYPE_FLOAT32:
                reader.ReadUInt32();
                return true;
            case TYPE_UINT64:
            case TYPE_INT64:
            case TYPE_FLOAT64:
                reader.ReadUInt64();
                return true;
            case TYPE_STRING:
                var len = reader.ReadUInt64();
                if (len > 10_000_000) return false;
                reader.ReadBytes((int)len);
                return true;
            case TYPE_ARRAY:
                var arrayType = reader.ReadUInt32();
                var arrayLen = reader.ReadUInt64();
                if (arrayLen > 1_000_000) return false;
                for (ulong i = 0; i < arrayLen; i++)
                {
                    if (!SkipValue(reader, arrayType))
                        return false;
                }
                return true;
            default:
                return false;
        }
    }
}
