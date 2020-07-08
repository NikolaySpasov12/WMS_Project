using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Text;
using mstore_WMS.Activities;
using mstore_WMS.Models.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mstore_WMS.Utils
{
    internal static class Utility
    {
        public const string WS_DEFAULT_PORT = "8080";

        public const string RET_STATUS_OK = "OK";
        public const string RET_STATUS_FULL = "FULL";
        public const string RET_STATUS_INVALID_QUANTITY = "INVALID_QUANTITY";
        public const string RET_STATUS_QUANTITY_OK = "QUANTITY_OK";
        public const string RET_STATUS_QUANTITY_LESS = "QUANTITY_LESS";
        public const string RET_STATUS_QUANTITY_MORE = "QUANTITY_MORE";
        public const string RET_STATUS_TU_USED = "TUUSED";

        public const string RET_STATUS_COMPLETED_NAV = "Completed";
        public const string RET_STATUS_FULL_NAV = "Full";

        public static string WS_ACTUAL_URL_PATH;
        public static char DecimalSeparator => DecimalFormatSymbols.Instance.DecimalSeparator;

        public const int SELECTED_INDEX_NONE = -1;

        const string CONF_WEIGHT_DIFFERENCE_PERCENT = "WEIGHT_DIFFERENCE_PERCENT";
        const string CONF_WEIGHT_DIFFERENCE_PERCENT_SECTION = "MFR";


        public static string BuildWsUrlFromServerIpAndPort(string serviceAddress)
        {
            WS_ACTUAL_URL_PATH = serviceAddress;
            return WS_ACTUAL_URL_PATH;
        }

        private static ScreenOrientation _activityRequestedOrientation = ScreenOrientation.Unspecified;

        public static void SetActivityOrientation(int screenWidth, int screenHeigth)
        {
            _activityRequestedOrientation = screenWidth > screenHeigth ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
        }

        public static ScreenOrientation GetActivityOrientation()
        {
            return _activityRequestedOrientation;
        }

        public const string WE_BIN_NAME = "WE_BIN_NAME";
        public const string WE_BIN_ID = "WE_BIN_ID";

        private static IdAndNameDto _geBin = new IdAndNameDto();
        public static IdAndNameDto WeBin
        {
            get
            {
                if (string.IsNullOrEmpty(_geBin.Name))
                {

                    _geBin.Name = GetStringFromPreferances(Application.Context, WE_BIN_NAME);
                    string idStr = GetStringFromPreferances(Application.Context, WE_BIN_ID);
                    if (!string.IsNullOrEmpty(_geBin.Name))
                    {
                        _geBin.Id = 0;
                        long retVal = 0;
                        long.TryParse(idStr, out retVal);
                        _geBin.Id = retVal;
                    }
                }

                return _geBin;
            }

            set
            {
                PutStringIntoPreferances(Application.Context, WE_BIN_NAME, value.Name);
                PutStringIntoPreferances(Application.Context, WE_BIN_ID, value.Id.ToString());
            }
        }

        public static void MessageShow(Context context, int info, int message)
        {
            var messageDialog = new AlertDialog.Builder(context);
            messageDialog.SetTitle(info);
            messageDialog.SetMessage(message);
            messageDialog.SetNeutralButton("OK", delegate { });
            messageDialog.Create();
            messageDialog.Show();
        }

        public static void MessageShow(Context context, string info, string message)
        {
            var messageDialog = new AlertDialog.Builder(context);
            messageDialog.SetTitle(info);
            messageDialog.SetMessage(message);
            messageDialog.SetNeutralButton("OK", delegate { });
            messageDialog.Create();
            messageDialog.Show();
        }

        public static void ShowErrorMessage(Activity activity, int messageID)
        {
            ShowErrorMessage(activity, activity.Resources.GetString(messageID));
        }

        public static void ShowErrorMessage(Context context, string message)
        {
            PlayErrorSound(context);
            MessageShow(context, context.Resources.GetString(Resource.String.msgTitleError), message);
        }

        public static void ShowWarningMessage(Context context, int messageID)
        {
            ShowWarningMessage(context, context.Resources.GetString(messageID));
        }

        public static void ShowWarningMessage(Context context, string message)
        {
            MessageShow(context, context.Resources.GetString(Resource.String.msgTitleWarning), message);
        }

        public static void ShowInfoMessage(Context context, int messageID)
        {
            ShowInfoMessage(context, context.Resources.GetString(messageID));
        }

        public static void ShowInfoMessage(Context context, string message)
        {
            MessageShow(context, context.Resources.GetString(Resource.String.msgTitleInfo), message);
        }

        public static void ShowDuplicatedItems(Activity activity, string barcode, DataTable tblItem)
        {
            Intent intent = new Intent(activity, typeof(ItemListActivity));
            intent.PutExtra("paramBarcode", barcode);
            StringWriter writer = new StringWriter();
            tblItem.WriteXml(writer, true);

            intent.PutExtra("dt", writer.ToString());
            activity.StartActivityForResult(intent, 0);
        }

        public static void HideKeyboard(View editTxt, Context context)
        {
            InputMethodManager imm = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(editTxt.WindowToken, HideSoftInputFlags.None);
        }

        public static void ShowKeyboard(EditText editTxt, Context context, InputTypes keyboardType, bool deviceDepend = false)
        {
            if (deviceDepend)
            {
                var metrics = context.Resources.DisplayMetrics;
                int smaller = metrics.WidthPixels < metrics.HeightPixels ? metrics.WidthPixels : metrics.HeightPixels;
                int bigger = metrics.WidthPixels > metrics.HeightPixels ? metrics.WidthPixels : metrics.HeightPixels;
                if (480 == smaller && 576 == bigger) return;
            }

            editTxt.InputType = keyboardType;
            InputMethodManager imm = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
            imm.ShowSoftInput(editTxt, ShowFlags.Forced);
        }

        public static bool IsEnterKeyPressed(string scannedText, View.KeyEventArgs e, ref string returnedBarcode, bool returnOnEmpty = true)
        {
            if (string.Empty == scannedText && returnOnEmpty) return false;

            returnedBarcode = Regex.Replace(scannedText, @"\t|\n|\r", string.Empty);

            if (Keycode.Unknown == e.KeyCode && 441 == e.Event.ScanCode || e.KeyCode == Keycode.Enter)
            {
                e.Handled = e.KeyCode == Keycode.Enter; //true;
                return e.Event.Action == KeyEventActions.Up;
            }

            return false;
        }

        public static string FormatDecimalStringToScreenValue(string decimalText)
        {
            string retValue = decimalText;
            if (decimal.TryParse(decimalText, out var dec))
            {
                if (16 < dec.ToString().Length)
                {
                    if (decimalText.Contains("."))
                    {
                        retValue = decimalText.Split(new char[] { '.' })[0];
                    }
                    if (decimalText.Contains(","))
                    {
                        retValue = decimalText.Split(new char[] { ',' })[0];
                    }
                }
                else
                {
                    retValue = dec.ToString("0.###");
                }
            }
            return retValue;
        }

        public static bool IsQuantityScanned(string qtyString)
        {
            return !string.IsNullOrEmpty(qtyString) &&
                   (5 > qtyString.Length || 5 == qtyString.Length && qtyString.StartsWith("-")) &&
                   decimal.TryParse(qtyString, out _);
        }

        public static decimal DecimalFromString(string value)
        {
            if (decimal.TryParse(value, out var retDecimal))
            {
                if (value.Contains(".") && !retDecimal.ToString().Contains("."))
                {
                    decimal.TryParse(value.Replace('.', ','), out retDecimal);
                }

                if (value.Contains(",") && !retDecimal.ToString().Contains(","))
                {
                    decimal.TryParse(value.Replace(',', '.'), out retDecimal);
                }
            }

            return retDecimal;
        }

        public static void PlaySuccessSound(Context ctx)
        {
            PlaySound(ctx, Resource.Raw.beep);
        }

        public static void PlayErrorSound(Context ctx)
        {
            PlaySound(ctx, Resource.Raw.errorBeep);
        }

        private static void PlaySound(Context ctx, int resourceId)
        {
            MediaPlayer player = MediaPlayer.Create(ctx, resourceId);
            player.Start();
            player.Dispose();
        }

        public static bool DateFromMMYY(string mmyy)
        {
            if (4 != mmyy.Length) return false;

            if (!int.TryParse(mmyy, out var checkInt)) return false;

            checkInt = int.Parse(mmyy.Substring(0, 2));

            if (1 > checkInt || 12 < checkInt) return false;

            checkInt = 2000 + int.Parse(mmyy.Substring(2));

            if (DateTime.Now.Year > checkInt) return false;

            return true;
        }

        public static string GetAppVersion()
        {
            var info = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(
                Application.Context.ApplicationContext.PackageName, 0);

            return info.VersionName;
        }

        public static bool CheckPALCurrentBin(string palNo, string repRoutePlaceRowID, out string palCurrentBin, string scannedTargetBin)
        {
            palCurrentBin = string.Empty;

            if (palNo != string.Empty)
            {
                //TODO: service functions
                palCurrentBin = "";//WsHueckmann.CheckPALCurrentBin(palNo);
                //TODO: service functions
                string sourceBin = "";// WsHueckmann.ReplGetBinFromRouteID((int.Parse(repRoutePlaceRowID) - 1).ToString());
                string targetBin = "";// WsHueckmann.ReplGetBinFromRouteID(repRoutePlaceRowID);
                if (targetBin == string.Empty || scannedTargetBin == palCurrentBin)
                {
                    targetBin = scannedTargetBin;
                }

                if (palCurrentBin == sourceBin || palCurrentBin == targetBin || palCurrentBin == string.Empty)
                {
                    //TODO: service functions
                    return false; // WsHueckmann.ReplUpdateBinCodeAndTuInBin(repRoutePlaceRowID, targetBin, palNo);
                }
            }

            return false;
        }

        public static bool CheckPALCurrentBin(string palNo, out string palCurrentBin, string scannedTargetBin)
        {
            palCurrentBin = string.Empty;

            if (palNo != string.Empty)
            {
                //TODO: service functions
                palCurrentBin = "";// WsHueckmann.CheckPALCurrentBin(palNo);

                if (palCurrentBin == scannedTargetBin || palCurrentBin == string.Empty)
                {
                    return true;
                }
            }

            return false;
        }

        public static Task<bool> ShowConfirmationDialog(Context context, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            new AlertDialog.Builder(context)
                .SetMessage(message)
                .SetPositiveButton(context.GetString(Resource.String.Yes), delegate { tcs.SetResult(true); })
                .SetNegativeButton(context.GetString(Resource.String.Cancel), delegate { tcs.SetResult(false); })
                .Show();

            return tcs.Task;
        }

        public static Task<bool> ShowYesNoConfirmationDialog(Context context, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            new AlertDialog.Builder(context)
                .SetMessage(message)
                .SetPositiveButton(context.GetString(Resource.String.Yes), delegate { tcs.SetResult(true); })
                .SetNegativeButton(context.GetString(Resource.String.No), delegate { tcs.SetResult(false); })
                .Show();

            return tcs.Task;
        }

        private static JsonSerializerSettings GetDeserializeSettings()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return settings;
        }

        public static string GetDateTimeStringFromJavaLocalDateTimeFormat(string javaFormat)
        {
            DateTimeDto dateTimeObj = JsonConvert.DeserializeObject<DateTimeDto>(javaFormat, GetDeserializeSettings());
            return string.Format("{0}.{1}.{2} {3}:{4}:{5}", dateTimeObj.Date.Day, dateTimeObj.Date.Month, dateTimeObj.Date.Year, dateTimeObj.Time.Hour, dateTimeObj.Time.Minute, dateTimeObj.Time.Second);
        }
        public static string GetDateStringFromJavaLocalDateTimeFormat(string javaFormat)
        {
            DateDto dateTimeObj = JsonConvert.DeserializeObject<DateDto>(javaFormat, GetDeserializeSettings());
            return string.Format("{0}.{1}.{2}", dateTimeObj.Day, dateTimeObj.Month, dateTimeObj.Year);
        }
        public static string GetDateTimeStringFromJavaLocalDateTimeFormatDto(DateTimeDto datetime)
        {
            return string.Format("{0}.{1}.{2} {3}:{4}:{5}", datetime.Date.Day, datetime.Date.Month, datetime.Date.Year, datetime.Time.Hour, datetime.Time.Minute, datetime.Time.Second);
        }
        public static string GetDateStringFromJavaLocalDateTimeFormatDto(DateDto datetime)
        {
            return string.Format("{0}.{1}.{2}", datetime.Day, datetime.Month, datetime.Year);
        }

        public static bool CompareLastScannedDigits(string target, string scan, int lastDigitsCount)
        {
            if (scan.Length < lastDigitsCount)
            {
                while (scan.Length < lastDigitsCount)
                {
                    scan = "0" + scan;
                }
            }
            else if (scan.Length > lastDigitsCount)
            {
                return false;
            }

            if (!int.TryParse(scan, out _))
            {
                return false;
            }

            if (scan != target.Substring(target.Length - lastDigitsCount))
            {
                return false;
            }

            return true;
        }

        public static void PutStringIntoPreferances(Context context, string key, string value)
        {
            var prefs = context.GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private);
            var editor = prefs.Edit();
            editor.PutString(key, value);
            editor.Commit();
        }
        public static string GetStringFromPreferances(Context context, string key)
        {
            var prefs = context.GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private);
            if (prefs.Contains(key))
            {
                return prefs.GetString(key, string.Empty);
            }
            return null;
        }


        public static Dictionary<string, string> JsonToDictionary(string jsonText)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText, GetDeserializeSettings());
            }
            catch (Exception ex)
            {
                Log.Write("JsonToDictionary", "Exception:" + ex.Message);
            }
            return null; 
        }

        public static decimal GetWeightDifferencePercent()
        {
            decimal ret = 0;
            decimal.TryParse(WSWmsHelper.LoadConfiguration(CONF_WEIGHT_DIFFERENCE_PERCENT_SECTION, CONF_WEIGHT_DIFFERENCE_PERCENT)
                , out ret);
            return ret;
        }
    }

    internal static class CardActionTypes
    {
        public const string ITEM = "Item";
        public const string TU = "TU";
        public const string PAL = "PAL";
        public const string WAGON = "Wagon";
        public const string BIN = "Bin";
    }

    internal static class WebSrvReturnStatus
    {
        public const string OK = "OK";
        public const string ERROR = "ERROR:";
        public const string USER = "USER:";
        public const string DONE = "DONE";
        public const string WS_ERROR = "WS_ERROR";
        public const string ERROR_LOWER = "Error:";
    }

    internal static class InvBinStatus
    {
        //0-New; 1-In Process; 2- Counted; 3-Not counted;4-Empty
        public const int NEW = 0;
        public const int IN_PROCESS = 1;
        public const int COUNTED = 2;
        public const int UNCOUNTED = 3;
        public const int EMPTY = 4;
    }

    internal static class InvLogStatus
    {
        //0-New; 1-In Process; 2- Finished;
        public const int NEW = 0;
        public const int IN_PROCESS = 1;
        public const int FINISHED = 2;
        public const int UNCOUNTED = 3;
        public const int UNCOUNTED_FINISHED = 4;
        public const int SECOND_COUNT_IN_PROCESS = 5;
        public const int SECOND_COUNT_FINISHED = 6;
    }


    internal static class InvLeStatus
    {
        public const string OPEN = "OPEN";
        public const string IN_PROCESS = "IN_PROCESS";
    }

    internal static class SpecialEquipmentOptions
    {
        //wagons
        public const string WAGON = "BW";

        //pallets
        public const string EUROPALLET = "EP";
        public const string PALLET = "PL";
        public const string GRAY_BOX = "GB";
        public const string STAKES_PALLET = "RP";

        //other
        public const string PLASTIC_BOX = "PB";
        public const string PLASTIC_BOX_KLEIN = "PBK";
        public const string TABLAR = "TAB";
        public const string UMKARTON = "UMK";
        public const string VERSANDKARTON = "VK";
        public const string LB = "LB";

        public static bool IsWagon(string specialEquipmnet)
        {
            return specialEquipmnet.Equals(WAGON);
        }

        public static bool IsPallet(string specialEquipmnet)
        {
            return specialEquipmnet.Equals(EUROPALLET)
                || specialEquipmnet.Equals(PALLET)
                || specialEquipmnet.Equals(GRAY_BOX)
                || specialEquipmnet.Equals(STAKES_PALLET);
        }
    }

    internal static class UnitOfMeasureCodes
    {
        public const string STUEK = "STK";
        public const string BLISTER = "BL";
        public const string KARTON = "KT";
        public const string UMKARTON = "UMK";
        public const string PALLET = "PAL";
    }
}