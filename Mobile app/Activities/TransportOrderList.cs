using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using mstore_WMS.Adapters;
using mstore_WMS.Models;
using mstore_WMS.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace mstore_WMS.Activities
{
    [Activity(Label = "TransportOrderList")]
    public class TransportOrderList : Activity
    {
        private Button useless1;
        private Button useless2;
        private Button forwardButton;
        private ListView listView;
        private EditText hintTextView;

        private string transportAidNummer;
        private int transportAidID;
        private int shipmentID;
        private string shipmentNo;

        private DataTable dt;
        private List<Models.TableRow> tableRows = new List<Models.TableRow>();
        private int selectedIndex;

        private int trId;
        private int leId;
        private string leNummer = string.Empty;
        private string leTyp = string.Empty;
        private string sourceBin = string.Empty;
        private string targetBin = string.Empty;
        private int priority;
        private string from = string.Empty;
        private int places;
        private int digitsToCutFromTargetBin = -1;

        private const string MANUAL_LEVEL_INPUT = "MANUAL_LEVEL_INPUT";
        private const string TRANSPORT = "TRANSPORT";

        const ViewStates BUTTON_INVISIBLE = ViewStates.Gone;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.TransportOrderList);
            GetWidgetIDs();
            UnpackBundle();

            useless1.Visibility = useless2.Visibility = BUTTON_INVISIBLE;

            forwardButton.Text = GetString(Resource.String.btnNext_txt);

            forwardButton.Click += ForwardButton_Click;
            listView.ItemClick += ListView_ItemClick;
            SetTitle(Resource.String.labelTransportOrders);
        }

        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                if (GetDatatable())
                {
                    AssignValues(0);
                    PopulateTable();
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
                Log.Write(this, e.Message, Log.DebugLevels.ERROR);
            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            AssignValues(e.Position);
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            if (leId == 0 && trId == 0)
            {
                AssignValues(0);
            }

            if (CheckForChangesInTransportList())
            {
                PackBundleForTOD();
            }
            else
            {
                OnResume();
                Utility.ShowErrorMessage(this, Resource.String.transportNoLongerExists);
            }            
        }

        private bool CheckForChangesInTransportList()
        {
            if (GetDatatable())
            {
                foreach (DataRow dr in dt.Rows)
                {
                    if (trId == int.Parse(dr[FieldNames.TrId].ToString()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AssignValues(int index)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[index];
                trId = int.Parse(dr[FieldNames.TrId].ToString());
                leId = int.Parse(dr[FieldNames.LeId].ToString());
                leNummer = dr[FieldNames.LeNummer].ToString();
                leTyp = dr[FieldNames.LeTyp].ToString();
                sourceBin = dr[FieldNames.SourceBin].ToString();
                targetBin = dr[FieldNames.TargetBin].ToString();
                priority = int.Parse(dr[FieldNames.Priority].ToString());
                if (from == "Transport" && int.Parse(WSWmsHelper.LoadConfiguration(TRANSPORT, MANUAL_LEVEL_INPUT)) != 0)
                {
                    digitsToCutFromTargetBin = int.Parse(dr[FieldNames.LevelDigits].ToString());
                }
            }
        }

        private bool GetDatatable()
        {
            try
            {
                if (shipmentID == 0)
                    dt = WSWmsHelper.LoadTransportOrders(transportAidID);
                else
                    dt = WSWmsHelper.LoadShipmentTransportOrders(transportAidID, shipmentID);
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message + "Load orders exception: transpAidID = " + transportAidID);
                return false;
            }
            return true;
        }

        private void PopulateTable()
        {

            if (0 < dt.Rows.Count)
            {
                tableRows.Clear();
                foreach (DataRow item in dt.Rows)
                {
                    tableRows.Add(new Models.TableRow()
                    {
                        Text1 = item[FieldNames.LeNummer].ToString(),
                        Text2 = item[FieldNames.LeTyp].ToString(),
                        Text3 = item[FieldNames.SourceBin].ToString(),
                        Text4 = item[FieldNames.TargetBin].ToString(),
                        Text5 = item[FieldNames.Priority].ToString()
                    });
                }
            }
            else
            {
                hintTextView.Text = GetString(Resource.String.hintNoTranspOrders);
                forwardButton.Enabled = false;
                tableRows.Clear();
            }
            listView.Adapter = new ColumnsAdapter(this, tableRows, Resource.Layout.TransportOrderListTableRow, 5);
            selectedIndex = 0;
            listView.SetSelection(selectedIndex);
        }

        private void PackBundleForTOD()
        {
            Intent i = new Intent(this, typeof(TransportOrderDetails));
            Bundle b = new Bundle();

            b.PutInt("trid", trId);
            b.PutInt("leid", leId);
            b.PutString("lenum", leNummer);
            b.PutString("letyp", leTyp);
            b.PutString("sourceBin", sourceBin);
            b.PutString("targetBin", targetBin);
            b.PutInt("priority", priority);
            b.PutInt("transportAidId", transportAidID);
            b.PutString("from", from);
            b.PutInt("shipmentId", shipmentID);
            b.PutInt("places", places);
            b.PutInt("digitsToCutFromTargetBin", digitsToCutFromTargetBin);
            i.PutExtra("b", b);

            StartActivity(i);
        }

        private void UnpackBundle()
        {
            try
            {
                var bundle = Intent.GetBundleExtra("b");
                from = bundle.GetString("from");
                if (from == "Shipment")
                {
                    shipmentID = bundle.GetInt("shipmentId");
                    shipmentNo = bundle.GetString("shipmentNummer");
                    places = bundle.GetInt("places");
                    hintTextView.Text = GetString(Resource.String.chooseShipmentOrder);
                    
                }
                else if (from == "Transport")
                {
                    shipmentID = 0;
                    hintTextView.Text = GetString(Resource.String.chooseTransportOrder);
                }

                transportAidID = bundle.GetInt("transportAidID");
                transportAidNummer = bundle.GetString("transportAidNummer");

            }
            catch
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
            }
        }

        private void GetWidgetIDs()
        {
            useless1 = FindViewById<Button>(Resource.Id.buttonAct1);
            useless2 = FindViewById<Button>(Resource.Id.buttonAct2);
            forwardButton = FindViewById<Button>(Resource.Id.buttonAct3);
            listView = FindViewById<ListView>(Resource.Id.listViewTrans);
            hintTextView = FindViewById<EditText>(Resource.Id.tvHint);
            listView.ChoiceMode = ChoiceMode.Single;

        }
    }
}