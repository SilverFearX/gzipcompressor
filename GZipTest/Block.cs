using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    internal class Block
    {
        // исходный блок
        public Byte[] SourceData;

        // смещение блока в исходном файле
        public Int64 SourceOffset;

        // размер блока в исходном файле
        public Int32 SourceSize;

        // сжатый блок
        public Byte[] CompressedData;

        // смещение блока в сжатом файле
        public Int64 CompressedOffset;

        // размер блока в сжатом файле
        public Int32 CompressedSize;

        // конструктор
        public Block()
        {
            SourceData = null;
            SourceOffset = -1;
            SourceSize = -1;
            CompressedData = null;
            CompressedOffset = -1;
            CompressedSize = -1;
        }

        // размер
        public Int32 SizeOfBlock => 6 * sizeof(Int32); // исключая ссылки на массивы

        // метод сжатия блока
        public void Compress()
        {
            using (MemoryStream Output = new MemoryStream())
            {
                using (GZipStream CompressionStream = new GZipStream(Output, CompressionMode.Compress))
                {
                    CompressionStream.Write(SourceData, 0, SourceSize);
                }

                CompressedData = Output.ToArray();
                CompressedSize = CompressedData.Length;
            }

            SourceData = null;
        }

        // метод разжатия блока
        public void Decompress()
        {
            using (MemoryStream Input = new MemoryStream(CompressedData, 0, CompressedSize))
            {
                using (GZipStream DecompressionStream = new GZipStream(Input, CompressionMode.Decompress))
                {
                    using (MemoryStream Output = new MemoryStream())
                    {
                        Int32 CurrentSize = 1024;

                        Byte[] Buffer = new Byte[CurrentSize];

                        do
                        {
                            CurrentSize = DecompressionStream.Read(Buffer, 0, Buffer.Length);
                            Output.Write(Buffer, 0, CurrentSize);
                        }
                        while (CurrentSize != 0);

                        SourceData = Output.ToArray();
                    }
                }
            }

            CompressedData = null;
        }
    }
}
