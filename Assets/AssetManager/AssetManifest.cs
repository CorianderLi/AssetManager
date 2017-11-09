using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AssetBundles;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UAsset
{
	[System.Serializable]
	public class Bundle
	{
		public string name;
		public string[] assets;
		public int version;

		public bool update { get; set; }

		public Hash128 hash { get; set; }

		public Bundle ()
		{
			update = false;
			version = 0;
		}
	}

	public class BundleVersion
	{
		public string bundle;
		public Hash128 hash;
		public int version;

		public override string ToString ()
		{
			return string.Format ("{0}:{1}:{2}", bundle, hash, version);
		}
	}

	public class AssetManifest : ScriptableObject, ISerializationCallbackReceiver
	{
		#region ISerializationCallbackReceiver implementation

		public void OnBeforeSerialize ()
		{   
			List<Bundle> list = new List<Bundle> ();
			foreach (var item in map) {
				Bundle bundle = new Bundle ();
				bundle.name = item.Key;
				bundle.assets = item.Value.assets;
				list.Add (bundle);
			}  
			bundles = list.ToArray (); 
		}

		public void OnAfterDeserialize ()
		{ 
			map.Clear (); 
			paths.Clear (); 
			for (int i = 0; i < bundles.Length; i++) {
				var bundle = bundles [i];
				map [bundle.name] = bundle;
				for (int j = 0; j < bundle.assets.Length; j++) {
					var assetPath = bundle.assets [j];
					paths [assetPath] = bundle.name;
				}
			}  
		}

		#endregion

		[SerializeField] Bundle[] bundles = new Bundle[0];

		public Bundle[] Bundles {
			get {
				return bundles;
			}
		}

		Dictionary<string, string> paths = new Dictionary<string, string> ();
		Dictionary<string, Bundle> map = new Dictionary<string, Bundle> ();

		public string GetAssetBundleName (string assetPath)
		{
			string assetBundleName = null;
			if (!paths.TryGetValue (assetPath, out assetBundleName)) {
				Debug.LogError ("failed to GetAssetBundleName: " + assetPath);
			}
			return assetBundleName;
		}

		public const string ASSETBUNDLE_NAME = "assetmanifest";
		public const string ASSET_NAME = "AssetManifest.asset";

		static AssetManifest instance = null;

		public static AssetManifest Instance {
			get {
				#if UNITY_EDITOR
				if (instance == null) {  
					instance = AssetDatabase.LoadAssetAtPath<AssetManifest> (EXPORT_PATH + ASSET_NAME);
					if (instance == null) {
						instance = CreateInstance<AssetManifest> ();
						AssetDatabase.CreateAsset (instance, EXPORT_PATH + ASSET_NAME); 
						var importer = AssetImporter.GetAtPath (EXPORT_PATH + ASSET_NAME);
						if (importer != null) {
							importer.assetBundleName = ASSETBUNDLE_NAME;
						}
					}
				}
				#endif
				return instance;
			} 
		}

		static Dictionary<string, bool> bundleStates = new Dictionary<string, bool> ();

		public static IEnumerator Initialize ()
		{ 
			AssetBundleManager.SetDevelopmentAssetBundleServer ();
			var oper = AssetBundleManager.Initialize ();
			if (oper != null) {
				yield return oper; 
				var req = AssetBundleManager.LoadAssetAsync (ASSETBUNDLE_NAME, ASSET_NAME, typeof(AssetManifest));
				if (req != null) {
					yield return req;
					instance = req.GetAsset<AssetManifest> ();
				}
			}  
		}

		public static string[] GetNewBundles (AssetBundleManifest remote, AssetBundleManifest local)
		{  
			List<string> newBundles = new List<string> ();
			if (remote != null) {
				var localBundles = local.GetAllAssetBundles (); 
				var remoteBundles = remote.GetAllAssetBundles ();
				foreach (var item in remoteBundles) { 
					bool versionChanged = true;
					if (System.Array.Exists<string> (localBundles, o => { return o.Equals (item); })) {
						if (remote.GetAssetBundleHash (item).Equals (local.GetAssetBundleHash (item))) {
							versionChanged = false;
						}
					} 
					if (versionChanged) {
						newBundles.Add (item);
					}
				} 
			}
			return newBundles.ToArray ();
		}

		//		static string AssetBundles_AssetBundleManager_overrideBaseDownloadingURL (string bundleName)
		//		{
		//			if (instance.map [bundleName].update) {
		//				return  System.IO.Path.Combine (Application.streamingAssetsPath, bundleName);
		//			}
		//			return AssetBundleManager.BaseDownloadingURL + bundleName;
		//		}

		#if UNITY_EDITOR
		const string EXPORT_PATH = "Assets/";

		static public string CreateAssetBundleDirectory ()
		{
			// Choose the output path according to the build target.
			string outputPath = Path.Combine (Utility.AssetBundlesOutputPath, Utility.GetPlatformName ());
			if (!Directory.Exists (outputPath))
				Directory.CreateDirectory (outputPath);

			return outputPath;
		}

		public static AssetBundleManifest BuildAssetBundles ()
		{
			// Choose the output path according to the build target.
			string outputPath = CreateAssetBundleDirectory ();

			var options = BuildAssetBundleOptions.None;

			bool shouldCheckODR = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS; 
#if UNITY_TVOS
			shouldCheckODR |= EditorUserBuildSettings.activeBuildTarget == BuildTarget.tvOS;
#endif
			if (shouldCheckODR) {
#if ENABLE_IOS_ON_DEMAND_RESOURCES
				if (PlayerSettings.iOS.useOnDemandResources)
					options |= BuildAssetBundleOptions.UncompressedAssetBundle;
#endif
#if ENABLE_IOS_APP_SLICING
					options |= BuildAssetBundleOptions.UncompressedAssetBundle;
#endif
			}
			return BuildPipeline.BuildAssetBundles (outputPath, options, EditorUserBuildSettings.activeBuildTarget);
		}

		static string versionsFilePath { get { return string.Format ("{0}/version.txt", CreateAssetBundleDirectory ()); } }

		static void LoadVersions (Dictionary<string, BundleVersion> versions)
		{
			if (File.Exists (versionsFilePath)) {
				TextReader reader = new StreamReader (versionsFilePath);
				if (reader != null) {
					string line = null; 
					while ((line = reader.ReadLine ()) != null) {
						string[] fields = line.Split (':');  
						BundleVersion v = new BundleVersion ();
						v.bundle = fields [0];
						v.hash = Hash128.Parse (fields [1]);
						v.version = int.Parse (fields [2]); 
						versions [v.bundle] = v;
					}
					reader.Close ();
				}
			} 
		}

		static void SaveVersions (Dictionary<string, BundleVersion> versions)
		{
			TextWriter writer = new StreamWriter (versionsFilePath);
			if (writer != null) {
				foreach (var item in versions) {
					writer.WriteLine (item.Value.ToString ());
				} 
				writer.Close ();
			}

			EditorUtility.OpenWithDefaultApp (versionsFilePath);
		}

		public void Build ()
		{ 
			var time = System.DateTime.Now.TimeOfDay.TotalSeconds; 
			AssetDatabase.RemoveUnusedAssetBundleNames ();
			var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames ();
			for (int i = 0; i < allAssetBundleNames.Length; i++) {
				var item = allAssetBundleNames [i];
				if (System.IO.Directory.Exists (item)) {
					Debug.LogError (item + " Is Directory.");
				}
			}

			AssetDatabase.RemoveUnusedAssetBundleNames (); 
			allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames ();
			if (allAssetBundleNames.Length > 0) {
				bundles = new Bundle[allAssetBundleNames.Length];
				map.Clear ();
				for (int i = 0; i < allAssetBundleNames.Length; i++) {
					var item = allAssetBundleNames [i];
					bundles [i] = new Bundle ();
					bundles [i].name = item;
					bundles [i].assets = AssetDatabase.GetAssetPathsFromAssetBundle (item); 
					map [item] = bundles [i];
				} 
				EditorUtility.SetDirty (this);
			} 

			var importer = AssetImporter.GetAtPath (EXPORT_PATH + ASSET_NAME);
			if (importer != null) {
				importer.assetBundleName = ASSETBUNDLE_NAME;
			} 

			var elasped = System.DateTime.Now.TimeOfDay.TotalSeconds - time; 
			Dictionary<string, BundleVersion> versions = new Dictionary<string, BundleVersion> ();
			LoadVersions (versions);  
			var assetBundleManifest = BuildAssetBundles ();
			if (assetBundleManifest != null) {
				foreach (var item in allAssetBundleNames) {
					var hash = assetBundleManifest.GetAssetBundleHash (item);
					BundleVersion version = null;
					if (!versions.TryGetValue (item, out version)) {
						version = new BundleVersion ();
						version.bundle = item;
						version.hash = hash;
						version.version = 1;
						versions [item] = version;
					} else {
						if (!version.hash.Equals (hash)) {
							version.hash = hash;
							version.version++;
						}
					} 
				}

				Debug.Log ("assetBundleManifest.GetHashCode: " + assetBundleManifest.GetHashCode ());
			}
			SaveVersions (versions);
			Debug.Log ("[AssetManifest] Build with " + elasped + " seconds.");
		}

		#endif
	}
}
