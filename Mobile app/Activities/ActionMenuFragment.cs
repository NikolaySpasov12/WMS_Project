using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using mstore_WMS.AppCode;

namespace mstore_WMS.Activities
{
    public class ActionMenuFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            return inflater.Inflate(Resource.Layout.ActionMenuFragment, container, false);
        }
        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            Button btnInventur = View.FindViewById<Button>(Resource.Id.btnInventur);
            Button btnLagerInfo = View.FindViewById<Button>(Resource.Id.btnLagerInfo);
            Button btnUmpack = View.FindViewById<Button>(Resource.Id.btnUmpack);
            Button btnCloce = View.FindViewById<Button>(Resource.Id.btnCloce);
            TextView dbName = View.FindViewById<TextView>(Resource.Id.dbname);
            btnInventur.SetText(Resources.GetString(Resource.String.Action_Button1), null); 
            btnLagerInfo.SetText(Resources.GetString(Resource.String.Action_Button2), null);
            btnUmpack.SetText(Resources.GetString(Resource.String.Action_Button3), null);
            btnCloce.SetText(Resources.GetString(Resource.String.Action_Button4), null);

            Resources.GetString(Resource.String.Action_Button1);

            dbName.SetText(Settings.DB,null);

            btnCloce.Click += delegate (object sender, EventArgs e)
            {
                Intent loginIntent = new Intent(Activity, typeof(MainActivity));               
                StartActivity(loginIntent);
            };
            btnInventur.Click += delegate (object sender, EventArgs e)
            {

                CardFragment cardFragment = new CardFragment();
                FragmentTransaction fragmentTx = this.FragmentManager.BeginTransaction();
                fragmentTx.Replace(Resource.Id.realtivelayout_for_fragment, cardFragment);
                fragmentTx.AddToBackStack(null);
                Settings.CurrentFragment = "CardFragment";

                fragmentTx.Commit();
            };
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("_colorRes", 111);
        }
    }
}