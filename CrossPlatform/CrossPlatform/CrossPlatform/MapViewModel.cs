using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Portal;
using System.Linq;
using System.Diagnostics;
using Esri.ArcGISRuntime.Geometry;

namespace CrossPlatform
{
    class MapViewModel : INotifyPropertyChanged
    {

        public MapViewModel()
        {
            //Map = new Map(Basemap.CreateTopographic());
            initialize();
        }

        private Map _map;
        IEnumerable<ArcGISPortalItem> _basemaps;
        ArcGISPortalItem _basemapItem;
        private bool _isInitialized;
        
        /// <summary>
        /// Get Map courante
        /// </summary>
        public Map Map
        {
            get { return _map; }
            private set
            {
                _map = value;
                OnPropertyChanged();
            }
        }        
        
        /// <summary>
        /// Get et Set de la liste de basemaps
        /// </summary>
        public IEnumerable<ArcGISPortalItem> BasemapItems
        {
            get { return _basemaps; }
            set
            {
                _basemaps = value;
                OnPropertyChanged();                
            }
        }

        /// <summary>
        /// Get et Set basemap courante (issue du portal item). indique la basemap courante qui est sélectionnée 
        /// </summary>
        public ArcGISPortalItem BasemapItem
        {
            get { return _basemapItem; }
            set
            {
                if (!BasemapItems.Contains(value))
                    throw new ArgumentException("Basemap items must belong to basemaps collection"); // la basemap ne fait pas partie de la liste
                if (_basemapItem != value)
                {
                    _basemapItem = value;
                    OnPropertyChanged();

                    // màj de la basemap de la map courante si une nouvelle basemap est spécifiée
                    if (Map != null)
                        Map.Basemap = new Basemap(_basemapItem);
                }
            }
        }

        /// <summary>
        /// Get et Set de la propriété indiquant si le mapviewmodel est initialisé. (pour la View sache si le model ets OK ou pas car le sopérations sont asynchrones)
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
            set
            {
                _isInitialized = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Implémentation de INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void initialize()
        {
            try
            {
                // initialiser la liste des basemaps
                // obtenir la liste des basemap depuis la basemap gallery d'ArcGIS Online
                var portal = await ArcGISPortal.CreateAsync();
                var groupSearchResult = await portal.SearchGroupsAsync(new SearchParameters(portal.ArcGISPortalInfo.BasemapGalleryGroupQuery));
                var groupID = groupSearchResult.Results.First().Id;
                var resultInfo = await portal.SearchItemsAsync(new SearchParameters($"group:{groupID}"));
                BasemapItems = resultInfo.Results;

                // init de la map avec la basemap courante
                // set la basemap courante = worl topo map
                BasemapItem = BasemapItems.Where(b => b.Title == "Topographic").FirstOrDefault() ?? BasemapItems.First();
                // set the extent
                var extent = new Envelope(234273, 6136412, 236054, 6138468, SpatialReferences.WebMercator);
                // init de la map avec la basemap et l'extent
                Map = new Map(BasemapItem);
                Map.InitialViewpoint = new Viewpoint(extent);
                // avertir la view que le model est maintenant initialisé
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
