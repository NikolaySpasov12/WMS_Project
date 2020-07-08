using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using mstore_WMS.Models;
using mstore_WMS.Models.Dto;
using mstore_WMS.Utils;
using System;
using System.Data;
using System.Linq;

namespace mstore_WMS.Activities
{
    [Activity(Label = "TransportOrderDetails")]
    public class TransportOrderDetails : Activity
    {
        private TextView leTextView;
        private TextView quelleTextView;
        private TextView zielTextView;
        private TextView leTypeTextView;
        private TextView prioTextView;
        private TextView le2TextView;
        private Button fotoButton;
        private Button wiegenButton;
        private Button printButton;
        private Button nextButton;

        private Button btnBinEmpty, btnBinFull;

        private EditText scanEditText;
        private EditText hintTextView;
        private TextView gewichtTextView;
        private TextView gewichtLabelTextView;
        private ImageButton btnKeyboard;
        private LinearLayout thisLayout;
        private LinearLayout secondLeLayout;

        private const string TRANSPORT = "TRANSPORT";
        private const string SCALES_INTERFACE_ACTIVE = "SCALES_INTERFACE_ACTIVE";
        private const string PHOTOGATE_ACTIVE = "PHOTOGATE_ACTIVE";
        private const string BINDIGITS = "PHOTOGATE_ACTIVE";
        private const string MANUAL_LEVEL_INPUT = "MANUAL_LEVEL_INPUT";

        private const string FAST_FINISH_TRANSPORT = "FAST_FINISH_TRANSPORT";
        private bool FastFinishTransport {
            get { return WSWmsHelper.LoadConfiguration(TRANSPORT, FAST_FINISH_TRANSPORT).Equals("1"); }
        }

        private InputTypes keyboardType = InputTypes.Null;

        private int trId;
        private int trId2;
        private int leId;
        private int leId2;
        private string leNummer;
        private string leTyp;
        private string sourceBin;
        private string targetBin;
        private string shortenedTargetBin;
        private int priority;
        private DataTable dt;
        private int transportOrderAidID;
        private string from;
        private string scan;
        private bool isLeScanned;
        private bool isZielScanned;
        private bool isStartPressed;
        private string weight;
        private int shipmentID;
        private int shipmentID2;
        private string username;
        private int places;
        private string leNummer2;
        private int digitsToCutFromTargetBin = -1;
        private bool manualInputConf;
        private int binLength;

        private UserDto user;

        private const string CONST_SHIPMENT = "Shipment";
        public const string FROM_PROD_PALLET = "ProdPallet";
        public const string FROM_TRANSPORT = "Transport";

        const ViewStates BUTTON_INVISIBLE = ViewStates.Gone;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.TransportOrderDetails);
            GetWidgetIDs();

            hintTextView.Text = GetString(Resource.String.scanLe);

            username = LoginActivity.LoadUser();
            user = WSWmsHelper.GetAllUsers().First(u => u.Login == username);

            fotoButton.Text = GetString(Resource.String.fotobtn);
            fotoButton.Visibility = BUTTON_INVISIBLE;
            fotoButton.Click += FotoButton_Click;

            wiegenButton.Text = GetString(Resource.String.wiegenBtn);
            wiegenButton.Visibility = BUTTON_INVISIBLE;
            wiegenButton.Click += WiegenButton_Click;

            printButton.Text = GetString(Resource.String.menuBtnPrintLabels).ToUpper(); ///etiketten
            printButton.Visibility = BUTTON_INVISIBLE;
            printButton.Click += PrintButton_Click;

            nextButton.Text = GetString(Resource.String.btnNext_txt);
            nextButton.Visibility = FastFinishTransport ? BUTTON_INVISIBLE : ViewStates.Visible;
            nextButton.Click += NextButton_Click;

            btnBinEmpty.Text = GetString(Resource.String.btnTitleBinEmpty).ToUpper();
            btnBinEmpty.Click += BtnBinEmpty_Click; ;
            btnBinEmpty.Visibility = BUTTON_INVISIBLE;

            btnBinFull.Text = GetString(Resource.String.btnTitleBinFull).ToUpper();
            btnBinFull.Visibility = BUTTON_INVISIBLE;
            btnBinFull.Click += BtnBinFull_Click; ;

            UnpackBundle(null);
            Load();


            btnKeyboard.Click += BtnKeyboard_Click;
            scanEditText.KeyPress += ScanEditText_KeyPress;
            scanEditText.InputType = Android.Text.InputTypes.Null;

            SetTitle(Resource.String.labelTransportOrderDetails);
        }

        private void BtnBinFull_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(this).SetTitle(Resources.GetString(Resource.String.msgTitleInfo))
                        .SetMessage(Resources.GetString(Resource.String.msgConfirmBinFull)).SetPositiveButton(
                            Resources.GetString(Resource.String.Yes), delegate
                            {
                                FullBinAction();
                            }).SetNeutralButton(Resources.GetString(Resource.String.No), delegate { }).Show();
        }

        private void FullBinAction()
        {
            string newBin = "";
            try
            {
                newBin = WSWmsHelper.TranspBinFull(zielTextView.Text, leNummer);
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, "TranspBinFull - ERROR:" + ex.Message);
                return;
            }

            if (string.IsNullOrEmpty(newBin))
            {
                Utility.ShowErrorMessage(this, "NO New bin");
                return;
            }

            zielTextView.Text = newBin;
        }

        private void BtnBinEmpty_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(this).SetTitle(Resources.GetString(Resource.String.msgTitleInfo))
                        .SetMessage(Resources.GetString(Resource.String.msgConfirmBinEmpty)).SetPositiveButton(
                            Resources.GetString(Resource.String.Yes), delegate
                            {
                                EmptyBinAction();
                            }).SetNeutralButton(Resources.GetString(Resource.String.No), delegate { }).Show();
        }

        private void EmptyBinAction()
        {
            try
            {
                string binToDelete = quelleTextView.Text;
                if (WSWmsHelper.DeleteTranspBinEmpty(binToDelete, leNummer))
                {
                    SetNextTransport();
                }
                else
                {
                    Utility.ShowErrorMessage(this, Resources.GetString(Resource.String.msgOperationFailed));
                }
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, "EmptyBinAction - ERROR:" + ex.Message);
                return;
            }
        }


        private void BtnKeyboard_Click(object sender, EventArgs e)
        {
            Utility.ShowKeyboard(scanEditText, this, keyboardType);
        }

        private void WiegenButton_Click(object sender, EventArgs e)
        {
            try
            {
                weight = WSWmsHelper.GetGrossWeight(transportOrderAidID.ToString());
            }
            catch (Exception)
            {
                Log.Write(this,
                    $"Problem at WiegenButton in TransportOrderDetails, transportOrderAidID = {transportOrderAidID}",
                    Log.DebugLevels.ERROR);
            }

            if (string.IsNullOrEmpty(weight))
            {
                gewichtTextView.SetBackgroundColor(Color.Yellow);
                gewichtTextView.Text = "Fehler beim Wiegen!";
            }
            else
            {
                gewichtTextView.SetBackgroundColor(Color.LightGreen);
                gewichtTextView.Text = weight;
            }

            if (fotoButton.Visibility == ViewStates.Visible)
            {
                fotoButton.Enabled = true;
                hintTextView.Text = GetString(Resource.String.hintPressFoto);
            }
        }

        private void FotoButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!WSWmsHelper.TakeLePictures(leNummer))
                {
                    Utility.ShowErrorMessage(this, GetString(Resource.String.couldNotTakeFotoErr));
                }
            }
            catch (Exception)
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.couldNotTakeFotoErr));
                Log.Write(this, "Problem at fotoButton in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }

            nextButton.Enabled = true;
            fotoButton.Enabled = false;
            wiegenButton.Enabled = false;
            hintTextView.Text = GetString(Resource.String.hintPressNext);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            scan = savedInstanceState.GetString("scan");
            UnpackBundle(savedInstanceState);
            if (savedInstanceState.GetBoolean("isLeScanned"))
            {
                SetLeScanedOk();
                quelleTextView.SetBackgroundColor(Color.LightGreen);
                isLeScanned = true;
                hintTextView.Text = GetString(Resource.String.hintStartTransport);
                Start();
            }
            else
            {
                SetNewOrder();
                Load();
                zielTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                return;
            }

            if (savedInstanceState.GetBoolean("isStartPressed"))
            {
                isStartPressed = true;
                zielTextView.SetBackgroundColor(Color.Yellow);
            }

            if (savedInstanceState.GetBoolean("isZielScanned"))
            {
                zielTextView.SetBackgroundColor(Color.LightGreen);
                isZielScanned = true;
                if (from != CONST_SHIPMENT)
                {
                    nextButton.Enabled = true;
                    hintTextView.Text = GetString(Resource.String.hintPressNext);
                }
                else if (wiegenButton.Visibility == ViewStates.Visible)
                {
                    wiegenButton.Enabled = true;
                    hintTextView.Text = GetString(Resource.String.hintPressWiegen);
                }
            }

            base.OnRestoreInstanceState(savedInstanceState);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt("trid", trId);
            outState.PutInt("leid", leId);
            outState.PutString("lenum", leNummer);
            outState.PutString("letyp", leTyp);
            outState.PutString("sourceBin", sourceBin);
            outState.PutString("targetBin", targetBin);
            outState.PutInt("priority", priority);
            outState.PutInt("transportAidId", transportOrderAidID);
            outState.PutString("from", from);
            outState.PutInt("shipmentId", shipmentID);
            outState.PutBoolean("isLeScanned", isLeScanned);
            outState.PutString("scan", scan);
            outState.PutBoolean("isZielScanned", isZielScanned);
            outState.PutBoolean("isStartPressed", isStartPressed);
            base.OnSaveInstanceState(outState);
        }

        private void ScanEditText_KeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = false;
            scan = string.Empty;

            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Up)
            {
                e.Handled = true;

                Utility.HideKeyboard(scanEditText, this);
                scanEditText.InputType = Android.Text.InputTypes.Null;

                if (scanEditText.Text != string.Empty)
                {
                    scan = scanEditText.Text.ToUpper();
                    if (hintTextView.Text == GetString(Resource.String.scanLe))
                    {
                        if (scan == leNummer.ToUpper())
                        {
                            FillScannedLe();
                        }
                        else
                        {
                            GetNewTransportOrder();
                        }

                    }

                    else if (hintTextView.Text == GetString(Resource.String.hintScanSecondLe))
                    {
                        GetSecondLe();
                    }

                    else if (hintTextView.Text == GetString(Resource.String.hintGoToTargetBinAndScan))
                    {
                        if (scan == zielTextView.Text.ToUpper())
                        {
                            zielTextView.SetBackgroundColor(Color.LightGreen);
                            isZielScanned = true;
                            if (wiegenButton.Visibility == ViewStates.Visible)
                            {
                                wiegenButton.Enabled = true;
                            }
                            else if (fotoButton.Visibility == ViewStates.Visible)
                            {
                                fotoButton.Enabled = true;
                            }
                            else
                            {
                                if (FastFinishTransport)
                                {
                                    NextButton_Click(nextButton, new EventArgs());
                                }
                                else
                                {
                                    hintTextView.Text = GetString(Resource.String.hintPressNext);
                                    nextButton.Enabled = true;
                                }
                            }
                        }
                        else if (from == FROM_TRANSPORT
                                && manualInputConf
                                && scan.Substring(0, binLength - digitsToCutFromTargetBin) == zielTextView.Text.Substring(0, zielTextView.Text.Length - digitsToCutFromTargetBin)
                            )
                        {
                            hintTextView.Text = GetString(Resource.String.hintEnterLastDigitsTargetBin);
                            keyboardType = InputTypes.ClassNumber;
                            BtnKeyboard_Click(btnKeyboard, null);
                        }
                        else
                        {
                            Utility.ShowErrorMessage(this, GetString(Resource.String.errIncorrectTargetBin));
                        }
                    }
                    else if (hintTextView.Text == GetString(Resource.String.hintEnterLastDigitsTargetBin))
                    {
                        if(!Utility.CompareLastScannedDigits(zielTextView.Text, scan, digitsToCutFromTargetBin))
                        {
                            Utility.ShowErrorMessage(this, Resource.String.invalidScan);
                            scanEditText.Text = "";
                            BtnKeyboard_Click(btnKeyboard, null);
                            return;
                        }
                        zielTextView.SetBackgroundColor(Color.LightGreen);
                        isZielScanned = true;
                        scanEditText.InputType = InputTypes.Null;
                        if (wiegenButton.Visibility == ViewStates.Visible)
                        {
                            wiegenButton.Enabled = true;
                        }
                        else if (fotoButton.Visibility == ViewStates.Visible)
                        {
                            fotoButton.Enabled = true;
                        }
                        else
                        {

                            if (FastFinishTransport)
                            {
                                NextButton_Click(nextButton, new EventArgs());
                            }
                            else
                            {
                                hintTextView.Text = GetString(Resource.String.hintPressNext);
                                nextButton.Enabled = true;
                            }
                        }
                    }
                }
                else
                {
                    Utility.ShowErrorMessage(this, GetString(Resource.String.nothingScanned));
                }

                scanEditText.Text = "";
            }
        }

        private void FillScannedLe()
        {
            SetLeScanedOk();
            quelleTextView.SetBackgroundColor(Color.LightGreen);
            isLeScanned = true;
            if (places == 2 && secondLeLayout.Visibility == ViewStates.Visible)
            {
                hintTextView.Text = GetString(Resource.String.hintScanSecondLe);
            }
            else
            {
                hintTextView.Text = GetString(Resource.String.hintStartTransport);
            }
            Start();

            btnBinEmpty.Visibility = BUTTON_INVISIBLE;
            btnBinFull.Visibility = ViewStates.Visible;
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (from == CONST_SHIPMENT)
                {
                    if (WSWmsHelper.FinishShipmentTransport(trId, shipmentID, leId, weight))
                    {
                        if (secondLeLayout.Visibility == ViewStates.Visible)
                        {
                            if (!WSWmsHelper.FinishShipmentTransport(trId2, shipmentID, leId2, weight))
                            {
                                Utility.ShowErrorMessage(this, Resource.String.errProblemWithSecondLeFinish);
                            }

                            le2TextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                            le2TextView.Text = string.Empty;
                        }
                        isLeScanned = false;
                        isZielScanned = false;
                        weight = "0";
                        SetNewOrder();
                        Load();
                        zielTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                    }
                    else
                    {
                        Log.Write(this, "Problem at nextButton in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
                        Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
                    }
                }
                else if (from == FROM_PROD_PALLET)
                {
                    if (WSWmsHelper.FinishTransport(trId))
                    {
                        OnBackPressed();
                        return;
                    }
                    Log.Write(this, "Problem at nextButton in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
                    Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
                }
                else
                {
                    if (WSWmsHelper.FinishTransport(trId))
                    {
                        SetNextTransport();
                    }
                    else
                    {
                        Log.Write(this, "Problem at nextButton in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
                        Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, ex.Message);
                Log.Write(this, "Problem at nextButton in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }
        }

        private void SetNextTransport()
        {
            isLeScanned = false;
            isZielScanned = false;
            weight = "0";
            SetNewOrder();
            Load();
            zielTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
        }

        private void Start()
        {
            try
            {
                bool success;
                bool success2 = false;

                if (from == CONST_SHIPMENT)
                {
                    if (le2TextView.Text == string.Empty)
                    {
                        success = WSWmsHelper.PickShipmentTransport(trId.ToString(), transportOrderAidID, shipmentID.ToString(), leId.ToString(), user.Id);
                        secondLeLayout.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        success = WSWmsHelper.PickShipmentTransport(trId.ToString(), transportOrderAidID, shipmentID.ToString(), leId.ToString(), user.Id);
                        success2 = WSWmsHelper.PickShipmentTransport(trId2.ToString(), transportOrderAidID, shipmentID.ToString(), leId2.ToString(), user.Id);
                    }
                }
                else
                {
                    success = WSWmsHelper.PickTransport(trId.ToString(), transportOrderAidID);
                }

                if (success)
                {
                    if (from == CONST_SHIPMENT && (secondLeLayout.Visibility == ViewStates.Visible && !success2))
                    {
                        Utility.ShowErrorMessage(this, Resource.String.errProblemWithSecondLeStart);
                        secondLeLayout.Visibility = ViewStates.Gone;
                    }
                    zielTextView.SetBackgroundColor(Color.Yellow);
                    hintTextView.Text = GetString(Resource.String.hintGoToTargetBinAndScan);
                }
                else
                {
                    Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
                }

                scanEditText.Enabled = true;
                scanEditText.RequestFocus();
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, ex.Message);
            }
        }

        private bool ExistsStartedOrder()
        {
            try
            {
                if (from == CONST_SHIPMENT)
                {
                    dt = WSWmsHelper.LoadShipmentTransportOrders(transportOrderAidID, shipmentID);
                }
                else
                {
                    if (from.Equals(FROM_PROD_PALLET))
                    {
                        dt = WSWmsHelper.GetTransport(trId);
                    }
                    else
                    {
                        dt = WSWmsHelper.LoadTransportOrders(transportOrderAidID);
                    }
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (from == FROM_PROD_PALLET)
                    {
                        if (row[FieldNames.transpStatus].ToString() == "PICKED" && row[FieldNames.LeNummer].ToString() == leNummer)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (row[FieldNames.transpStatus].ToString() == "PICKED")
                        {
                            leNummer = row[FieldNames.LeNummer].ToString();
                            secondLeLayout.Visibility = ViewStates.Gone;
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
            }

            return false;
        }

        private void Load()
        {
            try
            {
                leTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                quelleTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                gewichtTextView.SetBackgroundResource(Resource.Color.formDefectGoodsBackColor);
                nextButton.Enabled = false;


                if (ExistsStartedOrder())
                {
                    if (from == FROM_PROD_PALLET)
                    {
                        SetNewOrderFromProdPallet();
                    }
                    else
                    {
                        SetNewOrder();
                    }

                    SetLeScanedOk();
                    quelleTextView.SetBackgroundColor(Color.LightGreen);
                    isLeScanned = true;
                    zielTextView.SetBackgroundColor(Color.Yellow);
                    hintTextView.Text = GetString(Resource.String.hintGoToTargetBinAndScan);
                }
                else
                {
                    if (places == 2)
                    {
                        secondLeLayout.Visibility = ViewStates.Visible;
                    }
                }

                FillForm();

                if (from.Equals(FROM_PROD_PALLET))
                {
                    if (!string.IsNullOrEmpty(leNummer))
                    {
                        FillScannedLe();
                    }
                }
            }
            catch
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
            }
        }

        private void FillForm()
        {
            leTextView.Text = leNummer;
            leTypeTextView.Text = leTyp;
            prioTextView.Text = priority.ToString();
            quelleTextView.Text = sourceBin;
            zielTextView.Text = targetBin;
            binLength = targetBin.Length;
            if (!isLeScanned)  printButton.Visibility = BUTTON_INVISIBLE;
        }

        private void SetNewOrderFromProdPallet()
        {
            try
            {
                dt = WSWmsHelper.GetTransport(trId);
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, ex.Message);
                Log.Write(this, "Problem at SetNewOrderFromProdPallet in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }

            if (dt != null && dt.Rows.Count == 0)
            {
                OnBackPressed();
            }

            try
            {
                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(leNummer))
                {
                    foreach (DataRow drSearch in dt.Rows)
                    {
                        if (leNummer.Equals(drSearch[FieldNames.LeNummer].ToString()))
                        {
                            dr = drSearch;
                            break;
                        }
                    }
                }

                leNummer = dr[FieldNames.LeNummer].ToString();
                le2TextView.Text = leNummer2;
                leTyp = dr[FieldNames.LeTyp].ToString();
                sourceBin = dr[FieldNames.SourceBin].ToString();
                targetBin = dr[FieldNames.TargetBin].ToString();
                priority = int.Parse(dr[FieldNames.Priority].ToString());
                trId = int.Parse(dr[FieldNames.TrId].ToString());
                leId = int.Parse(dr[FieldNames.LeId].ToString());
                gewichtTextView.Text = string.Empty;
                hintTextView.Text = GetString(Resource.String.scanLe);
                if (from == FROM_TRANSPORT && manualInputConf)
                {
                    digitsToCutFromTargetBin = int.Parse(dr[FieldNames.LevelDigits].ToString());
                    shortenedTargetBin = targetBin.Substring(0, targetBin.Length - digitsToCutFromTargetBin);
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
                Log.Write(this, "Get order info from datatable for transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }
        }

        private void SetNewOrder()
        {
            try
            {
                if (from == CONST_SHIPMENT)
                {
                    dt = WSWmsHelper.LoadShipmentTransportOrders(transportOrderAidID, shipmentID);
                }
                else
                {
                    if (from.Equals(FROM_PROD_PALLET))
                    {
                        dt = WSWmsHelper.GetTransport(trId);
                    }
                    else
                    {
                        dt = WSWmsHelper.LoadTransportOrders(transportOrderAidID);
                    }
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
                Log.Write(this, "Problem at SetNewOrder in TransportOrderDetails, transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }

            if (dt != null && dt.Rows.Count == 0)
            {
                OnBackPressed();
            }

            try
            {
                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(leNummer))
                {
                    foreach (DataRow drSearch in dt.Rows)
                    {
                        if (leNummer.Equals(drSearch[FieldNames.LeNummer].ToString()))
                        {
                            dr = drSearch;
                            break;
                        }
                    }
                }

                leNummer = dr[FieldNames.LeNummer].ToString();
                leTyp = dr[FieldNames.LeTyp].ToString();
                sourceBin = dr[FieldNames.SourceBin].ToString();
                targetBin = dr[FieldNames.TargetBin].ToString();
                priority = int.Parse(dr[FieldNames.Priority].ToString());
                trId = int.Parse(dr[FieldNames.TrId].ToString());
                leId = int.Parse(dr[FieldNames.LeId].ToString());
                gewichtTextView.Text = string.Empty;
                hintTextView.Text = GetString(Resource.String.scanLe);

                btnBinEmpty.Visibility = BUTTON_INVISIBLE;
                btnBinFull.Visibility = BUTTON_INVISIBLE;

                if (from == FROM_TRANSPORT && manualInputConf)
                {
                    digitsToCutFromTargetBin = int.Parse(dr[FieldNames.LevelDigits].ToString());
                    shortenedTargetBin = targetBin.Substring(0, targetBin.Length - digitsToCutFromTargetBin);
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(this, e.Message);
                Log.Write(this, "Get order info from datatable for transportOrderAidID = " + transportOrderAidID, Log.DebugLevels.ERROR);
            }
        }

        private void GetSecondLe()
        {
            foreach (DataRow item in dt.Rows)
            {
                if (scan == item[FieldNames.LeNummer].ToString().ToUpper())
                {
                    leId2 = int.Parse(item[FieldNames.LeId].ToString());
                    trId2 = int.Parse(item[FieldNames.TrId].ToString());
                    leNummer2 = item[FieldNames.LeNummer].ToString();

                    le2TextView.SetBackgroundColor(Color.LightGreen);
                    le2TextView.Text = scan;
                    hintTextView.Text = GetString(Resource.String.hintStartTransport);
                    Start();
                    return;
                }
            }
            Utility.ShowErrorMessage(this, GetString(Resource.String.errNoTransportFound));
        }

        private void GetNewTransportOrder()
        {
            foreach (DataRow item in dt.Rows)
            {
                if (scan == item[FieldNames.LeNummer].ToString().ToUpper())
                {
                    leTyp = item[FieldNames.LeTyp].ToString();
                    sourceBin = item[FieldNames.SourceBin].ToString();
                    targetBin = item[FieldNames.TargetBin].ToString();
                    priority = int.Parse(item[FieldNames.Priority].ToString());
                    leId = int.Parse(item[FieldNames.LeId].ToString());
                    trId = int.Parse(item[FieldNames.TrId].ToString());
                    leNummer = item[FieldNames.LeNummer].ToString();

                    SetLeScanedOk();
                    quelleTextView.SetBackgroundColor(Color.LightGreen);

                    if (places == 2 && secondLeLayout.Visibility == ViewStates.Visible)
                    {
                        hintTextView.Text = GetString(Resource.String.hintScanSecondLe);
                    }
                    else
                    {
                        hintTextView.Text = GetString(Resource.String.hintStartTransport);
                    }
                    Start();
                    FillForm();
                    return;
                }
            }
            Utility.ShowErrorMessage(this, GetString(Resource.String.errNoTransportFound));
        }

        private void UnpackBundle(Bundle bundle)
        {
            try
            {
                manualInputConf = WSWmsHelper.LoadConfiguration(TRANSPORT, MANUAL_LEVEL_INPUT).Equals("1");
                if (bundle == null)
                {
                    bundle = Intent.GetBundleExtra("b");
                }

                from = bundle.GetString("from");
                trId = bundle.GetInt("trid");
                leId = bundle.GetInt("leid");
                leNummer = bundle.GetString("lenum");
                leTyp = bundle.GetString("letyp");
                sourceBin = bundle.GetString("sourceBin");
                targetBin = bundle.GetString("targetBin");
                if (from == FROM_TRANSPORT && manualInputConf)
                {
                    digitsToCutFromTargetBin = bundle.GetInt("digitsToCutFromTargetBin");
                    shortenedTargetBin = bundle.GetString("targetBin").Substring(0, bundle.GetString("targetBin").Length - digitsToCutFromTargetBin);
                }
                priority = bundle.GetInt("priority");
                transportOrderAidID = bundle.GetInt("transportAidId");

                if (from == CONST_SHIPMENT)
                {
                    shipmentID = bundle.GetInt("shipmentId");
                    places = bundle.GetInt("places");

                    fotoButton.Enabled = false;
                    wiegenButton.Enabled = false;

                    if (int.Parse(WSWmsHelper.LoadConfiguration(TRANSPORT, SCALES_INTERFACE_ACTIVE)) != 0)
                    {
                        wiegenButton.Visibility = ViewStates.Visible;
                        gewichtLabelTextView.Visibility = ViewStates.Visible;
                        gewichtTextView.Visibility = ViewStates.Visible;
                    }
                    if (int.Parse(WSWmsHelper.LoadConfiguration(TRANSPORT, PHOTOGATE_ACTIVE)) != 0)
                    {
                        fotoButton.Visibility = ViewStates.Visible;
                    }
                }
                if (from == FROM_PROD_PALLET)
                {
                    leNummer2 = bundle.GetString("lenum2");
                    thisLayout.SetBackgroundColor(Color.ParseColor("#00FFFF"));
                }
            }
            catch
            {
                Utility.ShowErrorMessage(this, GetString(Resource.String.msgTitleError));
            }
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(PrintShippingLabelsActivity));
            intent.PutExtra(PrintShippingLabelsActivity.EXTRA_LE_NUMBER, leTextView.Text);
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        void SetLeScanedOk()
        {
            leTextView.SetBackgroundColor(Color.LightGreen);
            bool printalble = false;
            try
            {
                if(WSWmsHelper.CheckForLePrintable(leNummer))
                {
                    printButton.Visibility = ViewStates.Visible;
                    printButton.Enabled = true;
                }
            }
            catch (Exception e)
            {
                printButton.Visibility = BUTTON_INVISIBLE;
                Utility.ShowErrorMessage(this, e.Message);
                return;
            }
        }

        private void GetWidgetIDs()
        {
            leTextView = FindViewById<TextView>(Resource.Id.LETextViewTOD);
            le2TextView = FindViewById<TextView>(Resource.Id.LE2TextViewTOD);
            secondLeLayout = FindViewById<LinearLayout>(Resource.Id.secondLeNumTOD);
            quelleTextView = FindViewById<TextView>(Resource.Id.quelleTextViewTOD);
            zielTextView = FindViewById<TextView>(Resource.Id.zielTextViewTOD);
            leTypeTextView = FindViewById<TextView>(Resource.Id.leTypTextViewTOD);
            prioTextView = FindViewById<TextView>(Resource.Id.prioTextViewTOD);

            printButton = FindViewById<Button>(Resource.Id.buttonAct1);
            wiegenButton = FindViewById<Button>(Resource.Id.buttonAct2);
            fotoButton = FindViewById<Button>(Resource.Id.buttonAct3);
            nextButton = FindViewById<Button>(Resource.Id.buttonAct4);

            btnBinEmpty = FindViewById<Button>(Resource.Id.buttonAct5);
            btnBinFull = FindViewById<Button>(Resource.Id.buttonAct6);

            hintTextView = FindViewById<EditText>(Resource.Id.tvHint);
            scanEditText = FindViewById<EditText>(Resource.Id.editTextScan);
            gewichtTextView = FindViewById<TextView>(Resource.Id.gewichtTextViewTOD);
            gewichtTextView.Visibility = ViewStates.Gone;
            gewichtLabelTextView = FindViewById<TextView>(Resource.Id.gweichtLabelTextView);
            gewichtLabelTextView.Visibility = ViewStates.Gone;
            btnKeyboard = FindViewById<ImageButton>(Resource.Id.btnKeyboard);
            thisLayout = FindViewById<LinearLayout>(Resource.Id.TransportOrderDetails);
        }
    }
}