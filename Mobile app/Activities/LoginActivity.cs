using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Text;
using Android.Views;
using Android.Widget;
using mstore_WMS.Adapters;
using mstore_WMS.AppCode;
using mstore_WMS.Models.Dto;
using mstore_WMS.Models.Enums;
using mstore_WMS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace mstore_WMS.Activities
{
    [Activity(Label = "Hueckmann Login", Icon = "@drawable/help_desk")]
    public class LoginActivity : Activity
    {
        private const string RET_SUCCESS = "Success";
        private const string WmsMstoreUserName = "wmsMstoreUserName";
        private const string WMSMSTORE_WS_URL_SERVER = "wmsMstoreWSURLServer";
        private const string WMSMSTORE_WS_URL_PORT = "wmsMstoreWSURLPort";
        private bool loadUsers;

        private TextView tvUserLbl;
        private Spinner spinnerLoginName;
        private EditText txtPassword;
        private Button btnLogin;
        private TextView appVersion;
        private TextView tvPasswordLbl;
        private TextView tvPortLbl;
        private EditText editTextPort;
        private Button btnSettings;
        private Button btnRefresh;

        TextView tvWSS;
         
        private static string _userName;

        private const string lblWSS = "WSS:";
        public static string User
        {
            get
            {
                if (string.IsNullOrEmpty(_userName))
                {
                    _userName = LoadUser();
                }

                return _userName;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Title = GetString(Resource.String.loginFormLabelText);
            SetContentView(Resource.Layout.Login);

            btnLogin = FindViewById<Button>(Resource.Id.buttonLogin);
            btnLogin.Click += ButtonLogin_Click;

            btnSettings = FindViewById<Button>(Resource.Id.buttonSettings);
            btnSettings.Click += BtnSettings_Click;

            btnRefresh = FindViewById<Button>(Resource.Id.buttonRefresh);
            btnRefresh.Click += BtnRefresh_Click;

            tvPasswordLbl = FindViewById<TextView>(Resource.Id.tvPasswordLbl);

            txtPassword = FindViewById<EditText>(Resource.Id.editTextPassword);
            txtPassword.InputType = InputTypes.ClassNumber | InputTypes.NumberVariationPassword;

            tvUserLbl = FindViewById<TextView>(Resource.Id.tvUserLbl);
            spinnerLoginName = FindViewById<Spinner>(Resource.Id.spinnerUserNames);

            editTextPort = FindViewById<EditText>(Resource.Id.editTextPort);
            tvPortLbl = FindViewById<TextView>(Resource.Id.tvPortLbl);

            appVersion = FindViewById<TextView>(Resource.Id.appVersion);
            appVersion.Text = $"Ver. {Utility.GetAppVersion()}";

            tvWSS = FindViewById<TextView>(Resource.Id.tvWsServer);

            Set_WS_Url();

        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            var prefs = GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private);

            if (prefs.Contains(WMSMSTORE_WS_URL_SERVER))
            {
                string wsUrlServer = prefs.GetString(WMSMSTORE_WS_URL_SERVER, string.Empty);
                string[] parsedFirst = wsUrlServer.Split("//");
                if (2 > parsedFirst.Length) return;
                string[] parsedSecond = parsedFirst[1].Split('/');
                string urlToOpen = parsedFirst[0] + "//" + parsedSecond[0] + "/android/LVSS_Mobile.apk";
                Android.Net.Uri uri = Android.Net.Uri.Parse(urlToOpen);
                Intent intent = new Intent(Intent.ActionView);
                intent.SetData(uri);
                this.StartActivity(intent);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            if (GetString(Resource.String.btnSettings_txt) == btnSettings.Text)
            {
                ShowSettingsPasswordDialog();
            }
            else
            {
                InitLogin();
            }
        }

        private void SaveUser()
        {
            GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private)
                .Edit()
                .PutString(WmsMstoreUserName, User)
                .Commit();
        }

        public static string LoadUser()
        {
            return Application.Context.GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private)
                .GetString(WmsMstoreUserName, string.Empty);
        }

        private bool CheckWS(string wsUrl)
        {
            try
            {
                int tryCount = 5;
                string result = "";

                while (!result.Equals(WSWmsHelper.WS_RETURN_OK) && 0 < tryCount)
                {
                    result = WSWmsHelper.PingWS();
                    tryCount--;
                }

                if (!result.Equals(WSWmsHelper.WS_RETURN_OK))
                {
                    ShowSetupWebConnectionMessage();
                    return false;
                }

                if (null != tvWSS)
                {
                    tvWSS.Text = lblWSS + wsUrl;
                }

                return true;
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.NoConnection) + "\n" + ex.Message);
                return false;
            }
        }

        private void ShowSetupWebConnectionMessage()
        {
            Utility.ShowWarningMessage(this, Resource.String.msgSetupWebConnection);
        }

        private void Save_WS_Url(string urlServer, string urlPort)
        {
            GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private)
                .Edit()
                .PutString(WMSMSTORE_WS_URL_SERVER, urlServer)
                .PutString(WMSMSTORE_WS_URL_PORT, urlPort)
                .Commit();
        }

        private void Set_WS_Url()
        {
            var prefs = GetSharedPreferences("RunningAssistant.preferences", FileCreationMode.Private);
            if (prefs.Contains(WMSMSTORE_WS_URL_SERVER) && prefs.Contains(WMSMSTORE_WS_URL_PORT))
            {
                string wsUrlServer = prefs.GetString(WMSMSTORE_WS_URL_SERVER, string.Empty);
                string wsUrlPort = prefs.GetString(WMSMSTORE_WS_URL_PORT, string.Empty);
                Utility.BuildWsUrlFromServerIpAndPort(wsUrlServer);

                if (CheckWS(Utility.BuildWsUrlFromServerIpAndPort(wsUrlServer)))
                {
                    loadUsers = true;
                    InitLogin();
                    return;
                }

                loadUsers = false;
                InitLogin();
            }
            else
            {
                ShowSetupWebConnectionMessage();
                loadUsers = false;
                InitLogin();
            }
        }

        private void InitUrlSet(string port)
        {
            editTextPort.Visibility = tvPortLbl.Visibility = ViewStates.Visible;
            tvUserLbl.Visibility = ViewStates.Invisible;
            btnLogin.Text = GetString(Resource.String.btnTitleSave);
            tvPasswordLbl.Text = GetString(Resource.String.lblWS_Url_Server);

            txtPassword.Text = tvWSS.Text.Replace(lblWSS, "");
            txtPassword.InputType = InputTypes.ClassText;

            editTextPort.Text = port;
            btnSettings.SetText(Resource.String.btnBack);

            if (GetString(Resource.String.btnTitleSave) == btnLogin.Text)
            {
                spinnerLoginName.Visibility = ViewStates.Gone;
            }
            else
            {
                spinnerLoginName.Visibility = ViewStates.Visible;
            }


        }

        private void InitLogin()
        {
            editTextPort.Visibility = tvPortLbl.Visibility = ViewStates.Gone;
            spinnerLoginName.Visibility = tvUserLbl.Visibility = ViewStates.Visible;

            btnLogin.Text = GetString(Resource.String.btnLogin_txt);
            tvPasswordLbl.Text = GetString(Resource.String.lblPassword_txt);
            btnSettings.SetText(Resource.String.btnSettings_txt);

            txtPassword.InputType = InputTypes.ClassText | InputTypes.TextVariationPassword;
            txtPassword.Text = string.Empty;

            if (!loadUsers || null == spinnerLoginName)
            {
                btnRefresh.Enabled = false;
                return;
            }

            btnRefresh.Enabled = true;

            try
            {
                List<UserDto> usersList = WSWmsHelper.GetAllUsers();
                if (null == usersList) return;

                if (0 < usersList.Count)
                {
                    string savedLogin = LoadUser();

                    int idx = 0;
                    int selectedUserIdx = -1;
                    string[] spinnerItems = new string[usersList.Count];
                    foreach (UserDto udr in usersList)
                    {
                        if (string.Empty != savedLogin && udr.Login == savedLogin)
                        {
                            selectedUserIdx = idx;
                        }
                        spinnerItems[idx++] = udr.Login;
                    }

                    CustomArrayAdapter adapter = new CustomArrayAdapter(this, spinnerItems.ToList());
                    spinnerLoginName.Adapter = adapter;

                    if (-1 != selectedUserIdx)
                    {
                        spinnerLoginName.SetSelection(selectedUserIdx);
                    }
                }
            }
            catch (WebException wex)
            {
                string msgWSnotAvailable = GetString(Resource.String.msgWSnotAvailable);
                new AlertDialog.Builder(this).SetTitle(msgWSnotAvailable)
                    .SetMessage(GetString(Resource.String.msgActivateInetWSandTryAgain))
                    .SetPositiveButton(GetString(Resource.String.Ok), (s, e) => { Recreate(); })
                    .Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        private void ButtonLogin_Click(object sender, EventArgs e)
        {
            bool isSettingsMode = false;

            if (!GetString(Resource.String.btnTitleSave).Equals(btnLogin.Text))  //try login
            {
                if (spinnerLoginName.SelectedItem != null && !string.IsNullOrEmpty(spinnerLoginName.SelectedItem.ToString()))
                {
                    UserAuthentication.SetUserAndPassword(spinnerLoginName.SelectedItem.ToString(), txtPassword.Text);
                    string result = WSWmsHelper.AuthenticateUser(this);
                    if (result != WSWmsHelper.WS_RETURN_OK)
                    {
                        UserAuthentication.SetUserAndPassword("", "");
                        Utility.ShowErrorMessage(this, result);
                        return;
                    }
                }
                else
                {
                    Utility.ShowErrorMessage(this, Resource.String.msgUserOrPasswordError);
                    return;
                }
            }
            else //check WS connection and save settitngs
            {
                isSettingsMode = true;
                if (string.Empty != txtPassword.Text && string.Empty != editTextPort.Text) //save settings
                {
                    if (CheckWS(Utility.BuildWsUrlFromServerIpAndPort(txtPassword.Text)))
                    {
                        Save_WS_Url(txtPassword.Text, editTextPort.Text);
                        loadUsers = true;
                        editTextPort.Visibility = ViewStates.Visible;
                        tvPortLbl.Visibility = ViewStates.Visible;
                        InitLogin();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            if (isSettingsMode) return;

            EnableUserMenus(spinnerLoginName.SelectedItem.ToString());

            base.OnBackPressed();
            _userName = spinnerLoginName.SelectedItem.ToString();

            SaveUser();

            Log.Write(this, "User logged in.", Log.DebugLevels.INFO);

            Utility.SetActivityOrientation(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);
        }

        private void EnableUserMenus(string userName)
        {
            try
            {
                foreach (int btnId in MainActivity.ButtonIds)
                {
                    MainActivity.ButtonEnable[btnId] = MainActivity.BUTTON_HIDE_ID;
                }

                List<string> menuOptions = WSWmsHelper.GetUserMenuOptions(userName);
                if (null == menuOptions) return;

                int nextIdx = 0;
                foreach (string option in menuOptions)
                {
                    int nextButtonId = MainActivity.ButtonIds[nextIdx++];
                    if (option.Equals(MenuOptions.PUT_AWAY.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonWEEinlagerung;
                    }
                    else if (option.Equals(MenuOptions.GOODS_RECEIPT.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonGoodsReceipt;
                    }
                    else if (option.Equals(MenuOptions.INVENTORY.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonInventory;
                    }
                    else if (option.Equals(MenuOptions.PICKING.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonPick;
                    }
                    else if (option.Equals(MenuOptions.REALLOCATION.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonReallocation;
                    }
                    else if (option.Equals(MenuOptions.TRANSPORT.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonTransport;
                    }
                    else if (option.Equals(MenuOptions.STORE_PRODUCTION.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonStoreProduction;
                    }
                    else if (option.Equals(MenuOptions.PRINT_SHIPPING_LABELS.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonPrintLabels;
                    }
                    else if (option.Equals(MenuOptions.INITIAL_STOCKTAKING.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonInitialStocktaking;
                    }
                    else if (option.Equals(MenuOptions.INFO.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonInfo;
                    }
                    else if (option.Equals(MenuOptions.WE_HANDELSWARE.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonWeHandelsware;
                    }
                    else if (option.Equals(MenuOptions.SPLIT_LE.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonSplitTU;
                    }
                    else if (option.Equals(MenuOptions.REPACK.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonRepack;
                    }
                    else if (option.Equals(MenuOptions.DELETE_LE.ToString()))
                    {
                        MainActivity.ButtonEnable[nextButtonId] = Resource.Id.buttonDeleteTU;
                    }
                }
            }
            catch (Exception ex)
            {
                new AlertDialog.Builder(this)
                    .SetTitle(Resource.String.msgWSnotAvailable)
                    .SetMessage(Resource.String.msgActivateInetWSandTryAgain)
                    .SetPositiveButton(Resource.String.Ok, delegate { Recreate(); })
                    .Show();
            }
        }

        private void ShowSettingsPasswordDialog()
        {
            EditText userInput = new EditText(this)
            {
                InputType = InputTypes.ClassText | InputTypes.TextVariationPassword
            };

            new AlertDialog.Builder(this)
                .SetTitle(Resource.String.AccessPw)
                .SetView(userInput)
                .SetPositiveButton(Resource.String.Ok, delegate
                {
                    if (userInput.Text == "123456")
                    {
                        InitUrlSet(Utility.WS_DEFAULT_PORT);
                        editTextPort.Visibility = ViewStates.Invisible;
                        tvPortLbl.Visibility = ViewStates.Invisible;
                    }
                    else
                    {
                        Utility.ShowErrorMessage(this, GetString(Resource.String.WrongPw));
                    }
                })
                .SetNegativeButton(Resource.String.Cancel, delegate { })
                .Show();
        }

        public override void OnBackPressed()
        {
            FragmentManager.PopBackStack();
        }
    }
}