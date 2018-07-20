using System;
using System.IO.Compression;

namespace GZipTest
{
    internal class ArchiverParameters
    {
        public CompressionMode Mode { get; private set; }

        public String InputFilePath { get; private set; }

        public String OutputFilePath { get; private set; }

        public ArchiverParameters(String[] Arguments)
        {
            if (Arguments.Length != 3)
                throw new Exception("Неверное количество параметров. Использование GZipTest.exe [compress/decompress] [имя исходного файла] [имя результирующего файла].");

            InputFilePath = Arguments[1];
            OutputFilePath = Arguments[2];

            switch (Arguments[0])
            {
                case "compress": Mode = CompressionMode.Compress; break;
                case "decompress": Mode = CompressionMode.Decompress; break;
                default: throw new Exception("Некорректный режим работы.");
            }
        }
    }

}
