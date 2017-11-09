using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundles;

namespace UAsset
{
	public enum AssetLoadState
	{
		Unload,
		Loading,
		Loaded,
		Failed,
	}

	/// <summary>
	/// Asset request.针对编辑器环境使用
	/// </summary>
	public class AssetRequest : IEnumerator
	{
		#region IEnumerator implementation

		public bool MoveNext ()
		{
			return !isDone;
		}

		public void Reset ()
		{
//		throw new System.NotImplementedException ();
		}

		public object Current {
			get {
				return null;
			}
		}

		#endregion

		public string assetPath { get; protected set; }

		public System.Type assetType { get; protected set; }

		public virtual bool isDone { get; private set; }

		public AssetLoadState loadState { get ; protected set; }

		public Object asset  { get; protected set; }

		public System.Action<string, Object> onloaded { get; set; }

		public static AssetRequest Create (string path, System.Type type)
		{
			#if UNITY_EDITOR
			if (AssetBundleManager.SimulateAssetBundleInEditor) {
				return new AssetRequest (path, type); 
			} else {
				return new BundleAssetRequest (path, type);
			}
			#else 
			return new BundleAssetRequest (path, type); 
			#endif 
		}

		public AssetRequest (string path, System.Type type)
		{
			assetPath = path;
			assetType = type; 
			asset = null;
			isDone = false;
			refCount = 0;
			loadState = AssetLoadState.Unload;
		}

		protected virtual void OnLoad ()
		{
			#if UNITY_EDITOR
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath (assetPath, assetType);  
			isDone = true;
			#endif
		}

		protected virtual void OnUnload ()
		{
			#if UNITY_EDITOR
			Resources.UnloadAsset (asset);
			#endif
		}

		public void Retain ()
		{
			refCount++;
		}

		public bool Release ()
		{
			refCount--;
			return refCount == 0;
		}

		public void Load ()
		{  
			if (loadState == AssetLoadState.Unload) {
				OnLoad ();  
				loadState = AssetLoadState.Loading;
			} 
		}

		public bool Update ()
		{
			if (isDone) { 
				loadState = (asset == null ? AssetLoadState.Failed : AssetLoadState.Loaded); 
				return false;
			} 
			return true;
		}

		public void Unload ()
		{  
			OnUnload ();  
			asset = null; 
		}

		int refCount;
	}

	/// <summary>
	/// Bundle asset request.针对 AssetBundle 中的资源
	/// </summary>
	public class BundleAssetRequest : AssetRequest
	{
		public string assetBundleName { get; private set; }

		public string assetName { get; private set; }

		public AssetBundleLoadAssetOperation request { get; private set; }

		public BundleAssetRequest (string path, System.Type type) : base (path, type)
		{ 
			assetBundleName = AssetManifest.Instance.GetAssetBundleName (assetPath);
			assetName = System.IO.Path.GetFileName (assetPath); 
			request = null;
		}

		public override bool isDone {
			get {
				if (loadState == AssetLoadState.Unload) {
					return false;
				}

				if (request == null) {
					return true;
				} 

				if (request.IsDone ()) { 
					asset = request.GetAsset<Object> (); 
					if (asset is AudioClip) {
						var clip = asset as AudioClip;
						if (clip.loadState == AudioDataLoadState.Unloaded) {
							clip.LoadAudioData ();
						}
					} 
					return true;
				} 
				return false; 
			} 
		}


		protected override void OnLoad ()
		{
			request = AssetBundleManager.LoadAssetAsync (assetBundleName, assetName, assetType);
		}


		protected override void OnUnload ()
		{
			request = null;  
			AssetBundleManager.UnloadAssetBundle (assetBundleName);
		}
	}

	/// <summary>
	/// Resources asset request.针对 Resources 中的资源
	/// </summary>
	public class ResourcesAssetRequest : AssetRequest
	{
		public ResourcesAssetRequest (string path, System.Type type) : base (path, type)
		{
		}
	}
}
