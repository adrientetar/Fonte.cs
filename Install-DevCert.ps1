[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true, Position=0)][string]$pfxPath,
    [Parameter(Mandatory=$true, Position=1)][string]$password)

Add-Type -AssemblyName System.Security
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import($pfxPath, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]"PersistKeySet")
$store = new-object system.security.cryptography.X509Certificates.X509Store -argumentlist "MY", CurrentUser
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]"ReadWrite")
$store.Add($cert)
$store.Close()