using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    internal class Archiver : IDisposable
    {
        public ArchiverParameters ArchiverParameters { get; private set; }

        private SmartThreadManager<Block> SmartThreadManager;

        private Boolean IsCancelled;

        public Archiver(ArchiverParameters ArchiverParameters)
        {
            this.ArchiverParameters = ArchiverParameters;

            SmartThreadManager = new SmartThreadManager<Block>();

            IsCancelled = false;
        }

        public void Process()
        {
            // создание файловых потоков и обработка
            try
            {
                using (FileStream Input = new FileStream(ArchiverParameters.InputFilePath, FileMode.Open, FileAccess.Read))
                using (FileStream Output = new FileStream(ArchiverParameters.OutputFilePath, FileMode.Create, FileAccess.Write))
                {
                    switch (ArchiverParameters.Mode)
                    {
                        case CompressionMode.Compress: Compress(Input, Output); break;
                        case CompressionMode.Decompress: Decompress(Input, Output); break;
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine($"Ошибка. {E.Message}");
                Console.ReadKey();

                Environment.Exit(1);
            }

            // если было событие отмены - удаление файла и выход
            if (IsCancelled)
            {
                Console.WriteLine($"Обработка прервана.");

                try
                {
                    File.Delete(ArchiverParameters.OutputFilePath);

                    Console.WriteLine($"Файл {ArchiverParameters.OutputFilePath} удалён.");
                    Console.ReadKey();

                    Environment.Exit(0);
                }
                catch (Exception E)
                {
                    Console.WriteLine($"Файл {ArchiverParameters.OutputFilePath} не может быть удалён. {E.Message}");
                    Console.ReadKey();

                    Environment.Exit(1);
                }
            }
        }

        // метод сжатия архива
        private void Compress(FileStream Input, FileStream Output)
        {
            using (BinaryWriter BinaryWriter = new BinaryWriter(Output))
            {
                // размер блока
                Int32 SourceSize = 1024 * 1024;

                // количество блоков
                Int32 BlocksCount = (Int32)(Input.Length / SourceSize) + (Input.Length % SourceSize != 0L ? 1 : 0);

                // запись количества блоков в файл
                BinaryWriter.Write(BlocksCount);

                // цикл по всем блокам
                for (Int32 Index = 0; Index < BlocksCount; ++Index)
                {
                    // новый блок
                    Block Block = new Block();

                    Block.SourceOffset = Index * SourceSize;
                    Block.SourceSize = SourceSize;

                    // новая задача
                    Task<Block> Task = new Task<Block>(Block, (CurrentBlock) =>
                    {
                        try
                        {
                            CurrentBlock.SourceData = new Byte[CurrentBlock.SourceSize];

                            // поиск и копирование блока в исходном файле
                            lock (Input)
                            {
                                Input.Seek(CurrentBlock.SourceOffset, SeekOrigin.Begin);
                                CurrentBlock.SourceSize = Input.Read(CurrentBlock.SourceData, 0, CurrentBlock.SourceSize);
                            }

                            CurrentBlock.Compress();

                            // запись сжатого блока и информации о нём
                            CurrentBlock.CompressedOffset = Output.Position + CurrentBlock.SizeOfBlock;

                            lock (Output)
                            {
                                BinaryWriter.Write(CurrentBlock.SourceOffset);
                                BinaryWriter.Write(CurrentBlock.SourceSize);
                                BinaryWriter.Write(CurrentBlock.CompressedOffset);
                                BinaryWriter.Write(CurrentBlock.CompressedSize);

                                Output.Write(CurrentBlock.CompressedData, 0, CurrentBlock.CompressedSize);
                            }
                        }
                        catch (Exception E)
                        {
                            // ошибка - выход
                            Console.WriteLine($"Внутренняя ошибка. {E.Message}");
                            Console.ReadKey();

                            Environment.Exit(1);
                        }
                    });

                    // добавляем задачу в менеджер потоков
                    SmartThreadManager.Enqueue(Task);
                }

                // запускаем менеджер потоков
                SmartThreadManager.Start();

                SmartThreadManager.Wait();
            }
        }

        // метод разжатия архива
        private void Decompress(FileStream Input, FileStream Output)
        {
            using (BinaryReader BinaryReader = new BinaryReader(Input))
            {
                // чтение количества блоков из файла
                Int32 BlocksCount = BinaryReader.ReadInt32();

                // цикл по всем блокам
                for (Int32 Index = 0; Index < BlocksCount; ++Index)
                {
                    // новый блок
                    Block Block = new Block();

                    // новая задача
                    Task<Block> Task = new Task<Block>(Block, (CurrentBlock) =>
                    {
                        try
                        {
                            // чтение данных о блоке и чтение самого блока
                            lock (Input)
                            {
                                CurrentBlock.SourceOffset = BinaryReader.ReadInt64();
                                CurrentBlock.SourceSize = BinaryReader.ReadInt32();
                                CurrentBlock.CompressedOffset = BinaryReader.ReadInt64();
                                CurrentBlock.CompressedSize = BinaryReader.ReadInt32();

                                CurrentBlock.CompressedData = new Byte[CurrentBlock.CompressedSize];

                                Input.Seek(CurrentBlock.CompressedOffset, SeekOrigin.Begin);
                                CurrentBlock.CompressedSize = Input.Read(CurrentBlock.CompressedData, 0, CurrentBlock.CompressedSize);
                            }

                            CurrentBlock.Decompress();

                            // запись блока в файл выхода
                            lock (Output)
                            {
                                Output.Seek(CurrentBlock.SourceOffset, SeekOrigin.Begin);
                                Output.Write(CurrentBlock.SourceData, 0, CurrentBlock.SourceSize);
                            }
                        }
                        catch (Exception E)
                        {
                            // ошибка - выход
                            Console.WriteLine($"Error. {E.Message}");
                            Console.ReadKey();

                            Environment.Exit(1);
                        }
                    });

                    // добавляем задачу в менеджер потоков
                    SmartThreadManager.Enqueue(Task);
                }

                // запускаем менеджер потоков
                SmartThreadManager.Start();

                // ждем завершения всех задач
                SmartThreadManager.Wait();
            }
        }

        public void Cancel()
        {
            // в случае отмены - остановка всех потоков
            SmartThreadManager.Stop();

            IsCancelled = true;
        }

        public void Dispose()
        {
            SmartThreadManager.Dispose();
        }
    }
}
