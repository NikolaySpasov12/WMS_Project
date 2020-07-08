using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using mstore_WMS.Utils;
using System;
using System.Collections.Generic;
using mstore_WMS.Models.Enums;
using mstore_WMS.Models.Dto;
using System.Linq;

namespace mstore_WMS.Activities
{
    [Activity(MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public static readonly Dictionary<int, int> ButtonEnable = new Dictionary<int, int>();
        public const int BUTTON_HIDE_ID = 0;

        private static readonly Dictionary<int, string> ButtonTexts = new Dictionary<int, string>();
        private static readonly Dictionary<int, Drawable> ButtonBackground = new Dictionary<int, Drawable>();

        public static readonly int[] ButtonIds =
        {
            Resource.Id.buttonWEEinlagerung,
            Resource.Id.buttonTransport,
            Resource.Id.buttonPick,
            Resource.Id.buttonStoreProduction,
            Resource.Id.buttonReallocation,
            Resource.Id.buttonInventory,
            Resource.Id.buttonGoodsReceipt,
            Resource.Id.buttonPrintLabels,
            Resource.Id.buttonInitialStocktaking,
            Resource.Id.buttonInfo,
            Resource.Id.buttonWeHandelsware,
            Resource.Id.buttonSplitTU,
            Resource.Id.buttonRepack,
            Resource.Id.buttonDeleteTU
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                RequestWindowFeature(WindowFeatures.NoTitle);
                SetContentView(Resource.Layout.Main);

                foreach (int btnId in ButtonIds)
                {
                    Button btn = FindViewById<Button>(btnId);
                    if (null != btn)
                    {
                        btn.Click += GridButton_Click;
                        ButtonTexts[btn.Id] = btn.Text;
                        ButtonBackground[btn.Id] = btn.Background;
                    }
                }

                var showLogin = Intent.GetBooleanExtra("showLogin", true);
                if (showLogin)
                {
                    ShowLoginForm();
                }
            }
            catch (Exception ex)
            {
                Utility.ShowErrorMessage(this, ex.Message);
            }
        }

        private void ShowLoginForm()
        {
            Intent intent = new Intent(this, typeof(LoginActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowWagonListForm()
        {
            Intent intent = new Intent(this, typeof(WagonItemListActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowTransportForm(bool isTransport)
        {
            Intent intent = new Intent(this, typeof(TransportAidChoice));
            intent.SetFlags(ActivityFlags.NewTask);
            intent.PutExtra(TransportAidChoice.EXTRA_IS_TRANSPORT, isTransport);
            StartActivity(intent);
        }

        /// <summary>
        /// Resume picking from where the picker left off.
        /// </summary>
        private void ShowPickingForm()
        {
            Intent activity;
            string lastActivity = WSWmsHelper.GetPickingProgress(LoginActivity.User);
            if (lastActivity == PickingProgressLastActivity.PickingOverview.ToString())
            {
                activity = new Intent(this, typeof(NextOrderActivity));
            }
            else
            if (lastActivity == PickingProgressLastActivity.TuScan.ToString())
            {
                activity = new Intent(this, typeof(TuScanActivity));
            }
            else
            if (lastActivity == PickingProgressLastActivity.ItemScan.ToString())
            {
                activity = new Intent(this, typeof(ItemScanActivity));
            }
            else
            {
                activity = new Intent(this, typeof(PickActivity));
            }

            StartActivity(activity);
        }

        private void ShowPrintLabelsForm()
        {
            Intent intent = new Intent(this, typeof(PrintShippingLabelsActivity));
            intent.PutExtra(PrintShippingLabelsActivity.EXTRA_LE_NUMBER, string.Empty);
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowInitialStocktakingForm()
        {
            Intent intent = new Intent(this, typeof(InitialStockTakingOrderListActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowReallocationForm()
        {
            Intent intent = new Intent(this, typeof(LeRelocation));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void SetGoodsReceiptBin()
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.GoodsReceiptBinChoice, null);
            Spinner sBin = view.FindViewById<Spinner>(Resource.Id.spinnerWeBin);
            List<IdAndNameDto> places = WSWmsHelper.WeLoadLocation();
            var placeNames = places.Select(x => x.Name).ToList();
            ArrayAdapter adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, placeNames);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sBin.Adapter = adapter;

            IdAndNameDto binData = Utility.WeBin;
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(binData.Name)) {
                for (int i = 0; i < places.Count; i++)
                {
                    if(places[i].Name.Equals(binData.Name))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            sBin.SetSelection(selectedIndex);

            AlertDialog.Builder alertbuilder = new AlertDialog.Builder(this);
            alertbuilder.SetView(view);
            
            alertbuilder.SetCancelable(false)
            .SetPositiveButton(Resource.String.Yes, delegate
            {
                Utility.WeBin = new IdAndNameDto()
                {
                    Name = sBin.SelectedItem.ToString(),
                    Id = places[(int)sBin.SelectedItemId].Id
                };

                ShowGoodsReceiptListForm();
            })
            .SetNegativeButton(Resource.String.No, delegate
            {
            });
            AlertDialog dialog = alertbuilder.Create();
            alertbuilder.Dispose();
            dialog.Show();
        }

        private void ShowGoodsReceiptListForm()
        {
            Intent intent = new Intent(this, typeof(GoodsReceiptListActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowInventoryMenuForm()
        {
            Intent intent = new Intent(this, typeof(StockTakingOrderListActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowLeInfo()
        {
            Intent intent = new Intent(this, typeof(LEInformation));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowSplitTUform()
        {
            Intent intent = new Intent(this, typeof(SplitLeForExpeditionActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowWeHandelswareForm()
        {
            Intent intent = new Intent(this, typeof(WeHandelswareActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowRepackForm()
        {
            Intent intent = new Intent(this, typeof(RepackActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        private void ShowDeleteTuForm()
        {
            Intent intent = new Intent(this, typeof(DeleteLeActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        void ShowInfoMenu(Button button)
        {
            PopupMenu menu = new PopupMenu(this, button);
            menu.Inflate(Resource.Menu.menuInfo);

            menu.MenuItemClick += (s1, arg1) => {
                switch (arg1.Item.ItemId)
                {
                    case Resource.Id.menuOptArticleInfo:
                        ShowArticleBinInfoForm(true);
                        break;
                    case Resource.Id.menuOptBinInfo:
                        ShowArticleBinInfoForm(false);
                        break;
                    case Resource.Id.menuOptLeInfo:
                        ShowLeInfo();
                        break;
                }
            };

            menu.DismissEvent += (s2, arg2) => {
                ///Console.WriteLine("menu dismissed");
            };
            menu.Show();

        }

        private void ShowArticleBinInfoForm(bool isArticle)
        {
            Intent intent = new Intent(this, typeof(ItemBinInfoActivity));
            intent.PutExtra(ItemBinInfoActivity.EXTRA_INFO_FOR, isArticle ? ItemBinInfoActivity.INFO_FOR_ARTICLE : ItemBinInfoActivity.INFO_FOR_BIN);
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();
            RequestedOrientation = Utility.GetActivityOrientation();

            foreach (int btnId in ButtonEnable.Keys)
            {
                Button btn = FindViewById<Button>(btnId);
                if (null != btn)
                {
                    btn.Visibility = 0 != ButtonEnable[btnId] ? ViewStates.Visible : ViewStates.Gone;
                    btn.Tag = ButtonEnable[btnId];
                    if (null != btn.Tag && 0 != (int)btn.Tag)
                    {
                        if (ButtonTexts.ContainsKey((int)btn.Tag)
                            && ButtonBackground.ContainsKey((int)btn.Tag))
                        {
                            btn.Text = ButtonTexts[(int)btn.Tag];
                            btn.Background = ButtonBackground[(int)btn.Tag];
                        }
                    }
                }
            }

            var tvLoginUser = FindViewById<TextView>(Resource.Id.loginUser);
            tvLoginUser.Text = GetString(Resource.String.loggedInAs) + LoginActivity.User;
        }

        //add custom icon to toolbar
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menuMain, menu);
            if (menu != null)
            {
                menu.FindItem(Resource.Id.menuOptHelp).SetVisible(true);
                menu.FindItem(Resource.Id.menuOptLogout).SetVisible(true);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        //define action for tolbar icon press
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menuOptLogout:
                    ShowLoginForm();
                    return true;
                case Resource.Id.menuOptHelp:
                    Toast.MakeText(this, "No help yet!", ToastLength.Long).Show();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void GridButton_Click(object sender, EventArgs e)
        {
            if (null == ((Button)sender).Tag) return;

            int buttonId = (int)((Button)sender).Tag;

            switch (buttonId)
            {
                case Resource.Id.buttonWEEinlagerung:
                    ShowWagonListForm();
                    break;

                case Resource.Id.buttonPick:
                    ShowPickingForm();
                    break;

                case Resource.Id.buttonTransport:
                    ShowTransportForm(true);
                    break;

                case Resource.Id.buttonStoreProduction:
                    ShowTransportForm(false);
                    break;

                case Resource.Id.buttonReallocation:
                    ShowReallocationForm();
                    break;

                case Resource.Id.buttonInventory:
                    ShowInventoryMenuForm();
                    break;

                case Resource.Id.buttonGoodsReceipt:
                    ///ShowGoodsReceiptListForm();
                    SetGoodsReceiptBin();
                    break;

                case Resource.Id.buttonPrintLabels:
                    ShowPrintLabelsForm();
                    break;

                case Resource.Id.buttonInitialStocktaking:
                    ShowInitialStocktakingForm();
                    break;

                case Resource.Id.buttonInfo:
                    ShowInfoMenu((Button)sender);
                    break;

                case Resource.Id.buttonWeHandelsware:
                    ShowWeHandelswareForm();
                    break;

                case Resource.Id.buttonSplitTU:
                    ShowSplitTUform();
                    break;

                case Resource.Id.buttonRepack:
                    ShowRepackForm();
                    break;

                case Resource.Id.buttonDeleteTU:
                    ShowDeleteTuForm();
                    break;
            }
        }

        //to avoid direct app exit on backpreesed and to show fragment from stack
        public override async void OnBackPressed()
        {
            if (FragmentManager.BackStackEntryCount != 0)
            {
                FragmentManager.PopBackStack();
            }
            else
            {
                if (await Utility.ShowConfirmationDialog(this, GetString(Resource.String.menuLogoutConfirm)))
                {
                    ShowLoginForm();
                }
            }
        }
    }
}