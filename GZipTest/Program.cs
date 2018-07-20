using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;

namespace GZipTest
{
    class Program
    {
        static void Main(String[] Arguments)
        {
            DateTime StartTime = DateTime.Now;

            Arguments = new String[3];

            Arguments[0] = "compress";
            Arguments[1] = @"D:\1.bin";
            Arguments[2] = @"D:\2.bin.gz";

            try
            {
                using (Archiver Archiver = new Archiver(new ArchiverParameters(Arguments)))
                {
                    Console.CancelKeyPress += (o, e) =>
                    {
                        e.Cancel = true;

                        Archiver.Cancel();
                    };

                    Archiver.Process();
                }
            }
            catch (Exception E)
            {
                Console.WriteLine($"Ошибка. {E.Message}");
                Console.ReadKey();

                Environment.Exit(1);
            }

            Console.WriteLine($"Время выполнения = {(DateTime.Now - StartTime).TotalSeconds} секунд.");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }
}