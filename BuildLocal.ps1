$env:Solution_Name = "Valcoin.sln"
$env:Solution_Name = "Valcoin"
$env:Configuration = "Debug"
$env:Platform = "x86"
$env:Appx_Package_Build_Mode = "SideloadOnly"
$env:Appx_Bundle = "Never"
$env:CertificatePath = Resolve-Path .\Valcoin\Valcoin_TemporaryKey.pfx
$env:Appx_Package_Dir = "Packages\"

dotnet msbuild $env:Solution_Name /p:Configuration=$env:Configuration /p:Platform=$env:Platform /p:UapAppxPackageBuildMode=$env:Appx_Package_Build_Mode /p:AppxBundle=$env:Appx_Bundle /p:PackageCertificateKeyFile="$(Split-Path $env:CertificatePath -Leaf)" /p:AppxPackageDir="$env:Appx_Package_Dir" /p:GenerateAppxPackageOnBuild=true