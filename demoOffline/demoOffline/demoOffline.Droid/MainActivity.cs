using Android.App;
using Android.Widget;
using Android.OS;
using Android.Net;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using Android.OS.Storage;
using Esri.ArcGISRuntime.Mapping.Popup;
using Esri.ArcGISRuntime.Data;
using System;
using Android.Views;

namespace demoOffline.Droid
{
    //[Activity (Label = "testNativeShared.Droid", MainLauncher = true, Icon = "@drawable/icon")]
    [Activity(Label = "DemoXamarinNativeAndroid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        MapView _mapView;
        MapViewModel _mapViewModel;
        LinearLayout _statusArea;
        ProgressBar _busyIndicator;
        TextView _statusText;
        Button _offlineBtn;
        Button _syncBtn;
        //bool _loading;
        //string _localFilePath;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // on initialise l'UI et quelque variables qui serviront à manipuler les éléments de l'UI
            SetContentView(Resource.Layout.Main);

            _statusArea = FindViewById<LinearLayout>(Resource.Id.StatusArea);
            _busyIndicator = FindViewById<ProgressBar>(Resource.Id.BusyIndicator);
            _statusText = FindViewById<TextView>(Resource.Id.StatusText);
            _offlineBtn = FindViewById<Button>(Resource.Id.OfflineBtn);
            _offlineBtn.Click += OfflineBtn_Click;
            _syncBtn = FindViewById<Button>(Resource.Id.SyncBtn);
            _syncBtn.Click += SyncBtn_Click;

            _mapView = FindViewById<MapView>(Resource.Id.MainMapView);
            _mapView.DrawStatusChanged += MapView_DrawStatusChanged;
            _mapViewModel = new MapViewModel();
            _mapViewModel.PropertyChanged += MapViewModel_PropertyChanged;
            _mapView.GeoViewTapped += MapView_GeoViewTapped;
            
            // on appelle la méthode de chargement de la map (provenant du model)
            await _mapViewModel.LoadAsync();
            // la property map du model est màj donc on positionne la property map de la View
            _mapView.Map = _mapViewModel.Map;

        }


        /// <summary>
        /// Gestion du tap surla map pour l'operation d'identification (info attributaires) des incidents
        /// </summary>
        /// <param name="sender"></param> 
        /// <param name="args"></param>

        private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs args)
        {            
                // on envoie les paramètres de requête au model
                var tolerance = 50 * _mapView.UnitsPerPixel;
                var incident = await _mapViewModel.findIncident(args.Location, tolerance);
                if (incident == null)
                    return;

                // si on a un résultat on l'affiche dans l'UI sous forme d'un nouvel element d'interface défini dans Popup.axml et on le remplie
                var layoutInflater = this.LayoutInflater;
                var popupView = layoutInflater.Inflate(Resource.Layout.Popup, null);

                var popupWindow = new PopupWindow(popupView, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                popupWindow.Focusable = true;

                var volumeLabel = popupView.FindViewById<TextView>(Resource.Id.VolValue);
                var volValue = incident.Attributes["Volume"];
                volumeLabel.Text = volValue.ToString();
            
                // définition du bouton Annuler et de son handler
                var cancelButton = popupView.FindViewById(Resource.Id.CancelButton);
                cancelButton.Click += (o, a) =>
                {
                    popupWindow.Dismiss();
                };

                // définition de bouton OK et de son handler
                var saveButton = popupView.FindViewById(Resource.Id.OkButton);
                saveButton.Click += async (o, a) =>
                {
                    try
                    {
                        incident.Attributes["Volume"] = Convert.ToInt32(volumeLabel.Text);
                        popupWindow.Dismiss();
                        await _mapViewModel.SaveEdit(incident);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    }

                };
                // affichage de la popup
                popupWindow.ShowAtLocation(_mapView, GravityFlags.CenterVertical | GravityFlags.CenterHorizontal, 0, 0);            
        }


        //private void VolumeLabel_Click(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}


        /// <summary>
        /// Gestion du bouton "Déconnecter" Passage en mode déconnecté
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void OfflineBtn_Click(object sender, EventArgs e)
        {            
            try
            {
                await _mapViewModel.TakeMapOffline(_mapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry.Extent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }
        }


        /// <summary>
        /// Gestion du bouton "Syncrhoniser". Synchronisation de la map avec le référentiel online et passage en mode connecté
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SyncBtn_Click(object sender, EventArgs e)
        {            
            try
            {
                // on vérifie si le device est connecté ou pas
                ConnectivityManager cm = (ConnectivityManager) GetSystemService(ConnectivityService);
                NetworkInfo activeConnection = cm.ActiveNetworkInfo;
                bool isOnline = (activeConnection != null) && activeConnection.IsConnected;
                if (isOnline) // si le device est connecté on peut syncrhoniser
                {
                    var geom = _mapView.VisibleArea;
                    await _mapViewModel.SyncData();
                    await _mapView.SetViewpointGeometryAsync(geom);                    
                }
                else
                {
                    _statusText.Text = "Aucune connexion réseau";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }
        }

        /// <summary>
        /// Affichage du statut de 'dessin' de la map (pour test)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapView_DrawStatusChanged(object sender, DrawStatus e)
        {
            /*          System.Diagnostics.Debug.WriteLine("DrawStatus :" + e.ToString());
					  if (e.ToString() == "InProgress")
					  {
						  RunOnUiThread(() =>
						  {
							  _statusText.Text = "rafraichissement de la map en cours";
							  _busyIndicator.Visibility = ViewStates.Visible;
						  });

					  }
					  */
            if (e.ToString() == "Completed")
            {
                RunOnUiThread(() =>
                {
                    //     _statusText.Text = "";
                    //    _busyIndicator.Visibility = ViewStates.Gone;
                    if (!_mapViewModel.IsOffline)
                    {
                        _offlineBtn.Visibility = ViewStates.Visible;
                    }

                });

            }


        }


        /// <summary>
        /// PropertyChanged handler pour affichage d'info sur l'UI lorsque des properties du model changent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //   throw new NotImplementedException();
            if (e.PropertyName == "IsBusy")
            {
                RunOnUiThread(() =>
                {
                    if (_mapViewModel.IsBusy )
                        _busyIndicator.Visibility = ViewStates.Visible;
                    else { _busyIndicator.Visibility = ViewStates.Gone; }
                });

            }
            if (e.PropertyName == "Status")
            {
                RunOnUiThread(() =>
                {
                    _statusText.Text = _mapViewModel.Status;
                });
                //_statusText.Text = _mapViewModel.Status;
            }
            if (e.PropertyName == "IsOffline")
            {
                if (_mapViewModel.IsOffline)
                {
                    RunOnUiThread(() =>
                    {
                        _offlineBtn.Visibility = ViewStates.Gone;
                        _syncBtn.Visibility = ViewStates.Visible;
                    });
                }else
                {
                    RunOnUiThread(() =>
                    {
                        _offlineBtn.Visibility = ViewStates.Visible;
                        _syncBtn.Visibility = ViewStates.Gone;
                    });
                }
            }
            if (e.PropertyName == "Map")
            {
                _mapView.Map = _mapViewModel.Map;
            }

        }
    }
}


