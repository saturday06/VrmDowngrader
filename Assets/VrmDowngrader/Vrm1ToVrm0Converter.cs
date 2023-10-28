using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UniVRM10;
using VRM;
using VRMShaders;
using UniGLTF.Extensions.VRMC_vrm;
using System.Text;

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
        var vrm0Meta = ConvertMeta(vrm10Instance.Vrm.Meta);
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
            otherPermissionStringBuilder.Insert(
                0,
                "The following additional terms and conditions apply\n\n"
            );
            vrm0Meta.OtherPermissionUrl =
                "data:text/plain;charset=UTF-8,"
                + Uri.EscapeUriString(
                    otherPermissionStringBuilder.ToString().Replace("\r\n", "\n")
                );
            UnityEngine.Debug.LogFormat(vrm0Meta.OtherPermissionUrl);
        }

        return vrm0Meta;
    }
}
