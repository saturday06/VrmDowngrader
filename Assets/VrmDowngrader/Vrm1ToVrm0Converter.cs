using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using UniVRM10;
using VRM;
using VRMShaders;
using VRMShaders.VRM10.MToon10.Runtime;

public class Vrm1ToVrm0Converter
{
    /// <summary>
    /// VRM1のバイト列をVRM0のバイト列に変換します。
    /// 中間リソースの解放は別途Resources.UnloadUnusedAssets()を呼び出して行うものとする。
    /// </summary>
    /// <param name="vrm1Bytes">VRM1のバイト列</param>
    /// <returns>VRM0のバイト列</returns>
    public static async Task<byte[]> Convert(byte[] vrm1Bytes)
    {
        Debug.Log("開始");
        var vrm10Instance = await Vrm10.LoadBytesAsync(
            vrm1Bytes,
            canLoadVrm0X: false,
            showMeshes: true
        );
        if (vrm10Instance == null)
        {
            throw new Exception("Vrm10.LoadBytesAsync => null");
        }

        Debug.Log("インポートはうまくいきました");

        Debug.Log("VRM1のコンポーネントをVRM0で置換していきます");

        var allSharedMaterials = vrm10Instance.gameObject
            .GetComponentsInChildren<Renderer>()
            .SelectMany(renderer => renderer.sharedMaterials)
            .Distinct()
            .ToArray();

        // https://github.com/vrm-c/UniVRM/blob/7e052b19b3c0b4cd02e63159fc37db820729554e/Assets/VRM10/Runtime/Migration/MigrationVrmMeta.cs
        var vrm0Meta = ConvertMeta(vrm10Instance.Vrm.Meta);
        var vrm0MetaComponent = vrm10Instance.gameObject.AddComponent<VRMMeta>();
        vrm0MetaComponent.Meta = vrm0Meta;

        var vrm0BlendShapeProxyComponent =
            vrm10Instance.gameObject.AddComponent<VRMBlendShapeProxy>();
        vrm0BlendShapeProxyComponent.BlendShapeAvatar = ConvertExpression(
            vrm10Instance.Vrm.Expression,
            allSharedMaterials
        );

        var vrm0FirstPersonComponent = vrm10Instance.gameObject.AddComponent<VRMFirstPerson>();
        var vrm0LookAtHeadComponent = vrm10Instance.gameObject.AddComponent<VRMLookAtHead>();
        ConvertFirstPersonLookAt(
            vrm10Instance.Vrm.FirstPerson,
            vrm10Instance.Vrm.LookAt,
            vrm0FirstPersonComponent,
            vrm0LookAtHeadComponent
        );

        Debug.Log("エクスポートします");
        var configuration = new GltfExportSettings();
        var textureSerializer = new RuntimeTextureSerializer();
        var exportingGltfData = VRMExporter.Export(
            configuration,
            vrm10Instance.gameObject,
            textureSerializer
        );
        return exportingGltfData.ToGlbBytes();
    }

    private static void ConvertFirstPersonLookAt(
        VRM10ObjectFirstPerson vrm1FirstPerson,
        VRM10ObjectLookAt vrm1LookAt,
        VRMFirstPerson vrm0FirstPerson,
        VRMLookAtHead vrm0LookAt
    )
    {
        vrm0FirstPerson.Reset();
        vrm0FirstPerson.FirstPersonOffset = vrm1LookAt.OffsetFromHead; // TODO: ワールド方向だった気がする
        switch (vrm1LookAt.LookAtType)
        {
            case UniGLTF.Extensions.VRMC_vrm.LookAtType.bone:
                // vrm0LookAt.gameObject.AddComponent<VRMLookAtBoneApplyer>().OnImported(context);
                break;
            case UniGLTF.Extensions.VRMC_vrm.LookAtType.expression:
                // vrm0LookAt.gameObject.AddComponent<VRMLookAtBlendShapeApplyer>().OnImported(context);
                break;
        }
        foreach (var renderer in vrm1FirstPerson.Renderers)
        {
            //
        }
    }

    private static BlendShapeAvatar ConvertExpression(
        VRM10ObjectExpression vrm1Expression,
        Material[] allSharedMaterials
    )
    {
        var vrm0BlendShapeAvatar = ScriptableObject.CreateInstance<BlendShapeAvatar>();
        var vrm1PresetToVrm0Preset = new Dictionary<ExpressionPreset, BlendShapePreset>
        {
            { ExpressionPreset.happy, BlendShapePreset.Joy },
            { ExpressionPreset.angry, BlendShapePreset.Angry },
            { ExpressionPreset.sad, BlendShapePreset.Sorrow },
            { ExpressionPreset.relaxed, BlendShapePreset.Fun },
            { ExpressionPreset.surprised, BlendShapePreset.Unknown },
            { ExpressionPreset.aa, BlendShapePreset.A },
            { ExpressionPreset.ih, BlendShapePreset.I },
            { ExpressionPreset.ou, BlendShapePreset.U },
            { ExpressionPreset.ee, BlendShapePreset.E },
            { ExpressionPreset.oh, BlendShapePreset.O },
            { ExpressionPreset.blink, BlendShapePreset.Blink },
            { ExpressionPreset.blinkLeft, BlendShapePreset.Blink_L },
            { ExpressionPreset.blinkRight, BlendShapePreset.Blink_R },
            { ExpressionPreset.lookUp, BlendShapePreset.LookUp },
            { ExpressionPreset.lookDown, BlendShapePreset.LookDown },
            { ExpressionPreset.lookLeft, BlendShapePreset.LookLeft },
            { ExpressionPreset.lookRight, BlendShapePreset.LookRight },
            { ExpressionPreset.neutral, BlendShapePreset.Neutral },
        };

        foreach (var (vrm1Preset, vrm1Clip) in vrm1Expression.Clips)
        {
            if (!vrm1PresetToVrm0Preset.TryGetValue(vrm1Preset, out var vrm0Preset))
            {
                vrm0Preset = BlendShapePreset.Unknown;
            }

            var vrm0BlendShapeClip = ScriptableObject.CreateInstance<BlendShapeClip>();
            vrm0BlendShapeClip.Preset = vrm0Preset;
            vrm0BlendShapeClip.BlendShapeName = vrm1Preset.ToString();
            vrm0BlendShapeClip.IsBinary = vrm1Clip.IsBinary;

            vrm0BlendShapeClip.Values = vrm1Clip.MorphTargetBindings
                .Select(
                    morphTargetBinding =>
                        new BlendShapeBinding
                        {
                            RelativePath = morphTargetBinding.RelativePath,
                            Index = morphTargetBinding.Index,
                            Weight = morphTargetBinding.Weight,
                        }
                )
                .ToArray();

            var mtoon0ColorValueNames = new Dictionary<MaterialColorType, string>
            {
                { MaterialColorType.color, "_Color" },
                { MaterialColorType.emissionColor, "_EmissionColor" },
                { MaterialColorType.shadeColor, "_ShadeColor" },
                { MaterialColorType.rimColor, "_RimColor" },
                { MaterialColorType.outlineColor, "_OutlineColor" },
            };

            var gltfColorValueNames = new Dictionary<MaterialColorType, string>
            {
                { MaterialColorType.color, "_Color" },
                { MaterialColorType.emissionColor, "_EmissionColor" },
            };

            var vrm0MaterialColorBindings = vrm1Clip.MaterialColorBindings
                .Select(materialColorBinding =>
                {
                    var material = allSharedMaterials.FirstOrDefault(
                        material => material.name == materialColorBinding.MaterialName
                    );
                    var colorValueNames =
                        material?.shader.name == MToon10Meta.UnityShaderName
                            ? mtoon0ColorValueNames
                            : gltfColorValueNames;
                    colorValueNames.TryGetValue(materialColorBinding.BindType, out var valueName);
                    if (valueName == null)
                    {
                        return null;
                    }
                    return new MaterialValueBinding?(
                        new MaterialValueBinding
                        {
                            MaterialName = materialColorBinding.MaterialName,
                            ValueName = valueName,
                            TargetValue = materialColorBinding.TargetValue,
                        }
                    );
                })
                .OfType<MaterialValueBinding>()
                .ToArray();

            var mtoon0UvValueNames = new[]
            {
                "_MainTex_ST",
                "_ShadeTexture_ST",
                "_BumpMap_ST",
                "_ReceiveShadowTexture_ST",
                "_ShadingGradeTexture_ST",
                "_RimTexture_ST",
                "_EmissionMap_ST",
                "_OutlineWidthTexture_ST",
                "_UvAnimMaskTexture_ST",
            };

            var gltfUvValueNames = new[] { "_MainTex_ST", "_BumpMap_ST", "_EmissionMap_ST", };

            var vrm0MaterialUVBindings = vrm1Clip.MaterialUVBindings
                .SelectMany(materialUvBinding =>
                {
                    var material = allSharedMaterials.FirstOrDefault(
                        material => material.name == materialUvBinding.MaterialName
                    );
                    var valueNames =
                        material?.shader.name == MToon10Meta.UnityShaderName
                            ? mtoon0UvValueNames
                            : gltfUvValueNames;
                    return valueNames.Select(
                        valueName =>
                            new MaterialValueBinding
                            {
                                MaterialName = materialUvBinding.MaterialName,
                                ValueName = valueName,
                                TargetValue = materialUvBinding.ScalingOffset,
                            }
                    );
                })
                .ToArray();
            vrm0BlendShapeClip.MaterialValues = vrm0MaterialColorBindings
                .Concat(vrm0MaterialUVBindings)
                .ToArray();
            vrm0BlendShapeAvatar.Clips.Add(vrm0BlendShapeClip);
        }

        return vrm0BlendShapeAvatar;
    }

    private static VRMMetaObject ConvertMeta(VRM10ObjectMeta vrm1Meta)
    {
        // 厳しいほうに倒す

        // ダウングレードで欠落してしまう情報はotherPermissionUrlに入れる。Data URLsしかない気がする。
        var otherPermissionStringBuilder = new StringBuilder();
        if (!vrm1Meta.PoliticalOrReligiousUsage)
        {
            otherPermissionStringBuilder.AppendLine("- Political or religious use is prohibited.");
        }
        if (!vrm1Meta.AntisocialOrHateUsage)
        {
            otherPermissionStringBuilder.AppendLine("- Antisocial or hate use is prohibited.");
        }
        if (!vrm1Meta.Redistribution)
        {
            otherPermissionStringBuilder.AppendLine("- Redistribution is prohibited.");
        }
        switch (vrm1Meta.Modification)
        {
            case ModificationType.prohibited:
                otherPermissionStringBuilder.AppendLine("- Modification is prohibited.");
                break;
            case ModificationType.allowModification:
                otherPermissionStringBuilder.AppendLine(
                    "- Modification is permitted. Redistribution of modified versions is prohibited."
                );
                break;
        }

        var vrm0Meta = ScriptableObject.CreateInstance<VRMMetaObject>();
        vrm0Meta.Title = vrm1Meta.Name;
        vrm0Meta.Author = string.Join(" / ", vrm1Meta.Authors);
        vrm0Meta.Version = vrm1Meta.Version;
        vrm0Meta.Reference = string.Join(" / ", vrm1Meta.References);
        vrm0Meta.Thumbnail = vrm1Meta.Thumbnail;

        vrm0Meta.AllowedUser = vrm1Meta.AvatarPermission switch
        {
            AvatarPermissionType.onlyAuthor => AllowedUser.OnlyAuthor,
            AvatarPermissionType.everyone => AllowedUser.Everyone,
            AvatarPermissionType.onlySeparatelyLicensedPerson
                => AllowedUser.ExplicitlyLicensedPerson,
            _ => AllowedUser.OnlyAuthor,
        };

        switch (vrm1Meta.CommercialUsage)
        {
            case CommercialUsageType.corporation:
                vrm0Meta.CommercialUssage = UssageLicense.Allow;
                break;
            case CommercialUsageType.personalProfit:
                vrm0Meta.CommercialUssage = UssageLicense.Disallow;
                otherPermissionStringBuilder.AppendLine(
                    "- Personal use for commercial purposes is permitted."
                        + " Corporate use for commercial purposes is prohibited."
                );
                break;
            default:
                vrm0Meta.CommercialUssage = UssageLicense.Disallow;
                break;
        }

        vrm0Meta.SexualUssage = vrm1Meta.SexualUsage ? UssageLicense.Allow : UssageLicense.Disallow;
        vrm0Meta.ViolentUssage = vrm1Meta.ViolentUsage
            ? UssageLicense.Allow
            : UssageLicense.Disallow;
        vrm0Meta.ContactInformation = vrm1Meta.ContactInformation;
        vrm0Meta.LicenseType = LicenseType.Other;

        // https://github.com/vrm-c/vrm-specification/blob/c9e782981bef0c22568f49eafa5b2b7ac9f7d07d/specification/VRMC_vrm-1.0/meta.md#licenseurl
        vrm0Meta.OtherLicenseUrl = "https://vrm.dev/licenses/1.0/";

        if (vrm1Meta.CreditNotation == CreditNotationType.required)
        {
            otherPermissionStringBuilder.AppendLine("- Credit notation is required.");
        }
        if (!string.IsNullOrEmpty(vrm1Meta.CopyrightInformation))
        {
            otherPermissionStringBuilder.AppendLine("- Copyright information is as follows");
            otherPermissionStringBuilder.AppendLine(vrm1Meta.CopyrightInformation);
        }
        if (!string.IsNullOrEmpty(vrm1Meta.ThirdPartyLicenses))
        {
            otherPermissionStringBuilder.AppendLine("- Third party licenses are as follows");
            otherPermissionStringBuilder.AppendLine(vrm1Meta.ThirdPartyLicenses);
        }

        if (otherPermissionStringBuilder.Length > 0)
        {
            vrm0Meta.OtherPermissionUrl =
                "data:text/plain;charset=UTF-8,"
                + Uri.EscapeUriString(
                    "The following additional terms and conditions apply\n\n"
                        + otherPermissionStringBuilder.ToString().Replace("\r\n", "\n")
                );
            UnityEngine.Debug.LogFormat(vrm0Meta.OtherPermissionUrl);
        }

        return vrm0Meta;
    }
}
