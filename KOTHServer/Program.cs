using System;

namespace KOTHServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var repository = new PlayerRepository(Settings._connectionString);
            var server = new HttpServer(Settings._hostAdress, repository);
            server.Start();
            Console.WriteLine($@"[SERVER] Started at {Settings._hostAdress}"); // По хорошему над дергать с HttpServer а не Сеттинга
            Console.WriteLine("Press any key to stop the server..."); // А как нынче жить...
            Console.ReadLine();
            server.Stop();
            // Антибабахи нету, в неудобном случае придется ребутать
        }
    }
}