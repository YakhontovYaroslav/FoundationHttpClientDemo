using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoundationHttpClientDemo.Common;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FoundationHttpClientDemo.Server
{
    [Authorize]
    [HubName(Constants.HUB_NAME)]
    public class TestHub : Hub<IHubClient>, IHubServer
    {
        public Task SayHelloAsync(string message)
        {
            Console.WriteLine($"Client says: {message}");

            Clients.Caller.SayHello(message);

            return Task.CompletedTask;
        }
    }
}