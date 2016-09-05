using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace WebControlService
{
    public sealed class HttpServer
    {
        private readonly StreamSocketListener _listener;
        private AppServiceConnection _appServiceConnection;
        private int _port = 0;
        private const uint BufferSize = 8192;
        private AppServiceConnection appServiceConnection;

        public HttpServer(int port, AppServiceConnection connection)
        {
            _listener = new StreamSocketListener();
            _port = port;
            _appServiceConnection = connection;
            _listener.ConnectionReceived += ((s, e) =>
            {
                ProcessRequestAsync(e.Socket);
            }); 
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            StringBuilder request = new StringBuilder();
            using (IInputStream input = socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            using (IOutputStream output = socket.OutputStream)
            {
                string requestMethod = request.ToString().Split('\n')[0];
                string[] requestParts = requestMethod.Split(' ');
                if (requestParts[0] == "GET")
                {
                    await WriteResponseAsync(requestParts[1], output);
                }
                else
                    throw new InvalidDataException("HTTP method not supported: "
                                                   + requestParts[0]);
            }
        }

        private async Task WriteResponseAsync(string v, IOutputStream output)
        {
            string state = "Unspecified";
            bool stateChanged = false;
            if (v.Contains("dcmotor.html?state=on"))
            {
                state = "On";
                stateChanged = true;
            }
            else if (v.Contains("dcmotor.html?state=off"))
            {
                state = "Off";
                stateChanged = true;
            }
            if (stateChanged)
            {
                var updateMessage = new ValueSet();
                updateMessage.Add("State", state);
                var responseStatus = await appServiceConnection.SendMessageAsync(updateMessage);
            }
            string html = state == "On" ? onHtmlString : offHtmlString;
            // Show the html 
            using (Stream resp = output.AsStreamForWrite())
            {
                // Look in the Data subdirectory of the app package
                byte[] bodyArray = Encoding.UTF8.GetBytes(html);
                MemoryStream stream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: {0}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        public void StartServer()
        {
#pragma warning disable CS4014
            _listener.BindServiceNameAsync(_port.ToString());
#pragma warning restore CS4014
        }
        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
