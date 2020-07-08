using Android.App;
using Android.Provider;
using System;
using System.IO;

namespace mstore_WMS.Utils
{
    public static class Log
    {
        private static readonly string Filename = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/mstorewms.log";
        private static readonly string AndroidId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);

        private const string deviceSeparator = "#DEVID#";
        private const string LogWriteErrorText = "Log Write Error: ";
        private const string NewLine = "\n";
        private const int DebugLevel = 3;

        public enum DebugLevels
        {
            FATAL,
            ERROR,
            WARN,
            INFO
        }

        public static void Write(object formOrString, string message, DebugLevels level = DebugLevels.FATAL)
        {
            try
            {
                if (DebugLevel < (int)level)
                {
                    return;
                }

                string formName;
                if (formOrString is string)
                {
                    formName = formOrString.ToString();
                }
                else
                {
                    formName = formOrString.GetType().Name;
                }

                if (string.Empty != formName)
                {
                    formName += ";";
                }

                long maxMemory = Java.Lang.Runtime.GetRuntime().MaxMemory();
                long freeMemory = Java.Lang.Runtime.GetRuntime().FreeMemory();
                long totalMemory = Java.Lang.Runtime.GetRuntime().TotalMemory();

                string userName = string.Empty != Activities.LoginActivity.User ? Activities.LoginActivity.User : "__NO_USER__";

                string userTimeLevel = userName + ";" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + ";" + formName + level + ";";
                string memoryInfo = " Memory:Max=" + maxMemory + " Free=" + freeMemory + " Total=" + totalMemory + NewLine;

                string lineWS = userTimeLevel + message + memoryInfo;

                string retWSstatus = WriteToWS(AndroidId, lineWS);

                if (Utility.RET_STATUS_OK != retWSstatus)
                {
                    File.AppendAllText(Filename, lineWS);
                    File.AppendAllText(Filename, userTimeLevel + LogWriteErrorText + retWSstatus + NewLine);
                }
                else if (File.Exists(Filename))
                {
                    string[] lines = File.ReadAllLines(Filename);
                    File.Delete(Filename);
                    foreach (string line in lines)
                    {
                        string devId = AndroidId;
                        string msg = line;

                        retWSstatus = WriteToWS(devId, msg);
                        if (Utility.RET_STATUS_OK != retWSstatus)
                        {
                            File.AppendAllText(Filename, line);
                            File.AppendAllText(Filename, userTimeLevel + LogWriteErrorText + retWSstatus + NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        static string WriteToWS(string deviceId, string message)
        {
            string retString = string.Empty;
            try
            {
                WSWmsHelper.WriteDeviceLog(deviceId, message);
            }
            catch (Exception ex)
            {
                retString = ex.Message;
            }

            return retString;
        }
    }
}