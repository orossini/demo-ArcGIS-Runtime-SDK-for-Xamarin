using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.ArcGISServices;
using System.Linq;
using Esri.ArcGISRuntime.Symbology;
using System.Drawing;

namespace demoOffline
{
    public class MapViewModel : INotifyPropertyChanged
    {
        // map ReseauEauDemoGrenevillEnBeauce
        string _mapID = "1c3a4d768c4e458182177fb500a04617";       
        
        // URL du servuce pour extraction (mise en cache local) des couches métiers du réseau d'eau
        private const string BASE_URL = "http://services.arcgis.com/d3voDfTFbHOCRwVR/arcgis/rest/services/ReseauxEauDemoGrenevilleEnBeauce/FeatureServer";
        
        // nom du répertoire pour le stockage des données locales
        private const string GDB_FOLDER = "ReseauEauDemo";

        // nom de la gdb locale pour le stockage des données métier en local
        private const string GDB_NAME = "ReseauEauDemo.geodatabase";

        // URL de la basemap pour l'extraction du cache et copie en local (création d'un tiled package)
        private const string ONLINE_BASEMAP_URL = "http://tiledbasemaps.arcgis.com/arcgis/rest/services/World_Topo_Map/MapServer";

        // nom du tpk (Tiled Package) pour la basemap en local
        private const string TPK_NAME = "offlineBaseMap.tpk";

        private Geodatabase _geodatabase;
        private string _gdbPath;

        private Map _map;

        /// <summary>
        /// Get et Set la Map courante
        /// </summary>
        public Map Map
        {
            get { return _map; }
            set
            {
                _map = value;
                OnPropertyChanged();
            }
        }

        private bool _isBusy;
        private bool _isOffline;

        /// <summary>
        /// Get et Set propriété indiquant si une opération est en cours afin d'afficher le spinner dans l'UI
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private string _status;

        /// <summary>
        /// Get et Set de la propriété Status indiquant des messages (opération en cours) dans l'UI
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get et Set de la propriété IsOffline indiquant si l'app est en mode Offline (data locale) ou online (data online)
        /// </summary>
        public bool IsOffline
        {
            get { return _isOffline; }
            set
            {
                _isOffline = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get et Set de la prorpiété GeoDatabase = gdb locale
        /// </summary>
        public Geodatabase GeoDatabase
        {
            get { return _geodatabase; }
            set
            {
                _geodatabase = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get et Set de la propriété GdbPath = chemin d'accès à la gdb locale
        /// </summary>
        public string GdbPath
        {
            get { return _gdbPath; }
            set
            {
                _gdbPath = value;
                OnPropertyChanged();
            }
        }

        public MapViewModel()
        {            

        }

        /// <summary>
        /// Implémentation de INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

         
        /// <summary>
        /// chargement de la map provenant du portail ArcGIS (en fonction d'un mapID)
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            try
            {                
                setBusyState(busy: true, status: "Initialisation de la carte");
                //authentification
                var credentials = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri("http://esrifrance.maps.arcgis.com/sharing/rest/"), "login", "password");
                AuthenticationManager.Current.AddCredential(credentials);
                // initialisation de la map
                Map = new Map(new Uri($"http://esrifrance.maps.arcgis.com/home/item.html?id={_mapID}"));
                setBusyState(busy: false, status: "Carte initialisée");
            }
            catch (Exception ex)
            {
                setBusyState(busy: false, status: "Erreur de chargement de la carte");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }


        /// <summary>
        /// Affichage d'info dans l'UI
        /// </summary>
        /// <param name="busy"></param> spécifie si une opération est en cours, pemret d'afficher le spinner dans l'UI
        /// <param name="status"></param> message à afficher dans l'UI
        public void setBusyState(bool busy, string status)
        {
            IsBusy = busy;
            Status = status;
        }


        /// <summary>
        /// Mise en mode offline de la basemap
        /// </summary>
        /// <param name="extent"></param> étendue à traiter (à mettre en cahche local)
        /// <returns></returns>       
        public async Task<string> TakeBasemapOffline(Envelope extent)
        {
            try
            {
                var extractTask = new ExportTileCacheTask(new Uri(ONLINE_BASEMAP_URL));
                var tileService = await ArcGISMapServiceInfo.CreateAsync(new Uri(ONLINE_BASEMAP_URL), null);
                int arrayLength = tileService.TileInfo.LevelsOfDetail.ToArray().Length;
                double minScale = tileService.TileInfo.LevelsOfDetail.ToArray()[arrayLength - 10].Scale;
                double maxScale = tileService.TileInfo.LevelsOfDetail.ToArray()[arrayLength - 5].Scale;

                ExportTileCacheParameters tileParameters = await extractTask.CreateExportTileCacheParametersAsync(extent, minScale, maxScale);

                var _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = _folderPath.ToString();
                folderPath = Path.Combine(folderPath, GDB_FOLDER);
                folderPath = folderPath.Replace(".config/", "");

                //   Debug.WriteLine("[EXPORT BASEMAP STATUS MESSAGE] création du folder en sortie...");
                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, TPK_NAME);

                // Download tile cache
                //var cache = await downloadTileCache(OfflineMapLocation, OfflineMapName, MapViewEnvelope);

                // Download tile cache and export to file
                Job<TileCache> exportJob = extractTask.ExportTileCache(tileParameters, filePath);                

                //Get the result
                TileCache cache = await exportJob.GetResultAsync();
                await cache.LoadAsync();
                
                return filePath;
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                Debug.WriteLine("[EXPORT BASEMAP EXCEPTION MESSAGE]" + errorMessage);
                return null;
            }
        }


        /// <summary>
        /// Mise en mode offline de la baasemap. Cette méthode est légèrement différente de la méthode TakaBasemapOffline. Elle permet aussi de remplacer la basemap de la map courant epar la basemap locale.
        /// </summary>
        /// <param name="extent"></param> étendue à traiter (à mettre en mode Offline)
        /// <returns></returns>
        public async Task TakeBasemapOffline2(Envelope extent)
        {
            try
            {
                var extractTask = new ExportTileCacheTask(new Uri(ONLINE_BASEMAP_URL));
                var tileService = await ArcGISMapServiceInfo.CreateAsync(new Uri(ONLINE_BASEMAP_URL), null);
                int arrayLength = tileService.TileInfo.LevelsOfDetail.ToArray().Length;
                // choix des echelles min et max pour la création du cache local
                double minScale = tileService.TileInfo.LevelsOfDetail.ToArray()[arrayLength - 10].Scale;
                double maxScale = tileService.TileInfo.LevelsOfDetail.ToArray()[arrayLength - 5].Scale;

                ExportTileCacheParameters tileParameters = await extractTask.CreateExportTileCacheParametersAsync(extent, minScale, maxScale);

                var _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = _folderPath.ToString();
                folderPath = Path.Combine(folderPath, GDB_FOLDER);
                folderPath = folderPath.Replace(".config/", "");

                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, TPK_NAME);

                // Download tile cache and export to file
                Job<TileCache> exportJob = extractTask.ExportTileCache(tileParameters, filePath);

                //Get the result
                TileCache cache = await exportJob.GetResultAsync();
                await cache.LoadAsync();

                // si le chemin est OK alors on créée la couche et on remplace la basemap online par la basemap offline dans la map
                if (filePath != null)
                {
                    TileCache tileCache = new TileCache(filePath);
                    await tileCache.LoadAsync();

                    // creation de la couche
                    ArcGISTiledLayer layer = new ArcGISTiledLayer(tileCache);

                    // Create basemap
                    Basemap basemap = new Basemap(layer);
                    Map.Basemap = basemap;
                    //        Debug.WriteLine("LA BASEMAP EST OFFLINE");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                Debug.WriteLine("[EXPORT BASEMAP EXCEPTION MESSAGE]" + errorMessage);
                //  return null;
            }
        }


        public async Task<string> TakeOpLayerOffline(Envelope extent)
        {
            try
            {
                /* 
                 // METHOD 1 : utiliser ArcGISFeatureServiceInfo pour créer GeodatabaseSyncTask
                 //Generate the token options and credential
                 var optionscred = new Esri.ArcGISRuntime.Security.GenerateTokenOptions()
                 {
                     Referer = new Uri(BASE_URL)
                 };
                 var cred = await Esri.ArcGISRuntime.Security.AuthenticationManager.Current.GenerateCredentialAsync(
                 new Uri("https://www.arcgis.com/sharing/rest/generatetoken"),
                 "login", "password",
                 optionscred);

                 //Check the credential and add to the identity manager
                 if (cred != null)
                     Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);
                 //setBusyState(busy:true,status:"création de GeodatabaseSyncTask...");
                 Debug.WriteLine("GDB GENERATE STATUS MESSAGE] création de GeodatabaseSyncTask...");

                 //Check the credential and add to the identity manager
                 if (cred != null)
                    Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);

                 //Get the feature service and load it
                 ArcGISFeatureServiceInfo arcgisFSInfo = await ArcGISFeatureServiceInfo.CreateAsync(new Uri(BASE_URL));

                 //Create the task and get the default parameters
                 GeodatabaseSyncTask syncTask = new GeodatabaseSyncTask(arcgisFSInfo);      
                 // FIN METHOD 1
                 */


                // METHOD 2 : on utilsie l'Uri pour créer la GeodatabaseSyncTask  
                //Generate the token options and credential
                var optionscred = new Esri.ArcGISRuntime.Security.GenerateTokenOptions()
                {
                    Referer = new Uri(BASE_URL)
                };
                var cred = await Esri.ArcGISRuntime.Security.AuthenticationManager.Current.GenerateCredentialAsync(
                new Uri("https://www.arcgis.com/sharing/rest/generatetoken"),
                "login", "password",
                optionscred);

                //Check the credential and add to the identity manager
                if (cred != null)
                    Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);
                //setBusyState(busy:true,status:"création de GeodatabaseSyncTask...");
                Debug.WriteLine("GDB GENERATE STATUS MESSAGE] création de GeodatabaseSyncTask...");

                //Check the credential and add to the identity manager
                if (cred != null)
                    Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);

                var syncTask = new GeodatabaseSyncTask(new Uri(BASE_URL));

                // fin METHOD 2

                var options = new GenerateGeodatabaseParameters()
                {
                    Extent = extent,
                    ReturnAttachments = true,
                    OutSpatialReference = extent.SpatialReference,
                    SyncModel = SyncModel.Layer
                };
                options.LayerOptions.Add(new GenerateLayerOption(0));
                options.LayerOptions.Add(new GenerateLayerOption(1));
                options.LayerOptions.Add(new GenerateLayerOption(2));
                options.LayerOptions.Add(new GenerateLayerOption(3));
                options.LayerOptions.Add(new GenerateLayerOption(4));
                var _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                //var _folderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments);
                //     string gdbPath = Path.Combine(ConfigurationManager.OfflineDataPath, ConfigurationManager.OfflineMapsFolder + "/" + mapName);
                string folderPath = _folderPath.ToString();
                folderPath = Path.Combine(folderPath, GDB_FOLDER);
                folderPath = folderPath.Replace(".config/", "");

                Debug.WriteLine("[GDB GENERATE STATUS MESSAGE] création du folder en sortie...");
                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, GDB_NAME);

                Debug.WriteLine("[GDB GENERATE STATUS MESSAGE] création de la gdb...");
                //                setBusyState(busy: true, status: "création de la gdb...");
                //Esri.ArcGISRuntime.Tasks.Offline.Job<Task> =
                //Job<Geodatabase> gdbJob = syncTask.GenerateGeodatabase(options, filePath);
                var gdbJob = syncTask.GenerateGeodatabase(options, filePath);

                gdbJob.JobChanged += (object sender, EventArgs evt) =>
                {
                    //setBusyState(busy: true, status: gdbJob.Status.ToString());                  
                    Debug.WriteLine("[GDB GENERATE STATUS MESSAGE] " + gdbJob.Status.ToString());
                    if (gdbJob.Status.ToString() == "Succeeded")
                    {
                        Debug.WriteLine("job status = Succeeded !!!");
                    }
                };

                //Get the result
                var gdb = await gdbJob.GetResultAsync();

                //Check the result
                if (gdb == null)
                {
                    Debug.WriteLine("Download Error", "Unable to download the selected feature data.\n\nError: " + gdbJob.Error.Message);
                    //    setBusyState(busy: false, status: gdbJob.Error.Message);
                    //                    ToastHelpers.ShowError("Download Error", "Unable to download the selected feature data.\n\nError: " + gdbJob.Error.Message, 7);
                }
                else
                {
                    //   setBusyState(busy: false, status: gdbJob.Error.Message);
                    Debug.WriteLine("Successfully downloaded the map data for offline use");
                    IsOffline = true;
                    //   ToastHelpers.ShowSuccess("Feature Data Download, Successfully downloaded the feature data for offline use.");
                    //   GeoDatabase = gdb;
                    GdbPath = filePath;
                }
                return filePath;
            }
            catch (Exception ex)
            {
                setBusyState(busy: false, status: "Erreur lors de l'extraction de la carte");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }


        /// <summary>
        /// Mise en cache local des données métiers (réseau d'eau). Pemret aussi de remplacer les OpLayer de la map courante (qui sont online) par les données lcoale générées.
        /// </summary>
        /// <param name="extent"></param> Etendue des données à traiter 
        /// <returns></returns>
        public async Task TakeOpLayerOffline2(Envelope extent)
        {
            try
            {

                // on utilsie l'Uri pour créer la GeodatabaseSyncTask  
                //Generate the token options and credential
                var optionscred = new Esri.ArcGISRuntime.Security.GenerateTokenOptions()
                {
                    Referer = new Uri(BASE_URL)
                };
                var cred = await Esri.ArcGISRuntime.Security.AuthenticationManager.Current.GenerateCredentialAsync(
                new Uri("https://www.arcgis.com/sharing/rest/generatetoken"),
                "login", "password",
                optionscred);

                //Check the credential and add to the identity manager
                if (cred != null)
                    Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);
                Debug.WriteLine("GDB GENERATE STATUS MESSAGE] création de GeodatabaseSyncTask...");

                //Check the credential and add to the identity manager
                if (cred != null)
                    Esri.ArcGISRuntime.Security.AuthenticationManager.Current.AddCredential(cred);

                var syncTask = new GeodatabaseSyncTask(new Uri(BASE_URL));

                // fin 

                var options = new GenerateGeodatabaseParameters()
                {
                    Extent = extent,
                    ReturnAttachments = true,
                    OutSpatialReference = extent.SpatialReference,
                    SyncModel = SyncModel.Layer
                };
                // on demande les 5 couches
                options.LayerOptions.Add(new GenerateLayerOption(0));
                options.LayerOptions.Add(new GenerateLayerOption(1));
                options.LayerOptions.Add(new GenerateLayerOption(2));
                options.LayerOptions.Add(new GenerateLayerOption(3));
                options.LayerOptions.Add(new GenerateLayerOption(4));
                var _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = _folderPath.ToString();
                folderPath = Path.Combine(folderPath, GDB_FOLDER);
                folderPath = folderPath.Replace(".config/", "");

                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, GDB_NAME);

                var gdbJob = syncTask.GenerateGeodatabase(options, filePath);

                //Get the result
                var gdb = await gdbJob.GetResultAsync();

                //Check the result
                if (gdb == null)
                {
                    Debug.WriteLine("Download Error", "Unable to download the selected feature data.\n\nError: " + gdbJob.Error.Message);
                }
                else
                {
                    //   setBusyState(busy: false, status: gdbJob.Error.Message);
                    Debug.WriteLine("Successfully downloaded the map data for offline use");
                    IsOffline = true;
                }

                // si le chemin est OK, alors on remplace les données online de la map par ces données locales
                if (filePath != null)
                {
                    var _gdb = await Geodatabase.OpenAsync(filePath);
                    GdbPath = filePath;
                    if (_gdb.GeodatabaseFeatureTables.Count == 0)
                        throw new ApplicationException("Downloaded geodatabase has no feature tables.");

                    foreach (var table in gdb.GeodatabaseFeatureTables)
                    {
                        table.UseAdvancedSymbology = true;
                        var flayer = new FeatureLayer()
                        {
                            Name = table.TableName,
                            FeatureTable = table
                        };

                        foreach (var layer in Map.OperationalLayers.OfType<FeatureLayer>())
                        {
                            if (layer.Name == flayer.Name)
                            {
                                if (layer.Name == "Incidents")
                                {
                                    SimpleRenderer pointRenderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10));
                                    flayer.Renderer = pointRenderer;
                                }
                                if (flayer.FeatureTable.GeometryType == GeometryType.Polyline)
                                {
                                    SimpleRenderer lineRenderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2));
                                    flayer.Renderer = lineRenderer;
                                }
                                Map.OperationalLayers.Remove(layer);
                                break;
                            }
                        }
                        Map.OperationalLayers.Add(flayer);

                    }
                }
            }
            catch (Exception ex)
            {
                setBusyState(busy: false, status: "Erreur lors de l'extraction de la carte");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }


        /// <summary>
        /// Mise en cache loclae de la map (basemap + couches métiers). Appelle les méthodes TakeBasemapOffline2 et TakeOpLayerOffline2 sur l'étendue spécifiée
        /// </summary>
        /// <param name="extent"></param> Etendue à traiter
        /// <returns></returns>
        public async Task TakeMapOffline(Envelope extent)
        {
            try
            {
                setBusyState(busy: true, status: "Extraction de la map");
                var task1 = TakeBasemapOffline2(extent);
                var task2 = TakeOpLayerOffline2(extent);
                await Task.WhenAll(task1, task2);
                setBusyState(busy: false, status: "La carte est déconnectée");
            }
            catch (Exception ex)
            {
                setBusyState(busy: false, status: "Erreur lors du passage en mode déconnecté");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }



        /// <summary>
        /// Recherche d'incident en fonction du tap sur écran (= identify)
        /// </summary>
        /// <param name="location"></param> Emplacement du tap en coordonnées de la map (pas en coordonnées écran)
        /// <param name="tolerance"></param> Tolérance d'interogation. Est utilisé pou rle calcul de l('étendue d'interrogation
        /// <returns></returns>

        public async Task<Feature> findIncident(MapPoint location, double tolerance)
        {
            try
            {
                var queryParams = new Esri.ArcGISRuntime.Data.QueryParameters();
                FeatureLayer featureLayer = new FeatureLayer();
                foreach (var layer in Map.OperationalLayers.OfType<FeatureLayer>())
                {
                    if (layer.Name == "Incidents")
                    {
                        Envelope extent = new Envelope(location.X - tolerance, location.Y - tolerance, location.X + tolerance, location.Y + tolerance);
                        queryParams.Geometry = extent;
                        queryParams.ReturnIDsOnly = false;
                        if (IsOffline)
                        {
                            FeatureQueryResult result = await layer.FeatureTable.QueryFeaturesAsync(queryParams);
                            return result.FirstOrDefault();
                        }
                        else
                        {
                            ServiceFeatureTable _featureTable = new ServiceFeatureTable(new Uri("http://services.arcgis.com/d3voDfTFbHOCRwVR/arcgis/rest/services/ReseauxEauDemoGrenevilleEnBeauce/FeatureServer/0"));
                            QueryFeatureOptions options = QueryFeatureOptions.LoadAllFeatures;
                            FeatureQueryResult result = await _featureTable.QueryFeaturesAsync(queryParams, options);
                            var features = result.ToList();
                            if (features.Any())
                            {
                                Feature featResult = features[0];
                                return featResult;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }


        /// <summary>
        /// Sauvegarde des mises à jour
        /// </summary>
        /// <param name="feature"></param> Feature (objet) à mettre à jour = la nouvelle 'version' de cette feature à jour et à enregistrer
        /// <returns></returns>

        public async Task SaveEdit(Feature feature)
        {         
            foreach (var layer in Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (layer.Name == "Incidents")
                {                                      
                    if (IsOffline)
                    {
                        await layer.FeatureTable.UpdateFeatureAsync(feature);
                    }
                    else
                    {
                        var svcFeatTable = new ServiceFeatureTable(new Uri("http://services.arcgis.com/d3voDfTFbHOCRwVR/ArcGIS/rest/services/ReseauxEauDemoGrenevilleEnBeauce/FeatureServer/0"));
                        await svcFeatTable.UpdateFeatureAsync(feature);
                        await svcFeatTable.ApplyEditsAsync();
                    }                                            
                    break;                    
                }
            }
        }

        /// <summary>
        /// Synchro des màj avec le service en ligne. Et permet de repasser en mode 'online', on se reconnecte à la définition de la webmap. après appel de cette méthode, la map courante est 'online'
        /// </summary>
        /// <returns></returns>

        public async Task SyncData()
        {
            foreach (var layer in Map.OperationalLayers.OfType<FeatureLayer>())
            {
                if (GdbPath == null)
                    return;
                _geodatabase = await Geodatabase.OpenAsync(GdbPath);
                GeodatabaseSyncTask _syncTask = new GeodatabaseSyncTask(_geodatabase.Source);

                SyncGeodatabaseParameters _syncParams = await _syncTask.CreateDefaultSyncGeodatabaseParametersAsync(_geodatabase);

                _syncParams.GeodatabaseSyncDirection = SyncDirection.Bidirectional;
                //       _syncParams.RollbackOnFailure = true;
                if (layer.Name == "Incidents")
                {
                    if (layer.FeatureTable is ServiceFeatureTable)
                        return;

                    if (_geodatabase.HasLocalEdits())
                    {
                        setBusyState(busy: true, status: "Synchronisation en cours");
                        SyncGeodatabaseJob syncJob = _syncTask.SyncGeodatabase(_syncParams, _geodatabase);

                        syncJob.JobChanged += (o, a) => {
                            setBusyState(busy: true, status: syncJob.Status.ToString());
                            Debug.WriteLine("[SYNC STATUS MSG]" + syncJob.Status);
                        };
                        var syncResult = await syncJob.GetResultAsync();

                        setBusyState(busy: false, status: "Synchronisation terminée - " + syncResult.ToString());
                    }

                    // on remplace la map par la map en ligne
                    Map = new Map(new Uri($"http://esrifrance.maps.arcgis.com/home/item.html?id={_mapID}"));
                    IsOffline = false;
                }
            }
        }




        




    }
}

