using Android.App;
using mstore_WMS.AppCode;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace mstore_WMS.Utils
{
    public class HttpConnectWmsRest
    {
        private readonly string authorizationSeq;
        private readonly string wsGlobalPath;

        private const string Name = "fa6fbcda4f1d317212";
        private const string Password = "vmju_yX7eGxb8Vr4e!";

        private string LoginUser { get; }
        private string LoginPassword { get; }
        private string Content { get; }

#if DEBUG        
        private const double TimeoutGetMethod = 4;
        private const double TimeoutPostMethod = 4;
#else
        private const double TimeoutGetMethod = 4;
        private const double TimeoutPostMethod = 4;
#endif
        private class TrustAllCertificatePolicy : ICertificatePolicy
        {
            public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem)
            {
                return true;
            }
        }

        public HttpConnectWmsRest(string path)
        {
            if (!string.IsNullOrEmpty(Utility.WS_ACTUAL_URL_PATH))
            {
                defaultWSUrl = Utility.WS_ACTUAL_URL_PATH + @"/";
            }

            ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
            wsGlobalPath = GetPathSequence(path);
            LoginUser = UserAuthentication.User;
            LoginPassword = UserAuthentication.Password;

            string androidId = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
            var plainTextBytes = Encoding.UTF8.GetBytes(Name + ":" + Password + ":" + androidId);
            authorizationSeq = "Basic " + Convert.ToBase64String(plainTextBytes);
        }

        public HttpConnectWmsRest(string path, string content) : this(path)
        {
            Content = content;
        }

        public async Task<string> GetRequest(double tout = TimeoutGetMethod)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(tout);

                string url = defaultWSUrl + wsGlobalPath;

                var message = new HttpRequestMessage(new HttpMethod("GET"), url);
                message.Headers.Add("Authorization", authorizationSeq);
                message.Headers.Add("U", LoginUser);
                message.Headers.Add("P", LoginPassword);
                string retMess;

                try
                {
                    var response = await client.SendAsync(message);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    retMess = responseBody;

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.Error.WriteLine("Error: GET to `{0}` not successful: {1}", url, responseBody);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    retMess = e.Message;
                }

                return retMess;
            }
        }

        public async Task<string> PostRequest()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(TimeoutPostMethod);

                var uri = new Uri(string.Format(defaultWSUrl + wsGlobalPath, string.Empty));

                var content = new StringContent(Content, Encoding.UTF8, "application/json");
                string retMess;
                try
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationSeq);
                    client.DefaultRequestHeaders.Add("UP", LoginUser + "##" + LoginPassword);
                    var response = await client.PostAsync(uri, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    retMess = responseBody;

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.Error.WriteLine("Error: GET to `{0}` not successful: {1}", uri, responseBody);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    retMess = e.Message;
                }

                return retMess;
            }
        }

        private static string GetPathSequence(string type)
        {
            string seq;

            switch (type)
            {
                case "login":
                    seq = "login";
                    break;
                default:
                    seq = type;
                    break;
            }

            return seq;
        }
    }
}