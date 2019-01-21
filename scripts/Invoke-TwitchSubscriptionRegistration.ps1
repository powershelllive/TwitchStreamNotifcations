function Invoke-TwitchSubscriptionRegistration {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FunctionSecret,

        [Parameter(DontShow)]
        [string]
        $FunctionUri = 'https://twitchstreamnotifications.azurewebsites.net/api/TwitchSubscriptionRegistration'
        )

    end {
        try {
            $Body = Get-Content -Raw -Path $Path -ErrorAction stop
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        $Params = @{
            Headers = @{
                'x-functions-key' = $FunctionSecret
            }
            Uri = $FunctionUri
            Method = 'POST'
            Body = $Body
            ContentType = 'application/json'
        }
        Invoke-RestMethod @Params
    }
}

$invokeTwitchSubscriptionRegistrationSplat = @{
    Path = 'config/Subscriptions.json'
    FunctionSecret = $env:FunctionCode
}
$Result = Invoke-TwitchSubscriptionRegistration @invokeTwitchSubscriptionRegistrationSplat
' '
' '
' '
' '
' '
'-----------------'
'Requested Subscriptions:'
$Result.RequestSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
' '
'-----------------'
'Current Subscriptions:'
$Result.CurrentSubscriptions.subscription | Format-Table -AutoSize twitchname, twittername, discordname
' '
'-----------------'
'Queued for Subscribe'
$Result.AddSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
' '
'-----------------'
'Queued for Unsubscribe'
$Result.RemoveSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
' '
' '
'-----------------'
'Queued for Renewal'
$Result.RenewSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
' '
' '
