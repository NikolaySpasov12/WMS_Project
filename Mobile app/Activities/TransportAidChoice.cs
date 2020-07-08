using System;
using System.Collections.Generic;
using System.Data;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Text;
using Android.Graphics;

using mstore_WMS.Utils;
using mstore_WMS.Models;

namespace mstore_WMS.Activities
{
    [Activity(Label = "TransportAndChoice")]
    public class TransportAidChoice : Activity
    {
        public const string EXTRA_IS_TRANSPORT = "IS_TRANSPORT";

        private const string TransportAidsNummerKey = "TransportAidsNummerKey";
        private const string TransportAidsNummer = "TransportAidsNummer";

        private Spinner spinner;
        private EditText hintTextView;
        private EditText scanEditText;
        private Button transportCount;
        private Button loadingCount;
        private ImageButton kbButton;
        private LinearLayout thisLayout;

        private DataTable dt;
        private DataTable counts;

        private InputTypes keyboardType = InputTypes.Null;

        private string scan = string.Empty;
        List<string> transportAidNummers = new List<string>();
        int transportAidID;
        int spinnerIndex;
        private int places = 1;

        private bool isTansport = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.TransportAidChoice);
            GetWidgetIDs();

            if (Intent.HasExtra(EXTRA_IS_TRANSPORT))
            {
                isTansport = Intent.GetBooleanExtra(EXTRA_IS_TRANSPORT, true);
                if (!isTansport)
                {
                    thisLayout.SetBackgroundColor(Color.ParseColor("#00FFFF"));
                }
            }
            loadingCount.Click += ShipmentListButton_Click;
            transportCount.Click += ForwardButton_Click;
            scanEditText.KeyPress += ScanEditText_KeyPress;
            spinner.ItemSelected += Spinner_ItemSelected;
            kbButton.Click += KbButton_Click;

            SetTitle(Resource.String.labelTransportAidChoice);
        }

        private void KbButton_Click(object sender, EventArgs e)
        {
            Utility.ShowKeyboard(scanEditText, this, keyboardType);
        }

        protected override void OnResume()
        {
            Load();
            base.OnResume();
        }

        private void ShipmentListButton_Click(object sender, EventArgs e)
        {
            if (transportAidID != 0)
            {
                if (isTansport)
                {
                    PackBundleForShipmentList();
                }
                else
                {
                    PackBundleForProdPallet();
                }

            }
            else
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.nothingScanned));
            }
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            spinnerIndex = spinner.SelectedItemPosition;
            WriteLastChosenNummer(spinner.SelectedItem.ToString());
            transportAidID = GetTransportIdByNummer(spinner.GetItemAtPosition(spinnerIndex).ToString());
            try
            {
                counts = WSWmsHelper.GetTransportLoadingCount(transportAidID);
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, ex.Message);
                return;
            }
            if (int.Parse(counts.Rows[0][FieldNames.TransportCount].ToString()) > 0)
            {
                transportCount.Text = GetString(Resource.String.transportCountIs) + counts.Rows[0][FieldNames.TransportCount].ToString();
                transportCount.Visibility = ViewStates.Visible;
                transportCount.Enabled = true;
            }
            else
                transportCount.Visibility = ViewStates.Invisible;

            if (isTansport)
            {
                if (int.Parse(counts.Rows[0][FieldNames.LoadingCount].ToString()) > 0)
                {
                    loadingCount.Text = GetString(Resource.String.loadingCountIs) + counts.Rows[0][FieldNames.LoadingCount].ToString();
                    loadingCount.Visibility = ViewStates.Visible;
                }
                else
                    loadingCount.Visibility = ViewStates.Invisible;
            }
            else
            {
                transportCount.Enabled = false;
                loadingCount.Visibility = ViewStates.Visible;
                loadingCount.Text = GetString(Resource.String.btnNext_txt);
            }
        }

        private void ScanEditText_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            scan = string.Empty;

            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                e.Handled = true;
                if (scanEditText.Text != string.Empty)
                {
                    scan = scanEditText.Text.ToUpper();
                    if (TransportmittelExists(scan))
                    {
                        spinner.SetSelection(spinnerIndex);
                        hintTextView.Text = GetString(Resource.String.pressForward);
                        scanEditText.Text = "";
                        scanEditText.RequestFocus();
                        Forward();
                    }
                    else
                    {
                        scanEditText.Text = "";
                        scanEditText.RequestFocus();
                    }
                }
                else
                {
                    Utility.ShowErrorMessage(this, GetString(Resource.String.nothingScanned));
                }
            }
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            Forward();
        }

        private void Forward()
        {
            if (transportAidID != 0)
            {
                if (isTansport)
                {
                    PackBundleForTOL();
                }
                else
                {
                    PackBundleForProdPallet();
                }
            }
            else
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.nothingScanned));
            }
        }

        private void WriteLastChosenNummer(string nummer)
        {
            var prefs = GetSharedPreferences(TransportAidsNummer, FileCreationMode.Private);
            var editor = prefs.Edit();
            editor.PutString(TransportAidsNummerKey, nummer);
            editor.Apply();
        }

        private string GetLastChosenNummer()
        {
            var prefs = GetSharedPreferences(TransportAidsNummer, FileCreationMode.Private);

            if (prefs.Contains(TransportAidsNummerKey))
            {
                return prefs.GetString(TransportAidsNummerKey, string.Empty);
            }

            return string.Empty;
        }

        private bool TransportmittelExists(string nummer)
        {
            foreach (var transportmittel in transportAidNummers)
            {
                if (nummer == transportmittel)
                {
                    transportAidID = GetTransportIdByNummer(scan);
                    return true;
                }  
            }
            Utility.ShowErrorMessage(this, GetString(Resource.String.invalidTransportMittel));
            return false;
        }

        private int GetTransportIdByNummer(string nummer)
        {
            int counter = 0;
            foreach(DataRow dr in dt.Rows)
            {
                counter++;
                if(dr[1].ToString() == nummer)
                {
                    spinnerIndex = counter - 1;
                    places = int.Parse(dr[FieldNames.Places].ToString());
                    return transportAidID = int.Parse(dr[0].ToString());                    
                }
            }
            return 0;
        }

        private void Load()
        {
            hintTextView.Text = GetString(Resource.String.transportmittelScanOrChoose);
            try
            {
                dt = WSWmsHelper.LoadTransportAids();
                transportAidNummers.Clear();

                transportAidID = int.Parse(dt.Rows[0][0].ToString());

                foreach(DataRow i in dt.Rows)
                {
                    transportAidNummers.Add(i[1].ToString());
                }

                ArrayAdapter adapter = new ArrayAdapter(this, Resource.Layout.SpinnerItem2, transportAidNummers);

                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                spinner.Adapter = adapter;

                string lastChosenNum = GetLastChosenNummer();
                GetTransportIdByNummer(lastChosenNum);
                if (lastChosenNum != string.Empty)
                {
                    spinner.SetSelection(spinnerIndex);
                }
            }
            catch(Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
            }
        }

        private void PackBundleForTOL()
        {
            Intent i = new Intent(this, typeof(TransportOrderList));
            Bundle b = new Bundle();
            b.PutString("transportAidNummer", spinner.SelectedItem.ToString());
            b.PutInt("transportAidID", transportAidID);
            b.PutString("from", "Transport");
            i.PutExtra("b", b);
            b.PutInt("places", places);
            StartActivity(i);
        }

        private void PackBundleForShipmentList()
        {
            Intent i = new Intent(this, typeof(ShipmentOrderList));
            Bundle b = new Bundle();
            b.PutString("transportAidNummer", spinner.SelectedItem.ToString());
            b.PutInt("transportAidID", transportAidID);
            b.PutInt("places", places);
            i.PutExtra("b", b);
            StartActivity(i);
        }

        private void PackBundleForProdPallet()
        {
            Intent i = new Intent(this, typeof(ProdPalletActivity));
            i.SetFlags(ActivityFlags.NewTask);
            Bundle b = new Bundle();
            b.PutString("transportAidNummer", spinner.SelectedItem.ToString());
            b.PutInt("transportAidID", transportAidID);
            b.PutString("from", "Transport");
            b.PutInt("places", places);
            i.PutExtra("b", b);
            StartActivity(i);
        }


        private void GetWidgetIDs()
        {
            spinner = FindViewById<Spinner>(Resource.Id.transportSpinner);
            hintTextView = FindViewById<EditText>(Resource.Id.tvHint);
            scanEditText = FindViewById<EditText>(Resource.Id.editTextScan);
            transportCount = FindViewById<Button>(Resource.Id.transportCount);
            loadingCount = FindViewById<Button>(Resource.Id.loadingCount);
            kbButton = FindViewById<ImageButton>(Resource.Id.btnKeyboard);
            thisLayout = FindViewById<LinearLayout>(Resource.Id.TransportAidChoice);
        }
    }
}