$version='1.0.1'
dotnet build -c Release   /property:Version=$version
dotnet pack -c Release /property:Version=$version

$ostatniPakiet = (gci .\src\Voyager.Configuration.MountPath\bin\Release\*.nupkg | select -last 1).Name
$sciezka = ".\src\Voyager.Configuration.MountPath\bin\Release\$ostatniPakiet"

dotnet nuget push "$sciezka" -s Voyager-Poland
