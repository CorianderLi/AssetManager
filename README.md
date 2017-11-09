# AssetManager #


主要提供了资源相关操作的上层接口, 和资源构建工具, 让用户不再直接使用引擎的底层资源操作接口 Resources 和 AssetBundle. 

资源路径决定了资源是从 Resources 还是 AssetBundle 中加载, 加载前, 首先会对资源路径进行检查:如果路径中包含 "/Resources/", 则内部构建一个 ResourcesAssetRequest 从 Resources 中加载.反之则视作是 AssetBundle 中的资源, 在加载的时候, 内部构建一个 BundleAssetRequest, 然后根据一份 "Manifest" 文件把路径转换为资源的 assetBundleName 和 assetName 后, 再通过 AssetBundleManager 进行资源加载.对 AssetBundle 中的资源, 每个资源的 assetPath 和 assetBundleName 的映射关系都会被记录到一个 "Manifest" 文件中, 构建前可以通过 "Assets/BuildManifest" 命令生成这个文件.为避免阻塞主线程, 所有资源加载都是异步操作, 同时内建了基于引用计数的缓存策略, 确保同一份资源不会被重复加载亦不会轻易卸载. 

**AssetManager 的接口主要包括**  

    Initialize ()
        初始化 "Manifest" 文件, 并实例化资源管理器单例
    LoadAssetAsync ()
        异步加载资源 输入 assetPath and assetType 返回 AssetRequest 
    UnloadAsset ()
        卸载资源 输入 assetPath or asset 返回 空, 所有资源的卸载都是在资源请求操作完成后才执行
    maxLoadCountPerFrame:3
        每帧最大并行加载数量默认3, UWA 推荐 2 - 3

**AssetManager 提供的编辑器构建工具**

    "Build Manifest"
        生成所有 AssetBundle 中的资源的 assetPath 和 assetBundleName 的映射文件
    "Copy Asset Path" 
        把编辑器中的资源路径复制到剪切板

**对于 AssetBundle 的版本更新** 

目前主要考虑了 基于一个 zip 多个 AssetBundle 的补丁包 和 直接下载需要更新的单个 AssetBundle 两种模式.对于一个 zip 多个 AssetBundle 的补丁包, 好处是可以对 AssetBundle 进行二次压缩, 同时每次递增的版本更新资源都是一个整体. 对于后者, 好处是对版本更新的资源不用二次解压, 然后跨版本更新的时候, 可以不用下载当前版本中在过往被重复迭代的资源.但不论是前者还是后者, 版本更新都可以对比打包前后 AssetBundleManifest 来过滤出需要更新的资源, 这个过程可以通过以下接口实现:

1. AssetManifest.GetNewBundles() 输入远程和本地的 AssetBundleManifest, 返回更新的 AssetBundle, 内部主要通过对比 AssetBundle 在 AssetBundleManifest 中的新旧 AssetBundleHash 来识别哪些是新的 AssetBundle 
1. AssetBundleManifest.GetAssetBundleHash 输入 assetBundleName 返回对应的 hash

**对于 AssetBundle 的打包, 在 Unity.5x 中建议把以下官方资料理解透彻之后, 自行编写批量构建规则:**

-  [Assets, Resources and AssetBundles](https://unity3d.com/cn/learn/tutorials/s/best-practices)
-  [assetbundles-and-assetbundle-manager](https://unity3d.com/cn/learn/tutorials/topics/scripting/assetbundles-and-assetbundle-manager)

注: 官方的 AssetBundleManager 默认使用 www 进行加载, AssetManager 中的版本做了一定优化, 直接使用 AssetBundle.LoadFromFileAsync 进行加载(UNITY_5_4_OR_NEWER), 具体参考 AssetBundleLoadFromFileOperation
