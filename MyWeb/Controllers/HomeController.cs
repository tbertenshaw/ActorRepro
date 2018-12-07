using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyWeb.Models;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Fabric;
using System.Fabric.Query;
using System.Threading;

namespace MyWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public HomeController(StatelessServiceContext serviceContext,
            FabricClient fabricClient)
        {
            this.serviceContext = serviceContext;
            this.fabricClient = fabricClient;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }
       
        public async Task<IActionResult> GetAsync()
        {
            string[] serviceUris = { "fabric:/ActorRepro/MyActorService" };
            var result = new List<CountViewModel>();

            for (int i = 0; i < serviceUris.Length; i++)
            {
                var uri = new Uri(serviceUris[i]);
                ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(uri);

                long count = 0;

                foreach (Partition partition in partitions)
                {
                    long partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;

                    IActorService actorServiceProxy = ActorServiceProxy.Create<IActorService>(new Uri(serviceUris[i]), partitionKey);

                    ContinuationToken continuationToken = null;

                    do
                    {
                        PagedResult<ActorInformation> page = await actorServiceProxy.GetActorsAsync(continuationToken, CancellationToken.None);
                        count += page.Items.Where(x => x.IsActive).LongCount();
                        
                        continuationToken = page.ContinuationToken;
                    }
                    while (continuationToken != null);
                }
                result.Add(new CountViewModel { ActorType = uri.LocalPath, Count = count });
            }

            return this.Json(result);
        }
        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public class CountViewModel
        {
            public string ActorType { get; set; }
            public long Count { get; set; }
        }
    }
}
