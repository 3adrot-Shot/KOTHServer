using System;
using System.Linq;
using System.Net;

namespace KOTHServer
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly RequestHandler _handler;

        public HttpServer(string url, PlayerRepository repository)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _handler = new RequestHandler(repository);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"[SERVER] Started at {_listener.Prefixes.First()}");
            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                _handler.HandleRequest(context);
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}