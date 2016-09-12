using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace CrossPlatform
{
	public partial class Page1 : ContentPage
	{
        MapViewModel _mapViewModel;

		public Page1 ()
		{
			InitializeComponent ();
            _mapViewModel = new MapViewModel();
            _mapViewModel.PropertyChanged += _mapViewModel_PropertyChanged;
            BindingContext = _mapViewModel;
            
		}

        private void _mapViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // verif si le viewmodel est initialisé
            if (_mapViewModel.IsInitialized && e.PropertyName == nameof(MapViewModel.IsInitialized))
            {
                // viewmodel ok, init l'UI en ajoutant les noms des basemaps à la liste dans l'UI
                foreach(var basemap in _mapViewModel.BasemapItems)
                {
                    BasemapPicker.Items.Add(basemap.Title);
                }

                // on specifie quel item est selectionné en fonction de la basemap du viewmodel
                BasemapPicker.SelectedIndex = _mapViewModel.BasemapItems.ToList().IndexOf(_mapViewModel.BasemapItem);

                // récup de la map du viewmodel
                this.MapView.Map = _mapViewModel.Map;

                // init de la liste de basemaps dans l'UI
                
            }
        }

        private void BasemapPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // verif si la map est chargée
            if (_mapViewModel.Map.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
                return;

            // mise à jour du viewmodel avec la basemap sélectionnée
            _mapViewModel.BasemapItem = _mapViewModel.BasemapItems.ElementAt(BasemapPicker.SelectedIndex);
        }
    }
}
