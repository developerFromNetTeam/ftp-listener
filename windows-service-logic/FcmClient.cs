using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace windows_service_logic
{
    public class FcmClient
    {
        private const string url = "https://fcm.googleapis.com/fcm/send";

        private const string host = "fcm.googleapis.com";

        private readonly string firebaseServerKey;

        public FcmClient()
        {
            var firebaseServerKey = ConfigurationSettings.AppSettings["firebaseServerKey"];

            if (string.IsNullOrWhiteSpace(firebaseServerKey))
            {
                throw new Exception("Firebase server key is empty.");
            }

            this.firebaseServerKey = firebaseServerKey;
        }

        public async Task<string> SendNotificationAsync(NotificationPayload notification)
        {
            return await this.CallFcm(this.GenerateStringContent(notification));
        }

        private StringContent GenerateStringContent(dynamic model)
        {
            var jsonModel = JsonConvert.SerializeObject(model);

            return new StringContent(jsonModel, Encoding.UTF8, "application/json");
        }

        private async Task<string> CallFcm(HttpContent content)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.TryAddWithoutValidation("Authorization",
                    string.Format(CultureInfo.InvariantCulture, "key={0}", this.firebaseServerKey));
                requestMessage.Headers.TryAddWithoutValidation("Host", host);
                requestMessage.Content = content;

                using (var httpMessageHandler = new WebRequestHandler())
                {
                    httpMessageHandler.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

                    var httpClient = new HttpClient(httpMessageHandler);
                    var httpResponseMessage = await httpClient.SendAsync(requestMessage);
                    var response = await httpResponseMessage.Content.ReadAsStringAsync();

                    //if (string.IsNullOrEmpty(response))
                    //{
                    //    throw new EnumException<FcmTopicActionResponseError>(FcmTopicActionResponseError.EmptyResponse);
                    //}

                    //return JsonConvert.DeserializeObject<T>(response);
                    return response;
                }
            }
        }
    }
}
