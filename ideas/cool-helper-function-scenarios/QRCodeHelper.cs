using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class QRCodeHelper
{
    [HelperFunctionDescription("Generates a QR code URL")]
    public static string GenerateQRCodeUrl(string url, bool isTransparent, int size, string format)
    {
        string transparencyOption = isTransparent ? "_transparent" : "";
        string sizeOption = size > 1 ? $"_{size}" : "";

        return $"https://qrtag.net/api/qr{transparencyOption}{sizeOption}.{format}?url={url}";
    }
}