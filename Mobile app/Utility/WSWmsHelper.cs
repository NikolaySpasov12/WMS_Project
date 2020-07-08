using Android.App;
using Android.Content;
using Android.Widget;
using mstore_WMS.Models;
using mstore_WMS.Models.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace mstore_WMS.Utils
{
    public static class WSWmsHelper
    {
        public const string WS_RETURN_OK = "[OK]";
        public const string WS_RETURN_FULL = "[FULL]";
        public const string WS_RETURN_SUCCESS = "[Success]";
        public const string WS_RETURN_ERROR = "[Error]";
        public const string WS_RETURN_NO_DATA = "[NO DATA]";
        public const string WS_TASK_CANCELLED = "[A task was canceled.]";

        public const string WS_RETURN_EXISTS = "[1]";
        public const string WS_RETURN_NOT_EXISTS = "[0]";
        public const string WS_RETURN_NOT_VALID = "[9]";

        public const string WS_RETURN_TRUE = "[true]";
        public const string WS_RETURN_FALSE = "[false]";

        public const int RET_EXISTS = 1;
        public const int RET_NOT_EXISTS = 0;
        public const int RET_NOT_VALID = 9;


        private const string AUTHERROR = "User not authenticated";
        private const string TIMEOUTERROR = "Execution timed out!";
        private const string SERVICEERROR = "Execution in web service returned error!";
        private const string SERVICEERROR_WEIGH = "No connection to scale!";

        private static string GetRequest(string pathName)
        {
            HttpConnectWmsRest httpConnect = new HttpConnectWmsRest(pathName);
            string request;

            try
            {
                request = AsyncContext.Run(() => httpConnect.GetRequest());

                if (!string.IsNullOrEmpty(request))
                {
                    request = "[" + request + "]";
                }
            }
            catch (Exception ex)
            {
                request = WS_RETURN_ERROR;
                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
            }

            return request;
        }

        private static Task<string> GetRequestAsync(string pathName)
        {
            HttpConnectWmsRest httpConnect = new HttpConnectWmsRest(pathName);
            string request;

            try
            {
                request = AsyncContext.Run(async () => await httpConnect.GetRequest(10));

                if (!string.IsNullOrEmpty(request))
                {
                    request = "[" + request + "]";
                }
            }
            catch (Exception ex)
            {
                request = WS_RETURN_ERROR;
                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
            }

            return Task.FromResult(request);
        }

        private static string PostRequest(string pathName, string content)
        {
            HttpConnectWmsRest httpConnect = new HttpConnectWmsRest(pathName, content);
            string request;

            try
            {
                request = AsyncContext.Run(() => httpConnect.PostRequest());

                if (!string.IsNullOrEmpty(request))
                {
                    request = "[" + request + "]";
                }
            }
            catch (Exception ex)
            {
                request = WS_RETURN_ERROR;
                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
            }

            return request;
        }

        public static DataTable SelectWeEinlagerungDetailHeader(string tuNo)
        {
            DataTable table = new DataTable();
            string request = GetRequest("selectWeEinlagerungDetailHeader?tuNo=" + tuNo);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {

                List<AllocationLineDetHeaderDto> list = JsonConvert.DeserializeObject<List<AllocationLineDetHeaderDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static DataTable SelectWEeinlagerungList(string tuNo)
        {
            DataTable table = new DataTable();
            string request = GetRequest("selectWEeinlagerungList?tuNo=" + tuNo);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {
                List<AllocationLineDto> list = JsonConvert.DeserializeObject<List<AllocationLineDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static DataTable SelectWeEinlagerungDetailList(string tuNo, string expectedBin, string currentBin)
        {
            DataTable table = new DataTable();
            string request = GetRequest("selectweeinlagerungdetaillist?tuNo=" + tuNo + "&expectedBin=" + expectedBin + "&currentBin=" + currentBin);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {
                List<AllocationLineDetailDto> list = JsonConvert.DeserializeObject<List<AllocationLineDetailDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static string WE_EinlagerungUpdateItemQuantitytoHandle(UpdateEinlagerungQuantityToHandleDto data, ref string newVirtualLeCreated)
        {
            string request = PostRequest("weeinlagerungupdateitemquantitytohandle", JsonConvert.SerializeObject(data));
            if (request.Contains("##"))
            {
                string[] parts = request.Split(new[] { "##" }, StringSplitOptions.RemoveEmptyEntries);

                request = parts[0];
                newVirtualLeCreated = parts[1];
            }

            return request;
        }

        public static string MarkWeEinlagerungInProcess(string headerNo, string tuNr, string OriginalBinCode, string NewBinCode, string tvQuellFach, string itemNo)
        {
            string retVal = string.Empty;
            string request = GetRequest("markweeinlagerunginprocess?headerNo=" + headerNo + "&tuNr=" + tuNr + "&OriginalBinCode=" + OriginalBinCode + "&NewBinCode=" + NewBinCode + "&tvQuellFach=" + tvQuellFach + "&scannedItemNo=" + itemNo);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {
                retVal = request.Replace("[", string.Empty).Replace("]", string.Empty);
            }

            return retVal;
        }

        public static string GetBinData(string barcode)
        {
            string retVal = string.Empty;
            string request = GetRequest("getbindata?barcode=" + barcode);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {
                retVal = request.Replace("[", string.Empty).Replace("]", string.Empty);
            }

            return retVal;
        }

        public static DataTable CreateAndLoadTransportUnitDetDto(string leNummer, string targetBin)
        {
            DataTable table = new DataTable();
            string request = GetRequest("createAndLoadTransportUnitDetDTO?leNummer=" + leNummer + "&targetBin=" + targetBin);
            if (!string.IsNullOrEmpty(request) && !request.Contains(WS_RETURN_ERROR))
            {
                List<LoadTransportUnitDetDto> list = JsonConvert.DeserializeObject<List<LoadTransportUnitDetDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        #endregion 

        #region Transport

        public static DataTable LoadTransportAids()
        {
            DataTable table = new DataTable();
            string request = GetRequest("loadTransportAids");
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request.Contains(WS_RETURN_ERROR))
                {
                    throw new Exception(SERVICEERROR);
                }

                List<TransportNummerDto> list = JsonConvert.DeserializeObject<List<TransportNummerDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static DataTable LoadTransportOrders(int transportAidId)
        {
            DataTable table = new DataTable();
            string request = GetRequest("loadTransportOrders?leId=" + transportAidId);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request.Contains(WS_RETURN_ERROR))
                {
                    throw new Exception(SERVICEERROR);
                }

                List<TransportOrderDto> list = JsonConvert.DeserializeObject<List<TransportOrderDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static DataTable GetTransport(int transportAidId)
        {
            DataTable table = new DataTable();
            string request = GetRequest("getTransport?transpId=" + transportAidId);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request.Contains(WS_RETURN_ERROR))
                {
                    throw new Exception(SERVICEERROR);
                }

                List<TransportOrderDto> list = JsonConvert.DeserializeObject<List<TransportOrderDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        public static bool PickTransport(string trId, int TranspAidId)
        {
            bool retval = false;
            string request = GetRequest("pickTransport?trId=" + trId + "&trAidId=" + TranspAidId);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request == WS_RETURN_SUCCESS)
                {
                    retval = true;
                }
            }

            return retval;
        }

        public static bool FinishTransport(int trId)
        {
            bool retval = false;
            string request = GetRequest("finishTransport?trId=" + trId);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request == WS_RETURN_SUCCESS)
                {
                    retval = true;
                }
            }

            return retval;
        }

        public static DataTable GetTransportLoadingCount(int transpAidID)
        {
            DataTable table = new DataTable();
            string request = GetRequest("getTransportLoadingCount?leId=" + transpAidID);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request.Contains(WS_RETURN_ERROR))
                {
                    throw new Exception(SERVICEERROR);
                }

                var list = JsonConvert.DeserializeObject<List<TransportLoadingCountDto>>(request, GetDeserializeSettings());
                table = ToDataTable(list);
            }

            return table;
        }

        

        #endregion

     
        public static List<BestListInfoDto> GetBestList(string artNummer, string standortName)
        {
            string request = GetRequest("getBestList?artNummer=" + artNummer + "&standortName=" + standortName);
            if (!string.IsNullOrEmpty(request))
            {
                if (request.Contains(WS_TASK_CANCELLED))
                {
                    throw new Exception(TIMEOUTERROR);
                }

                if (request.Contains(WS_RETURN_ERROR))
                {
                    throw new Exception(SERVICEERROR);
                }
            }

            if (string.IsNullOrEmpty(request))
            {
                throw new Exception(WS_RETURN_NO_DATA);
            }
            var res = JsonConvert.DeserializeObject<List<BestListInfoDto>>(request, GetDeserializeSettings());

            if (res.Count < 1)
            {
                return null;
            }

            return res;
        }

        #endregion

        public static string LoadConfiguration(string confSection, string confName)
        {
            var configuration = RequestToString(GetRequest($"loadConfiguration?section={confSection}&name={confName}"));
            return configuration;
        }
    }
}