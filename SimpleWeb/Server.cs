using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SimpleWeb
{
    class Server
    {
        private StreamSocketListener listener;
        private const uint BufferSize = 8192;
        private Stepper StepperA;
        private Stepper StepperB;
        private Stepper StepperC;
        public Server()
        {
        }
        public void Initialise()
        {
            listener = new StreamSocketListener();

#pragma warning disable CS4014
            listener.BindServiceNameAsync("80");
#pragma warning restore CS4014

            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);

            StepperA = new Stepper();
            StepperA.InitialiseGPIO(3);
            StepperB = new Stepper();
            StepperB.InitialiseGPIO(4);
            StepperC = new Stepper();
            StepperC.InitialiseGPIO(2);
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
            // Send a response back
            using (IOutputStream output = socket.OutputStream)
            {
                string requestMethod = request.ToString().Split('\n')[0];
                string[] requestParts = requestMethod.Split(' ');

                if (requestParts[0] == "GET")
                    await WriteResponseAsync(requestParts[1], output);
                else
                    throw new InvalidDataException("HTTP method not supported: "
                                                   + requestParts[0]);
                
            }
        }

        private async Task WriteResponseAsync(string v, IOutputStream output)
        {
            string sendPage = ParseInput(v);

            using (Stream response = output.AsStreamForWrite())
            {
                
                // For now we are just going to reply to anything with Hello World!
                byte[] bodyArray = Encoding.UTF8.GetBytes(sendPage);

                var bodyStream = new MemoryStream(bodyArray);

                // This is a standard HTTP header so the client browser knows the bytes returned are a valid http response
                var header = "HTTP/1.1 200 OK\r\n" +
                            $"Content-Length: {bodyStream.Length}\r\n" +
                                "Connection: close\r\n\r\n";

                byte[] headerArray = Encoding.UTF8.GetBytes(header);

                // send the header with the body inclded to the client
                await response.WriteAsync(headerArray, 0, headerArray.Length);
                await bodyStream.CopyToAsync(response);
                await response.FlushAsync();
            }
        }

        private string ParseInput(string v)
        {
            string retString = "";
            string[] splitString = v.Split('?');
            Stepper.RunningOption option1 = new Stepper.RunningOption();
            Stepper.RunningOption option2 = new Stepper.RunningOption();
            Stepper.RunningOption option3 = new Stepper.RunningOption();
            List<Stepper.RunningOption> listOption = new List<Stepper.RunningOption>();
            listOption.Add(option1);
            listOption.Add(option2);
            listOption.Add(option3);

            switch (splitString[0])
            {
                case "/index.html":
                    ParseIndexWebpage(splitString[1], listOption);
                    //if (option.RunInfitive) retString = File.ReadAllText("webpages\\stopmotor.html");
                    //else retString = File.ReadAllText("webpages\\index.html");
                    retString = File.ReadAllText("webpages\\index.html");
                    break;
                case "/stopmotor.html":
                    StepperA.ForceToStopMotor();
                    retString = File.ReadAllText("webpages\\mainpage.html");
                    break;
                case "/series.html":
                    GetSeriesMoves(splitString[1], listOption);
                    retString = File.ReadAllText("webpages\\index.html");
                    break;
                default:
                    retString = File.ReadAllText("webpages\\index.html");
                    break;
            }
            return retString;
        }

        private void GetSeriesMoves(string v, List<Stepper.RunningOption> listOption)
        {
            string textseries = v.Split('=')[1];
            textseries = textseries.Replace("%28","(");
            textseries = textseries.Replace("%2C", ",");
            textseries = textseries.Replace("%29", ")");
            textseries = textseries.Replace("%26", "&");
            string[] actions = textseries.Split(new string[] { "%0D%0A" }, StringSplitOptions.None);
            foreach(var item in actions)        // line
            {
                uint waitTime = 0;
                string[] joins = item.Split('&');
                foreach(var joinname in joins)      // joint
                {
                    string args = joinname.Replace('(', ',').Replace("(",",").Replace(")", "");
                    string[] arg = args.Split(',');
                    switch (joinname[0])            
                    {
                        case 'a':
                            SeriesParseJoint(listOption[0], arg);
                            break;
                        case 'b':
                            SeriesParseJoint(listOption[1], arg);
                            break;
                        case 'c':
                            SeriesParseJoint(listOption[2], arg);
                            break;
                        case 'w':
                            waitTime = Convert.ToUInt16(arg[1]);
                            break;
                        default: break;
                    }
                }
                StepperA.RunMotor(listOption[0]);
                StepperB.RunMotor(listOption[1]);
                StepperC.RunMotor(listOption[2]);
                Task.Delay(-1).Wait(10);
                if (waitTime > 0)
                {
                    while (StepperA.isBusy) Task.Delay(-1).Wait(10);
                    while (StepperB.isBusy) Task.Delay(-1).Wait(10);
                    while (StepperC.isBusy) Task.Delay(-1).Wait(10);
                }
                Task.Delay(-1).Wait((int)waitTime);
            }
        }

        private void SeriesParseJoint(Stepper.RunningOption opt, string[] args)
        {
            opt.Step = Convert.ToUInt16(args[1]);
            opt.StepDelay = Convert.ToUInt16(args[2]);
            if (args[3] == "l") opt.dir = Stepper.Direction.Left;
            else if (args[3] == "r") opt.dir = Stepper.Direction.Right;
        }

        private void ParseIndexWebpage(string v, List< Stepper.RunningOption> listoption)
        {
            string[] commands = v.Split('&');

            foreach ( var item in commands)
            {
                ParseCommand(item, listoption);
            }
            StepperA.RunMotor(listoption[0]);
            StepperB.RunMotor(listoption[1]);
            StepperC.RunMotor(listoption[2]);
        }

        private void ParseCommand(string item, List<Stepper.RunningOption> listoption)
        {
            string[] data = item.Split('=');
            switch (data[0])
            {
                case "pha1":
                    listoption[0].Step = Convert.ToUInt32(data[1]);
                    break;
                case "dir1":
                    if (data[1] == "l") listoption[0].dir = Stepper.Direction.Left;
                    else listoption[0].dir = Stepper.Direction.Right;
                    break;
                case "inf1":
                    if (data[1] == "on") listoption[0].RunInfitive = true;
                    else listoption[0].RunInfitive = false;
                    break;
                case "delay1":
                    listoption[0].StepDelay = Convert.ToUInt16(data[1]);
                    break;
                case "pha2":
                    listoption[1].Step = Convert.ToUInt32(data[1]);
                    break;
                case "dir2":
                    if (data[1] == "l") listoption[1].dir = Stepper.Direction.Left;
                    else listoption[1].dir = Stepper.Direction.Right;
                    break;
                case "inf2":
                    if (data[1] == "on") listoption[1].RunInfitive = true;
                    else listoption[1].RunInfitive = false;
                    break;
                case "delay2":
                    listoption[1].StepDelay = Convert.ToUInt16(data[1]);
                    break;
                case "pha3":
                    listoption[2].Step = Convert.ToUInt32(data[1]);
                    break;
                case "dir3":
                    if (data[1] == "l") listoption[2].dir = Stepper.Direction.Left;
                    else listoption[2].dir = Stepper.Direction.Right;
                    break;
                case "inf3":
                    if (data[1] == "on") listoption[2].RunInfitive = true;
                    else listoption[2].RunInfitive = false;
                    break;
                case "delay3":
                    listoption[2].StepDelay = Convert.ToUInt16(data[1]);
                    break;
                default: break;
            }
        }
    }
}
