using System;
using System.Threading.Tasks;
using Dapr.Client;

namespace RvClientApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Dapr SDK 클라이언트 생성
            using var daprClient = new DaprClientBuilder().Build();

            string serviceName = "rv-gateway"; // Dapr app-id (rv-gateway Deployment에서 지정)
            string methodName = "inventory";    // GatewayController의 route (POST /invoke/inventory)

            // 호출할 데이터
            var requestBody = new { productId = "A001" };

            try
            {
                // Service Invocation: POST
                var response = await daprClient.InvokeMethodAsync<object, string>(
                    serviceName: serviceName,
                    methodName: methodName,
                    body: requestBody,
                    httpMethod: System.Net.Http.HttpMethod.Post);

                Console.WriteLine("RV Gateway Response:");
                Console.WriteLine(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error invoking rv-gateway:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
