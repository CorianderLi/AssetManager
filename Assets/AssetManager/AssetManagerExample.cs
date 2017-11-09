using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UAsset;
using UnityEngine;
using UnityEngine.UI;

public class AssetManagerExample : MonoBehaviour
{
	Bundle[] bundles = new Bundle[0];

	public Transform imageRoot;

	IEnumerator Start ()
	{  
		yield return AssetManager.Initialize ();  
		var manifest = AssetManifest.Instance;
		bundles = manifest.Bundles;
	}

	IEnumerator _LoadAssets ()
	{ 
		List<AssetRequest> requests = new List<AssetRequest> ();
		foreach (var item in bundles) { 
			foreach (var path in item.assets) {
				for (int i = 0; i < 3; i++) {
					requests.Add (AssetManager.Instance.LoadAssetAsync<Object> (path));  
				}
			}
		}

		foreach (var req in requests) {
			yield return req; 
			var asset = req.asset; 
			if (asset is AudioClip) {
				AudioSource.PlayClipAtPoint (asset as AudioClip, Vector3.zero); 
			} else if (asset is Texture) {
				GameObject go = new GameObject (asset.name, typeof(RawImage));
				go.transform.SetParent (imageRoot, false);
				var image = go.GetComponent<RawImage> ();
				image.texture = asset as Texture; 
			}  
		}  
	}

	void UnloadAssets (Bundle bundle)
	{ 
		foreach (var item in bundle.assets) {
			AssetManager.Instance.UnloadAsset (item); 
		}
	}

	IEnumerator LoadABAsync ()
	{
		List<AssetBundleLoadAssetOperation> loading = new List<AssetBundleLoadAssetOperation> (); 
		foreach (var item in bundles) {
			foreach (var path in item.assets) {
				for (int i = 0; i < 3; i++) {
					var req = AssetBundleManager.LoadAssetAsync (item.name, System.IO.Path.GetFileName (path), typeof(Object));
					loading.Add (req); 
				}
			}
		}

		foreach (var req in loading) {
			yield return req;
			if (req != null) {
				yield return req;
				var asset = req.GetAsset<Object> (); 
				if (asset is AudioClip) {
					AudioSource.PlayClipAtPoint (asset as AudioClip, Vector3.zero); 
				} else if (asset is Texture) {
					GameObject go = new GameObject (asset.name, typeof(RawImage));
					go.transform.SetParent (imageRoot, false);
					var image = go.GetComponent<RawImage> ();
					image.texture = asset as Texture; 
				}  
			}
		} 
	}

	public void LoadAssets ()
	{
		StartCoroutine (_LoadAssets ());
	}

	public void UnloadAssets ()
	{
		foreach (var item in bundles) {
			UnloadAssets (item);
		}
	}

	public void UnloadAB ()
	{
		foreach (var item in bundles) {
			foreach (var path in item.assets) {
				AssetBundleManager.UnloadAssetBundle (item.name);
			}
		}
	}

	public void LoadAB ()
	{
		StartCoroutine (LoadABAsync ());
	}
}
