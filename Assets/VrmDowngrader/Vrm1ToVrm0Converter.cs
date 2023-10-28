using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UniVRM10;
using VRM;
using VRMShaders;

public class Vrm1ToVrm0Converter
{
    public static async Task<byte[]> Convert(byte[] vrm1Bytes)
    {
        Debug.Log("開始");
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var vrm10Instance = await Vrm10.LoadBytesAsync(
            vrm1Bytes,
            false,
            showMeshes: true,
            awaitCaller: new ImmediateCaller(),
            ct: cancellationToken
        );
        if (vrm10Instance == null)
        {
            throw new Exception("Vrm10.LoadBytesAsync => null");
        }

        Debug.Log("インポートはうまくいきました");

        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます");
        // https://github.com/vrm-c/UniVRM/blob/7e052b19b3c0b4cd02e63159fc37db820729554e/Assets/VRM10/Runtime/Migration/MigrationVrmMeta.cs
        var vrm0Meta = ScriptableObject.CreateInstance<VRMMetaObject>();
        var vrm1Meta = vrm10Instance.Vrm.Meta;
        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます 1");
        // var vrm0Meta = VRMMeta.
        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます 2");
        vrm0Meta.Title = vrm1Meta.Name;
        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます 3");
        vrm0Meta.Author = string.Join("/ ", vrm1Meta.Authors);
        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます 4");
        vrm0Meta.Version = vrm1Meta.Version;
        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます 5");
        var vrm0MetaComponent = vrm10Instance.gameObject.AddComponent<VRMMeta>();
        vrm0MetaComponent.Meta = vrm0Meta;

        Debug.Log("エクスポートします");
        var configuration = new UniGLTF.GltfExportSettings();
        var textureSerializer = new RuntimeTextureSerializer();
        var exportingGltfData = VRMExporter.Export(
            configuration,
            vrm10Instance.gameObject,
            textureSerializer
        );
        return exportingGltfData.ToGlbBytes();
    }
}
