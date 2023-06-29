using ActiveLogin.Authentication.BankId.Api.CertificatePolicies;
using ActiveLogin.Authentication.BankId.Api.Models;
using ActiveLogin.Authentication.BankId.Core.Flow;
using ActiveLogin.Authentication.BankId.Core.Models;

using Microsoft.Extensions.Hosting;

using Spectre.Console;

namespace ActiveLoginDemos.ConsoleFlowSample;

internal sealed class BankIdDemoHostedService : IHostedService
{
    private readonly IBankIdFlowService _bankIdFlowService;

    public BankIdDemoHostedService(IBankIdFlowService bankIdFlowService)
    {
        _bankIdFlowService = bankIdFlowService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var policies = BankIdCertificatePolicies.GetPoliciesForProductionEnvironment(BankIdCertificatePolicy.MobileBankId);
        var flowOptions = new BankIdFlowOptions(policies, false, true);
        var initializeResult = await _bankIdFlowService.InitializeAuth(flowOptions, "/");
        var launchType = initializeResult.LaunchType as BankIdFlowInitializeLaunchTypeOtherDevice ?? throw new InvalidOperationException();
        var qrStartState = launchType.QrStartState;

        var isCompleted = false;
        CompletionData? completionData = null;

        var imageBytes = Convert.FromBase64String(launchType.QrCodeBase64Encoded);
        var imageAscii = System.Text.Encoding.UTF8.GetString(imageBytes);

        var qrCode = new QrCode(imageAscii);

        var table = new Table().BorderColor(Color.Grey);
        table.AddColumn("Sign in with BankID", column => column.Centered());

        var text = new Text("Init");
        table.AddRow(qrCode.Canvas);
        table.AddEmptyRow();
        table.AddRow(text);

        await AnsiConsole.Live(table)
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                while (!isCompleted)
                {
                    var collectResult = await _bankIdFlowService.Collect(initializeResult.BankIdResponse.OrderRef, 0, flowOptions);
                    var qrResult = _bankIdFlowService.GetQrCodeAsBase64(qrStartState);
                    imageBytes = Convert.FromBase64String(qrResult);
                    imageAscii = System.Text.Encoding.UTF8.GetString(imageBytes);
                    qrCode.SetCanvasPixels(imageAscii);

                    switch (collectResult)
                    {
                        case BankIdFlowCollectResultPending collectResultPending:
                            table.Rows.RemoveAt(2);
                            table.AddRow(new Text(collectResultPending.StatusMessage));
                            break;
                        case BankIdFlowCollectResultComplete collectResultComplete:
                            completionData = collectResultComplete.CompletionData;
                            isCompleted = true;
                            break;
                        default:
                            isCompleted = true;
                            break;
                    }

                    ctx.Refresh();
                    await Task.Delay(1000, cancellationToken);
                }
            });

        if (completionData != null)
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap());
            grid.AddColumn(new GridColumn().PadLeft(2));

            grid.AddRow("[b]Full name[/]", MaskFullName(completionData.User.Name));
            grid.AddRow("[b]Given name[/]", MaskGivenName(completionData.User.GivenName));
            grid.AddRow("[b]Surname[/]", completionData.User.Surname);
            grid.AddRow("[b]Personal Identity Number[/]", MaskIdentityNumber(completionData.User.PersonalIdentityNumber));

            var panel = new Panel(grid).Header($"Welcome {completionData.User.GivenName}", Justify.Center);
            AnsiConsole.Write(panel);
        }
    }

    public string MaskFullName(string fullName)
    {
        var nameParts = fullName.Split(' ');
        var maskedName = nameParts[0];

        for (var i = 1; i < nameParts.Length - 1; i++)
        {
            maskedName += " " + new string('*', nameParts[i].Length);
        }

        if (nameParts.Length > 1)
        {
            maskedName += " " + nameParts[nameParts.Length - 1];
        }

        return maskedName;
    }

    private static string MaskGivenName(string givenName)
    {
        var nameParts = givenName.Split(' ');
        var firstName = nameParts[0];
        for (var i = 1; i < nameParts.Length; i++)
        {
            firstName += " " + new string('*', nameParts[i].Length);
        }
        return firstName;
    }

    private static string MaskIdentityNumber(string identityNumber)
    {
        return identityNumber.Substring(0, identityNumber.Length - 4) + "****";
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
