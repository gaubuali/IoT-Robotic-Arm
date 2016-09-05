using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace WebControlService
{
    public sealed class WebControlService : IBackgroundTask
    {
        private AppServiceConnection appServiceConnection;
        private BackgroundTaskDeferral serviceDeferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;

            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null &&
                appService.Name == "WebControlService")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;  

            switch (command)
            {
                case "Initialize":
                    var messageDeferral = args.GetDeferral();
                    var returnMessage = new ValueSet();
                    HttpServer myServer = new HttpServer(8003, appServiceConnection);
                    IAsyncAction asyncAction = ThreadPool.RunAsync(
                        (workItem) =>
                        {
                            myServer.StartServer();
                        });
                    returnMessage.Add("Status", "Success");
                    var responsStatus = await args.Request.SendResponseAsync(returnMessage);
                    messageDeferral.Complete();
                    break;
                case "Quit":
                    serviceDeferral.Complete();
                    break;
                default:break;
            }
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            throw new NotImplementedException();
        }
    }
}
