using ActiveLogin.Authentication.BankId.Core.SupportedDevice;

namespace ActiveLoginDemos.ConsoleFlowSample;

public class BankIdSupportedDeviceDetectorUnknown : IBankIdSupportedDeviceDetector
{
    public BankIdSupportedDevice Detect()
    {
        return BankIdSupportedDevice.Unknown;
    }
}
