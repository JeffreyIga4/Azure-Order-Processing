function Generate-SasToken {
    param(
        [string]$resourceUri,
        [string]$keyName,
        [string]$key,
        [int]$expiryInSeconds = 3600
    )

    $encodedResourceUri = [System.Web.HttpUtility]::UrlEncode($resourceUri.ToLower())
    $expiry = [int][double]::Parse((Get-Date -UFormat %s)) + $expiryInSeconds
    $stringToSign = "$encodedResourceUri`n$expiry"

    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($key)
    $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($stringToSign))
    $signature = [Convert]::ToBase64String($signatureBytes)
    $encodedSignature = [System.Web.HttpUtility]::UrlEncode($signature)

    return "SharedAccessSignature sr=$encodedResourceUri&sig=$encodedSignature&se=$expiry&skn=$keyName"
}

$uri = "https://orderservicebusjiga.servicebus.windows.net/orderqueue"
$keyName = "RootManageSharedAccessKey"
$keyValue = "YOUR_KEY_HERE"

Generate-SasToken -resourceUri $uri -keyName $keyName -key $keyValue
