using ActiveLogin.Authentication.BankId.Core.Qr;

using QRCoder;

namespace ActiveLoginDemos.ConsoleFlowSample;


public class QrCoderAsciiBankIdQrCodeGenerator : IBankIdQrCodeGenerator
{
    private const int QrPixelsPerModule = 1;
    private const QRCodeGenerator.ECCLevel QrEccLevel = QRCodeGenerator.ECCLevel.L;


    public string GenerateQrCodeAsBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QrEccLevel);

        using var asciiQrCode = new AsciiQRCode(qrCodeData);
        var asciiQrCodeText = asciiQrCode.GetGraphic(
            QrPixelsPerModule,
            drawQuietZones: true,
            darkColorString: "*",
            whiteSpaceString: " " //██
        );
        var asciiQrCodeBytes = System.Text.Encoding.UTF8.GetBytes(asciiQrCodeText);
        var base64QrCode = Convert.ToBase64String(asciiQrCodeBytes);

        return base64QrCode;
    }
}
