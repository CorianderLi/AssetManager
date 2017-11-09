using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

namespace UAsset
{
	public class AssetManager : MonoBehaviour
	{ 
		private List<AssetRequest> loading = new List<AssetRequest> (); 
		private List<AssetRequest> unloading = new List<AssetRequest> ();
		private Dictionary<string, AssetRequest> requests = new Dictionary<string, AssetRequest> ();
		private Dictionary<Object, AssetRequest> objects = new Dictionary<Object, AssetRequest> ();

		/// <summary>
		/// 每帧最大加载数量 UWA 建议 2-3 个
		/// </summary>
		public int maxLoadCountPerFrame = 3;

		private static AssetManager instance = null;

		public static AssetManager Instance {
			get { 
				return instance;
			}
		}

		public static IEnumerator Initialize ()
		{
			yield return AssetManifest.Initialize ();
			if (instance == null) {
				var go = new GameObject ("AssetManager");
				DontDestroyOnLoad (go);
				instance = go.AddComponent<AssetManager> ();
			} 
		}

		public AssetRequest LoadAssetAsync<T> (string assetPath, System.Action<string, Object> onLoaded = null) where T : Object
		{  
			AssetRequest req = null;
			if (!requests.TryGetValue (assetPath, out req)) {
				req = AssetRequest.Create (assetPath, typeof(T)); 
				requests [assetPath] = req;
				loading.Add (req); 
			}

			req.onloaded += onLoaded;
			if (req.loadState == AssetLoadState.Loaded || req.loadState == AssetLoadState.Failed) {
				if (req.onloaded != null) { 
					req.onloaded.Invoke (req.assetPath, req.asset);
					req.onloaded = null;
				}
			}
			req.Retain ();
			return req;
		}

		public void UnloadAsset (string assetPath)
		{
			AssetRequest req = null;
			if (requests.TryGetValue (assetPath, out req)) {
				if (req.Release ()) {
					unloading.Add (req);
					requests.Remove (assetPath);	
				} 
			}
		}

		public void UnloadAsset (Object assetToUnload)
		{
			if (assetToUnload == null) {
				return;
			} 
			AssetRequest req = null;
			if (objects.TryGetValue (assetToUnload, out req)) {
				UnloadAsset (req.assetPath);
			}
		}

		// Update is called once per frame
		void Update ()
		{
			if (loading.Count > 0) {
				int loadCount = 0;
				for (int i = 0; i < loading.Count; i++) {
					var req = loading [i];
					if (req.loadState == AssetLoadState.Loading) {
						if (!req.Update ()) { 
							if (req.asset != null) {
								objects.Add (req.asset, req);
							} 
							if (req.onloaded != null) {
								req.onloaded.Invoke (req.assetPath, req.asset); 
								req.onloaded = null;
							} 
							loading.RemoveAt (i);
							i--;
						} 
					} else {
						if (req.loadState == AssetLoadState.Unload) {
							if (loadCount < maxLoadCountPerFrame) { 
								req.Load ();  
								loadCount++;
							}
						}

					}
				}
			}  

			if (unloading.Count > 0) {
				for (int i = 0; i < unloading.Count; i++) {
					var item = unloading [i];
					if (item.isDone) {
						if (item.asset != null) {
							objects.Remove (item.asset);
						}
						item.Unload ();
						unloading.RemoveAt (i);
						i--;
					}
				}
			}
		}
	}
}