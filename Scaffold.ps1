[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('InitSolution', 'AddModule', 'AddWorkerHost', 'AddSharedProject')]
    [string]$Command,

    [string]$SolutionName,
    [string]$RootPath = (Get-Location).Path,
    [string]$SolutionRoot,
    [string[]]$Modules,
    [string[]]$SharedProjects,
    [string]$ModuleName,
    [string]$WorkerName,
    [string]$ProjectName,

    [ValidateSet('Minimal', 'Full')]
    [string]$TestProfile = 'Minimal',

    [switch]$WithTests,
    [switch]$RegisterInApiHost,
    [switch]$SkipIfExists,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-WarnMessage {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "WARN: $Message" -ForegroundColor Yellow
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Remove-DirectorySafe {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Write-FileUtf8NoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        Ensure-Directory -Path $parent
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Write-GeneratedFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content,
        [switch]$Overwrite
    )

    if ((Test-Path -LiteralPath $Path) -and (-not $Overwrite)) {
        if ($SkipIfExists) {
            Write-WarnMessage "Skipped existing file: $Path"
            return
        }

        throw "File already exists: $Path"
    }

    Write-FileUtf8NoBom -Path $Path -Content $Content
}

function Assert-NotNullOrWhiteSpace {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$ParameterName
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Parameter '$ParameterName' is required."
    }
}

function Assert-ValidName {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$ParameterName
    )

    if ([string]::IsNullOrWhiteSpace($Name)) {
        throw "Parameter '$ParameterName' is required."
    }

    if ($Name -notmatch '^[A-Za-z][A-Za-z0-9\.]*$') {
        throw "Parameter '$ParameterName' has invalid value '$Name'. Only letters, numbers and dots are allowed, and it must start with a letter."
    }
}

function Get-SolutionRootPath {
    if (-not [string]::IsNullOrWhiteSpace($SolutionRoot)) {
        return $SolutionRoot
    }

    Assert-NotNullOrWhiteSpace -Value $SolutionName -ParameterName 'SolutionName'
    return (Join-Path $RootPath $SolutionName)
}

function Get-SolutionNameFromRoot {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)
    return (Split-Path -Leaf $ResolvedSolutionRoot)
}

function Get-SolutionManifestPath {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $ResolvedSolutionRoot
    return (Join-Path $ResolvedSolutionRoot "$resolvedSolutionName.slnx")
}

function Assert-SolutionExists {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    if (-not (Test-Path -LiteralPath $ResolvedSolutionRoot)) {
        throw "Solution root not found: $ResolvedSolutionRoot"
    }

    $manifestPath = Get-SolutionManifestPath -ResolvedSolutionRoot $ResolvedSolutionRoot
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "Solution manifest (.slnx) not found: $manifestPath"
    }
}

function Get-RelativeProjectPath {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$FullProjectPath
    )

    return ([System.IO.Path]::GetRelativePath($ResolvedSolutionRoot, $FullProjectPath)).Replace('\', '/')
}

function New-RootConfigFiles {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    Write-Step "Writing root config files"

    $globalJson = @'
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
'@
    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot 'global.json') -Content $globalJson -Overwrite:$Force

    $editorConfig = @'
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 2

[*.cs]
indent_size = 4
tab_width = 4

dotnet_language_version = latest_major

csharp_style_namespace_declarations = file_scoped:error
csharp_prefer_braces = true:error
csharp_style_allow_embedded_statements_on_same_line = false:error

dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true
dotnet_style_require_accessibility_modifiers = always:warning

csharp_style_var_for_built_in_types = false:error
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:warning

dotnet_diagnostic.IDE0007.severity = suggestion
dotnet_diagnostic.IDE0008.severity = suggestion

dotnet_naming_rule.interface_prefix_i.severity = error
dotnet_naming_rule.interface_prefix_i.symbols = interfaces
dotnet_naming_rule.interface_prefix_i.style = prefix_i

dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_symbols.interfaces.applicable_accessibilities = *
dotnet_naming_style.prefix_i.required_prefix = I
dotnet_naming_style.prefix_i.capitalization = pascal_case

dotnet_naming_rule.types_pascal.severity = error
dotnet_naming_rule.types_pascal.symbols = types
dotnet_naming_rule.types_pascal.style = pascal

dotnet_naming_symbols.types.applicable_kinds = class, struct, enum, record
dotnet_naming_symbols.types.applicable_accessibilities = *
dotnet_naming_style.pascal.capitalization = pascal_case

dotnet_naming_rule.private_fields_underscore.severity = error
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case

dotnet_diagnostic.IDE0005.severity = suggestion
dotnet_diagnostic.IDE0044.severity = suggestion
dotnet_diagnostic.IDE0032.severity = suggestion
dotnet_diagnostic.IDE0037.severity = suggestion

[**/*Tests/*.cs]
dotnet_style_require_accessibility_modifiers = never
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
dotnet_diagnostic.IDE0007.severity = none
dotnet_diagnostic.IDE0008.severity = none

[*.json]
indent_size = 2

[*.ps1]
indent_size = 2
'@
    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot '.editorconfig') -Content $editorConfig -Overwrite:$Force

    $gitIgnore = @'
### C#
[Bb]in/
[Oo]bj/
.vs/
*.suo
*.user
*.pdb
*.nupkg
**/[Pp]ackages/*
!**/[Pp]ackages/build/
artifacts/
publish/

### Windows
Thumbs.db
[Dd]esktop.ini
$RECYCLE.BIN/
*.lnk

### VisualStudio
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*
*.nuget.props
*.nuget.targets
*.[Pp]ublish.xml
'@
    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot '.gitignore') -Content $gitIgnore -Overwrite:$Force

    $directoryBuildProps = @'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latestMajor</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors>$(WarningsAsErrors);CS*;CA*;SA*</WarningsAsErrors>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
</Project>
'@
    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot 'Directory.Build.props') -Content $directoryBuildProps -Overwrite:$Force

    $directoryPackagesProps = @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.Authorization" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.SignalR.Common" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.8" />
    <PackageVersion Include="Microsoft.AspNetCore.Cryptography.KeyDerivation" Version="10.0.8" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.8" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.2.0" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.8" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions" Version="10.0.8" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.7" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.15.0" />
    <PackageVersion Include="Microsoft.IdentityModel.Tokens" Version="8.15.0" />
    <PackageVersion Include="System.Text.Json" Version="10.0.8" />
    <PackageVersion Include="System.Configuration.ConfigurationManager" Version="9.0.9" />
    <PackageVersion Include="Asp.Versioning.Abstractions" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Mvc" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore.Swagger" Version="10.1.7" />
    <PackageVersion Include="Swashbuckle.AspNetCore.SwaggerGen" Version="10.1.7" />
    <PackageVersion Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.1.7" />
    <PackageVersion Include="AspNetCore.HealthChecks.SignalR" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Network" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.SqlServer" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.System" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Uris" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.UI" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.UI.Core" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.UI.Client" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.UI.InMemory.Storage" Version="9.0.0" />
    <PackageVersion Include="MediatR" Version="14.0.0" />
    <PackageVersion Include="AutoMapper" Version="16.0.0" />
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="6.0.1" />
    <PackageVersion Include="Polly" Version="8.5.0" />
    <PackageVersion Include="Polly.Extensions.Http" Version="3.0.0" />
    <PackageVersion Include="AspNetCoreRateLimit" Version="5.0.0" />
    <PackageVersion Include="KubernetesClient" Version="17.0.14" />
    <PackageVersion Include="Serilog" Version="4.3.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageVersion Include="Serilog.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="10.0.0" />
    <PackageVersion Include="Serilog.Expressions" Version="5.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Process" Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.ClientInfo" Version="2.0.0" />
    <PackageVersion Include="Serilog.Sinks.Async" Version="2.1.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.1.1" />
    <PackageVersion Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="FluentAssertions" Version="8.8.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="MockQueryable.Moq" Version="9.0.0" />
    <PackageVersion Include="Respawn" Version="7.0.0" />
    <PackageVersion Include="WireMock.Net" Version="1.6.6" />
    <PackageVersion Include="Bogus" Version="36.1.1" />
    <PackageVersion Include="StyleCop.Analyzers" Version="1.1.118" />
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="10.18.0.131500" />
    <!-- ===================================================== -->
    <!-- Scriban                                               -->
    <!-- ===================================================== -->
    <PackageVersion Include="Scriban" Version="7.2.1" />	
  </ItemGroup>
</Project>
'@
    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot 'Directory.Packages.props') -Content $directoryPackagesProps -Overwrite:$Force
}

function Ensure-SolutionFolders {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    $folders = @(
        'src',
        'src\ApiHosts',
        'src\WorkerHosts',
        'src\Modules',
        'src\BuildingBlocks',
        'src\Shared',
        'tests',
        'tests\ApiHosts',
        'tests\WorkerHosts',
        'tests\Modules',
        'tests\BuildingBlocks',
        'tests\Shared'
    )

    foreach ($folder in $folders) {
        Ensure-Directory -Path (Join-Path $ResolvedSolutionRoot $folder)
    }
}

function New-BuildingBlocks {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName
    )

    Write-Step "Creating BuildingBlocks"

    $basePath = Join-Path $ResolvedSolutionRoot 'src\BuildingBlocks'

    $projects = @(
        "$ResolvedSolutionName.Abstractions",
        "$ResolvedSolutionName.Contracts",
        "$ResolvedSolutionName.Infrastructure",
        "$ResolvedSolutionName.SharedKernel"
    )

    foreach ($projectName in $projects) {
        $projectDir = Join-Path $basePath $projectName
        Ensure-Directory -Path $projectDir

        $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@
        Write-GeneratedFile -Path (Join-Path $projectDir "$projectName.csproj") -Content $csproj -Overwrite:$Force
        Write-GeneratedFile -Path (Join-Path $projectDir 'AssemblyReference.cs') -Content "namespace $projectName;" -Overwrite:$Force
    }
}

function New-ApiHost {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName
    )

    Write-Step "Creating ApiHost"

    $apiHostName = "$ResolvedSolutionName.ApiHost"
    $apiHostPath = Join-Path $ResolvedSolutionRoot "src\ApiHosts\$apiHostName"
    Ensure-Directory -Path $apiHostPath

    $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk.Web`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.AspNetCore.OpenApi`" />
    <PackageReference Include=`"Swashbuckle.AspNetCore`" />
  </ItemGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $apiHostPath "$apiHostName.csproj") -Content $csproj -Overwrite:$Force

    $program = @"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Solution = "$ResolvedSolutionName",
    Host = "$apiHostName",
    Timestamp = DateTimeOffset.UtcNow
}));

app.Run();
"@
    Write-GeneratedFile -Path (Join-Path $apiHostPath 'Program.cs') -Content $program -Overwrite:$Force

    $appsettings = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@
    Write-GeneratedFile -Path (Join-Path $apiHostPath 'appsettings.json') -Content $appsettings -Overwrite:$Force

    $appsettingsDev = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
'@
    Write-GeneratedFile -Path (Join-Path $apiHostPath 'appsettings.Development.json') -Content $appsettingsDev -Overwrite:$Force
}

function New-MinimalTestProject {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$TargetProjectReference,
        [Parameter(Mandatory = $true)][string]$SampleTestClassName,
        [Parameter(Mandatory = $true)][string]$SampleTestContent
    )

    Ensure-Directory -Path $ProjectPath

    $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.NET.Test.Sdk`" />
    <PackageReference Include=`"xunit`" />
    <PackageReference Include=`"xunit.runner.visualstudio`" />
    <PackageReference Include=`"coverlet.collector`" />
    <PackageReference Include=`"FluentAssertions`" />
    <PackageReference Include=`"Moq`" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=`"$TargetProjectReference`" />
  </ItemGroup>

</Project>
"@

    Write-GeneratedFile -Path (Join-Path $ProjectPath "$ProjectName.csproj") -Content $csproj -Overwrite:$Force
    Write-GeneratedFile -Path (Join-Path $ProjectPath "$SampleTestClassName.cs") -Content $SampleTestContent -Overwrite:$Force
}

function New-ApiHostIntegrationTests {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName
    )

    $projectName = "$ResolvedSolutionName.ApiHost.IntegrationTests"
    $projectPath = Join-Path $ResolvedSolutionRoot "tests\ApiHosts\$projectName"
    Ensure-Directory -Path $projectPath

    $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.NET.Test.Sdk`" />
    <PackageReference Include=`"xunit`" />
    <PackageReference Include=`"xunit.runner.visualstudio`" />
    <PackageReference Include=`"coverlet.collector`" />
    <PackageReference Include=`"FluentAssertions`" />
    <PackageReference Include=`"Microsoft.AspNetCore.Mvc.Testing`" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=`"..\..\..\src\ApiHosts\$ResolvedSolutionName.ApiHost\$ResolvedSolutionName.ApiHost.csproj`" />
  </ItemGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $projectPath "$projectName.csproj") -Content $csproj -Overwrite:$Force

    $test = @"
using FluentAssertions;
using Xunit;

namespace $projectName;

public sealed class SmokeTests
{
    [Fact]
    public void Project_should_exist()
    {
        true.Should().BeTrue();
    }
}
"@
    Write-GeneratedFile -Path (Join-Path $projectPath 'SmokeTests.cs') -Content $test -Overwrite:$Force
}

function New-SharedProject {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName,
        [Parameter(Mandatory = $true)][string]$SharedProjectShortName,
        [Parameter(Mandatory = $true)][bool]$CreateTests
    )

    Assert-ValidName -Name $SharedProjectShortName -ParameterName 'ProjectName'

    $projectName = "$ResolvedSolutionName.$SharedProjectShortName"
    $projectPath = Join-Path $ResolvedSolutionRoot "src\Shared\$projectName"

    if ((Test-Path -LiteralPath $projectPath) -and (-not $Force) -and (-not $SkipIfExists)) {
        throw "Shared project already exists: $projectPath"
    }

    Ensure-Directory -Path $projectPath

    $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $projectPath "$projectName.csproj") -Content $csproj -Overwrite:$Force

    $marker = @"
namespace $projectName;

public static class ${SharedProjectShortName}Marker
{
}
"@
    Write-GeneratedFile -Path (Join-Path $projectPath "${SharedProjectShortName}Marker.cs") -Content $marker -Overwrite:$Force

    if ($CreateTests) {
        $testProjectName = "$projectName.UnitTests"
        $testProjectPath = Join-Path $ResolvedSolutionRoot "tests\Shared\$testProjectName"

        $sampleTest = @"
using FluentAssertions;
using Xunit;

namespace $testProjectName;

public sealed class ${SharedProjectShortName}MarkerTests
{
    [Fact]
    public void Marker_should_exist()
    {
        typeof($projectName.${SharedProjectShortName}Marker).Should().NotBeNull();
    }
}
"@

        New-MinimalTestProject `
            -ProjectPath $testProjectPath `
            -ProjectName $testProjectName `
            -TargetProjectReference "..\..\src\Shared\$projectName\$projectName.csproj" `
            -SampleTestClassName "${SharedProjectShortName}MarkerTests" `
            -SampleTestContent $sampleTest
    }
}

function New-Module {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName,
        [Parameter(Mandatory = $true)][string]$NewModuleName,
        [Parameter(Mandatory = $true)][bool]$CreateTests
    )

    Assert-ValidName -Name $NewModuleName -ParameterName 'ModuleName'

    $moduleRoot = Join-Path $ResolvedSolutionRoot "src\Modules\$NewModuleName"

    if ((Test-Path -LiteralPath $moduleRoot) -and (-not $Force) -and (-not $SkipIfExists)) {
        throw "Module already exists: $moduleRoot"
    }

    Write-Step "Creating module: $NewModuleName"

    $domainPath = Join-Path $moduleRoot "$NewModuleName.Domain"
    $applicationPath = Join-Path $moduleRoot "$NewModuleName.Application"
    $infrastructurePath = Join-Path $moduleRoot "$NewModuleName.Infrastructure"
    $apiPath = Join-Path $moduleRoot "$NewModuleName.Api"

    Ensure-Directory -Path $domainPath
    Ensure-Directory -Path $applicationPath
    Ensure-Directory -Path $infrastructurePath
    Ensure-Directory -Path $apiPath

    $domainCsproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <ItemGroup>
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Abstractions\$ResolvedSolutionName.Abstractions.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.SharedKernel\$ResolvedSolutionName.SharedKernel.csproj`" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $domainPath "$NewModuleName.Domain.csproj") -Content $domainCsproj -Overwrite:$Force

    $domainMarker = @"
namespace $NewModuleName.Domain;

public static class AssemblyReference
{
}
"@
    Write-GeneratedFile -Path (Join-Path $domainPath 'AssemblyReference.cs') -Content $domainMarker -Overwrite:$Force

    $applicationCsproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.Extensions.DependencyInjection.Abstractions`" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=`"..\$NewModuleName.Domain\$NewModuleName.Domain.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Abstractions\$ResolvedSolutionName.Abstractions.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Contracts\$ResolvedSolutionName.Contracts.csproj`" />
  </ItemGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $applicationPath "$NewModuleName.Application.csproj") -Content $applicationCsproj -Overwrite:$Force

    $applicationMarker = @"
namespace $NewModuleName.Application;

public static class AssemblyReference
{
}
"@
    Write-GeneratedFile -Path (Join-Path $applicationPath 'AssemblyReference.cs') -Content $applicationMarker -Overwrite:$Force

    $infrastructureCsproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.EntityFrameworkCore`" />
    <PackageReference Include=`"Microsoft.EntityFrameworkCore.Design`" />
    <PackageReference Include=`"Npgsql.EntityFrameworkCore.PostgreSQL`" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=`"..\$NewModuleName.Application\$NewModuleName.Application.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Abstractions\$ResolvedSolutionName.Abstractions.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Infrastructure\$ResolvedSolutionName.Infrastructure.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.SharedKernel\$ResolvedSolutionName.SharedKernel.csproj`" />
  </ItemGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $infrastructurePath "$NewModuleName.Infrastructure.csproj") -Content $infrastructureCsproj -Overwrite:$Force

    $infrastructureDi = @"
using Microsoft.Extensions.DependencyInjection;

namespace $NewModuleName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection Add${NewModuleName}Infrastructure(this IServiceCollection services)
    {
        return services;
    }
}
"@
    Write-GeneratedFile -Path (Join-Path $infrastructurePath 'DependencyInjection.cs') -Content $infrastructureDi -Overwrite:$Force

    $apiCsproj = @"
<Project Sdk=`"Microsoft.NET.Sdk`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.AspNetCore.Mvc.Core`" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=`"..\$NewModuleName.Application\$NewModuleName.Application.csproj`" />
    <ProjectReference Include=`"..\$NewModuleName.Infrastructure\$NewModuleName.Infrastructure.csproj`" />
    <ProjectReference Include=`"..\..\..\BuildingBlocks\$ResolvedSolutionName.Contracts\$ResolvedSolutionName.Contracts.csproj`" />
  </ItemGroup>

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $apiPath "$NewModuleName.Api.csproj") -Content $apiCsproj -Overwrite:$Force

    Ensure-Directory -Path (Join-Path $apiPath 'Controllers')
    Ensure-Directory -Path (Join-Path $apiPath 'Models')
    Ensure-Directory -Path (Join-Path $apiPath 'Services')

    $controller = @"
using Microsoft.AspNetCore.Mvc;

namespace $NewModuleName.Api.Controllers;

[ApiController]
[Route(""api/$($NewModuleName.ToLower())"")]
public sealed class ${NewModuleName}Controller : ControllerBase
{
    [HttpGet(""ping"")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Module = "$NewModuleName",
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
"@
    Write-GeneratedFile -Path (Join-Path $apiPath "Controllers\${NewModuleName}Controller.cs") -Content $controller -Overwrite:$Force

    $apiDi = @"
using Microsoft.Extensions.DependencyInjection;

namespace $NewModuleName.Api;

public static class DependencyInjection
{
    public static IServiceCollection Add${NewModuleName}Api(this IServiceCollection services)
    {
        services.Add${NewModuleName}Infrastructure();
        return services;
    }
}
"@
    Write-GeneratedFile -Path (Join-Path $apiPath 'DependencyInjection.cs') -Content $apiDi -Overwrite:$Force

    $apiModel = @"
namespace $NewModuleName.Api.Models;

public sealed class ${NewModuleName}Response
{
    public string Message { get; init; } = string.Empty;
}
"@
    Write-GeneratedFile -Path (Join-Path $apiPath "Models\${NewModuleName}Response.cs") -Content $apiModel -Overwrite:$Force

    $apiService = @"
namespace $NewModuleName.Api.Services;

public sealed class ${NewModuleName}ApiService
{
}
"@
    Write-GeneratedFile -Path (Join-Path $apiPath "Services\${NewModuleName}ApiService.cs") -Content $apiService -Overwrite:$Force

    if ($CreateTests) {
        $testsBase = Join-Path $ResolvedSolutionRoot "tests\Modules\$NewModuleName"
        Ensure-Directory -Path $testsBase

        $domainTestName = "$NewModuleName.Domain.UnitTests"
        $domainTestPath = Join-Path $testsBase $domainTestName
        $domainTestContent = @"
using FluentAssertions;
using Xunit;

namespace $domainTestName;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void Assembly_reference_should_exist()
    {
        typeof($NewModuleName.Domain.AssemblyReference).Should().NotBeNull();
    }
}
"@
        New-MinimalTestProject `
            -ProjectPath $domainTestPath `
            -ProjectName $domainTestName `
            -TargetProjectReference "..\..\..\src\Modules\$NewModuleName\$NewModuleName.Domain\$NewModuleName.Domain.csproj" `
            -SampleTestClassName 'AssemblyReferenceTests' `
            -SampleTestContent $domainTestContent

        $applicationTestName = "$NewModuleName.Application.UnitTests"
        $applicationTestPath = Join-Path $testsBase $applicationTestName
        $applicationTestContent = @"
using FluentAssertions;
using Xunit;

namespace $applicationTestName;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void Assembly_reference_should_exist()
    {
        typeof($NewModuleName.Application.AssemblyReference).Should().NotBeNull();
    }
}
"@
        New-MinimalTestProject `
            -ProjectPath $applicationTestPath `
            -ProjectName $applicationTestName `
            -TargetProjectReference "..\..\..\src\Modules\$NewModuleName\$NewModuleName.Application\$NewModuleName.Application.csproj" `
            -SampleTestClassName 'AssemblyReferenceTests' `
            -SampleTestContent $applicationTestContent

        if ($TestProfile -eq 'Full') {
            $infrastructureTestName = "$NewModuleName.Infrastructure.UnitTests"
            $infrastructureTestPath = Join-Path $testsBase $infrastructureTestName
            $infrastructureTestContent = @"
using FluentAssertions;
using Xunit;

namespace $infrastructureTestName;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Type_should_exist()
    {
        typeof($NewModuleName.Infrastructure.DependencyInjection).Should().NotBeNull();
    }
}
"@
            New-MinimalTestProject `
                -ProjectPath $infrastructureTestPath `
                -ProjectName $infrastructureTestName `
                -TargetProjectReference "..\..\..\src\Modules\$NewModuleName\$NewModuleName.Infrastructure\$NewModuleName.Infrastructure.csproj" `
                -SampleTestClassName 'DependencyInjectionTests' `
                -SampleTestContent $infrastructureTestContent

            $apiTestName = "$NewModuleName.Api.UnitTests"
            $apiTestPath = Join-Path $testsBase $apiTestName
            $apiTestContent = @"
using FluentAssertions;
using Xunit;

namespace $apiTestName;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Type_should_exist()
    {
        typeof($NewModuleName.Api.DependencyInjection).Should().NotBeNull();
    }
}
"@
            New-MinimalTestProject `
                -ProjectPath $apiTestPath `
                -ProjectName $apiTestName `
                -TargetProjectReference "..\..\..\src\Modules\$NewModuleName\$NewModuleName.Api\$NewModuleName.Api.csproj" `
                -SampleTestClassName 'DependencyInjectionTests' `
                -SampleTestContent $apiTestContent
        }
    }
}

function Add-ModuleReferenceToApiHost {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName,
        [Parameter(Mandatory = $true)][string]$NewModuleName
    )

    $apiHostProject = Join-Path $ResolvedSolutionRoot "src\ApiHosts\$ResolvedSolutionName.ApiHost\$ResolvedSolutionName.ApiHost.csproj"
    if (-not (Test-Path -LiteralPath $apiHostProject)) {
        Write-WarnMessage "ApiHost project not found. Skipping module registration."
        return
    }

    $projectReference = "<ProjectReference Include=`"..\..\Modules\$NewModuleName\$NewModuleName.Api\$NewModuleName.Api.csproj`" />"
    $content = Get-Content -LiteralPath $apiHostProject -Raw

    if ($content.Contains($projectReference)) {
        return
    }

    $replacement = @"
  <ItemGroup>
    $projectReference
  </ItemGroup>

</Project>
"@

    $updated = $content -replace '</Project>', $replacement
    Write-FileUtf8NoBom -Path $apiHostProject -Content $updated
}

function New-WorkerHost {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionName,
        [Parameter(Mandatory = $true)][string]$NewWorkerName,
        [string[]]$ReferencedModules,
        [Parameter(Mandatory = $true)][bool]$CreateTests
    )

    Assert-ValidName -Name $NewWorkerName -ParameterName 'WorkerName'

    $projectName = "$ResolvedSolutionName.$NewWorkerName.WorkerHost"
    $projectPath = Join-Path $ResolvedSolutionRoot "src\WorkerHosts\$projectName"

    if ((Test-Path -LiteralPath $projectPath) -and (-not $Force) -and (-not $SkipIfExists)) {
        throw "WorkerHost already exists: $projectPath"
    }

    Write-Step "Creating WorkerHost: $projectName"
    Ensure-Directory -Path $projectPath

    $projectReferences = New-Object System.Collections.Generic.List[string]
    if ($ReferencedModules) {
        foreach ($module in $ReferencedModules) {
            Assert-ValidName -Name $module -ParameterName 'Modules'
            $projectReferences.Add("    <ProjectReference Include=`"..\..\Modules\$module\$module.Application\$module.Application.csproj`" />")
            $projectReferences.Add("    <ProjectReference Include=`"..\..\Modules\$module\$module.Infrastructure\$module.Infrastructure.csproj`" />")
        }
    }

    $referencesBlock = ''
    if ($projectReferences.Count -gt 0) {
        $referencesBlock = @"

  <ItemGroup>
$($projectReferences -join [Environment]::NewLine)
  </ItemGroup>
"@
    }

    $csproj = @"
<Project Sdk=`"Microsoft.NET.Sdk.Worker`">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=`"Microsoft.Extensions.Hosting`" />
  </ItemGroup>$referencesBlock

</Project>
"@
    Write-GeneratedFile -Path (Join-Path $projectPath "$projectName.csproj") -Content $csproj -Overwrite:$Force

    $program = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
"@
    Write-GeneratedFile -Path (Join-Path $projectPath 'Program.cs') -Content $program -Overwrite:$Force

    $worker = @"
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace $projectName;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(""Worker {WorkerName} running at: {Timestamp}"", "$NewWorkerName", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
"@
    Write-GeneratedFile -Path (Join-Path $projectPath 'Worker.cs') -Content $worker -Overwrite:$Force

    $appsettings = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
'@
    Write-GeneratedFile -Path (Join-Path $projectPath 'appsettings.json') -Content $appsettings -Overwrite:$Force

    $appsettingsDev = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
'@
    Write-GeneratedFile -Path (Join-Path $projectPath 'appsettings.Development.json') -Content $appsettingsDev -Overwrite:$Force

    if ($CreateTests) {
        $testProjectName = "$projectName.IntegrationTests"
        $testProjectPath = Join-Path $ResolvedSolutionRoot "tests\WorkerHosts\$testProjectName"

        $test = @"
using FluentAssertions;
using Xunit;

namespace $testProjectName;

public sealed class WorkerTests
{
    [Fact]
    public void Worker_type_should_exist()
    {
        typeof($projectName.Worker).Should().NotBeNull();
    }
}
"@

        New-MinimalTestProject `
            -ProjectPath $testProjectPath `
            -ProjectName $testProjectName `
            -TargetProjectReference "..\..\src\WorkerHosts\$projectName\$projectName.csproj" `
            -SampleTestClassName 'WorkerTests' `
            -SampleTestContent $test
    }
}

function Get-SolutionProjectPaths {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    return @(
        Get-ChildItem -LiteralPath $ResolvedSolutionRoot -Recurse -Filter *.csproj |
            Sort-Object FullName |
            ForEach-Object {
                Get-RelativeProjectPath -ResolvedSolutionRoot $ResolvedSolutionRoot -FullProjectPath $_.FullName
            }
    )
}

function Get-FolderProjectPaths {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot,
        [Parameter(Mandatory = $true)][string]$FolderRelativePathPrefix
    )

    return @(
        (Get-SolutionProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot) |
            Where-Object { $_ -like "$FolderRelativePathPrefix*" }
    )
}

function New-FolderNodeXml {
    param(
        [Parameter(Mandatory = $true)][string]$FolderName,
        [string[]]$ProjectPaths
    )

    if (-not $ProjectPaths -or $ProjectPaths.Count -eq 0) {
        return "  <Folder Name=`"$FolderName`" />"
    }

    $entries = $ProjectPaths | ForEach-Object { "    <Project Path=`"$_`" />" }
    return @"
  <Folder Name="$FolderName">
$($entries -join [Environment]::NewLine)
  </Folder>
"@
}

function Save-SolutionManifest {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $ResolvedSolutionRoot
    $manifestPath = Get-SolutionManifestPath -ResolvedSolutionRoot $ResolvedSolutionRoot

    Write-Step "Writing solution manifest (.slnx)"

    $allProjects = @(Get-SolutionProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot)
    $projectEntries = @(
        $allProjects | ForEach-Object { "    <Project Path=`"$_`" />" }
    )

    $folders = New-Object System.Collections.Generic.List[string]

    $buildingBlocksProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'src/BuildingBlocks/')
    $folders.Add((New-FolderNodeXml -FolderName '/BuildingBlocks/' -ProjectPaths $buildingBlocksProjects))

    $apiHostProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'src/ApiHosts/')
    $workerHostProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'src/WorkerHosts/')
    $hostProjects = @($apiHostProjects + $workerHostProjects | Sort-Object)
    $folders.Add((New-FolderNodeXml -FolderName '/Hosts/' -ProjectPaths $hostProjects))

    $folders.Add((New-FolderNodeXml -FolderName '/Modules/' -ProjectPaths @()))

    $moduleRoot = Join-Path $ResolvedSolutionRoot 'src\Modules'
    if (Test-Path -LiteralPath $moduleRoot) {
        $moduleDirectories = @(
            Get-ChildItem -LiteralPath $moduleRoot -Directory | Sort-Object Name
        )

        foreach ($moduleDir in $moduleDirectories) {
            $moduleProjects = @(
                Get-ChildItem -LiteralPath $moduleDir.FullName -Recurse -Filter *.csproj |
                    Sort-Object FullName |
                    ForEach-Object {
                        Get-RelativeProjectPath -ResolvedSolutionRoot $ResolvedSolutionRoot -FullProjectPath $_.FullName
                    }
            )

            $folders.Add((New-FolderNodeXml -FolderName "/Modules/$($moduleDir.Name)/" -ProjectPaths $moduleProjects))
        }
    }

    $sharedProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'src/Shared/')
    $folders.Add((New-FolderNodeXml -FolderName '/Shared/' -ProjectPaths $sharedProjects))

    $folders.Add((New-FolderNodeXml -FolderName '/Tests/' -ProjectPaths @()))

    $apiHostTestProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'tests/ApiHosts/')
    if ($apiHostTestProjects.Count -gt 0) {
        $folders.Add((New-FolderNodeXml -FolderName '/Tests/ApiHosts/' -ProjectPaths $apiHostTestProjects))
    }

    $workerHostTestProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'tests/WorkerHosts/')
    if ($workerHostTestProjects.Count -gt 0) {
        $folders.Add((New-FolderNodeXml -FolderName '/Tests/WorkerHosts/' -ProjectPaths $workerHostTestProjects))
    }

    $folders.Add((New-FolderNodeXml -FolderName '/Tests/Modules/' -ProjectPaths @()))

    $moduleTestsRoot = Join-Path $ResolvedSolutionRoot 'tests\Modules'
    if (Test-Path -LiteralPath $moduleTestsRoot) {
        $moduleTestDirectories = @(
            Get-ChildItem -LiteralPath $moduleTestsRoot -Directory | Sort-Object Name
        )

        foreach ($moduleTestDir in $moduleTestDirectories) {
            $moduleTestProjects = @(
                Get-ChildItem -LiteralPath $moduleTestDir.FullName -Recurse -Filter *.csproj |
                    Sort-Object FullName |
                    ForEach-Object {
                        Get-RelativeProjectPath -ResolvedSolutionRoot $ResolvedSolutionRoot -FullProjectPath $_.FullName
                    }
            )

            if ($moduleTestProjects.Count -gt 0) {
                $folders.Add((New-FolderNodeXml -FolderName "/Tests/Modules/$($moduleTestDir.Name)/" -ProjectPaths $moduleTestProjects))
            }
        }
    }

    $sharedTestProjects = @(Get-FolderProjectPaths -ResolvedSolutionRoot $ResolvedSolutionRoot -FolderRelativePathPrefix 'tests/Shared/')
    if ($sharedTestProjects.Count -gt 0) {
        $folders.Add((New-FolderNodeXml -FolderName '/Tests/Shared/' -ProjectPaths $sharedTestProjects))
    }

    $manifest = @"
<Solution Format="Scaffold.Script.Manifest.v1">
  <Name>$resolvedSolutionName</Name>
  <GeneratedAtUtc>$([DateTime]::UtcNow.ToString('o'))</GeneratedAtUtc>
  <Projects>
$($projectEntries -join [Environment]::NewLine)
  </Projects>
$($folders -join [Environment]::NewLine)
</Solution>
"@

    Write-FileUtf8NoBom -Path $manifestPath -Content $manifest
}

function New-Readme {
    param([Parameter(Mandatory = $true)][string]$ResolvedSolutionRoot)

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $ResolvedSolutionRoot

    $content = @"
# $resolvedSolutionName

Scaffolded modular monolith solution.

## Structure

- src/ApiHosts
- src/WorkerHosts
- src/Modules
- src/BuildingBlocks
- src/Shared
- tests

## Notes

- .NET 10
- Central package management enabled
- Minimal test profile supported
- Solution manifest stored as .slnx
"@

    Write-GeneratedFile -Path (Join-Path $ResolvedSolutionRoot 'README.md') -Content $content -Overwrite:$Force
}

function Invoke-InitSolution {
    Assert-NotNullOrWhiteSpace -Value $SolutionName -ParameterName 'SolutionName'
    Assert-ValidName -Name $SolutionName -ParameterName 'SolutionName'

    $resolvedSolutionRoot = Join-Path $RootPath $SolutionName

    if ((Test-Path -LiteralPath $resolvedSolutionRoot) -and $Force) {
        Write-Step "Removing existing solution root because -Force was specified"
        Remove-DirectorySafe -Path $resolvedSolutionRoot
    }

    if (Test-Path -LiteralPath $resolvedSolutionRoot) {
        throw "Solution root already exists: $resolvedSolutionRoot"
    }

    Write-Step "Creating solution root: $resolvedSolutionRoot"
    Ensure-Directory -Path $resolvedSolutionRoot

    Ensure-SolutionFolders -ResolvedSolutionRoot $resolvedSolutionRoot
    New-RootConfigFiles -ResolvedSolutionRoot $resolvedSolutionRoot
    New-BuildingBlocks -ResolvedSolutionRoot $resolvedSolutionRoot -ResolvedSolutionName $SolutionName
    New-ApiHost -ResolvedSolutionRoot $resolvedSolutionRoot -ResolvedSolutionName $SolutionName

    if ($SharedProjects) {
        foreach ($sharedProject in $SharedProjects) {
            New-SharedProject `
                -ResolvedSolutionRoot $resolvedSolutionRoot `
                -ResolvedSolutionName $SolutionName `
                -SharedProjectShortName $sharedProject `
                -CreateTests:$WithTests
        }
    }

    if ($Modules) {
        foreach ($module in $Modules) {
            New-Module `
                -ResolvedSolutionRoot $resolvedSolutionRoot `
                -ResolvedSolutionName $SolutionName `
                -NewModuleName $module `
                -CreateTests:$WithTests

            if ($RegisterInApiHost) {
                Add-ModuleReferenceToApiHost `
                    -ResolvedSolutionRoot $resolvedSolutionRoot `
                    -ResolvedSolutionName $SolutionName `
                    -NewModuleName $module
            }
        }
    }

    if ($WithTests) {
        New-ApiHostIntegrationTests -ResolvedSolutionRoot $resolvedSolutionRoot -ResolvedSolutionName $SolutionName
    }

    New-Readme -ResolvedSolutionRoot $resolvedSolutionRoot
    Save-SolutionManifest -ResolvedSolutionRoot $resolvedSolutionRoot

    Write-Host ""
    Write-Host "Solution scaffolded successfully:" -ForegroundColor Green
    Write-Host "  $resolvedSolutionRoot"
}

function Invoke-AddModule {
    Assert-NotNullOrWhiteSpace -Value $ModuleName -ParameterName 'ModuleName'
    Assert-ValidName -Name $ModuleName -ParameterName 'ModuleName'

    $resolvedSolutionRoot = Get-SolutionRootPath
    Assert-SolutionExists -ResolvedSolutionRoot $resolvedSolutionRoot

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $resolvedSolutionRoot

    New-Module `
        -ResolvedSolutionRoot $resolvedSolutionRoot `
        -ResolvedSolutionName $resolvedSolutionName `
        -NewModuleName $ModuleName `
        -CreateTests:$WithTests

    if ($RegisterInApiHost) {
        Add-ModuleReferenceToApiHost `
            -ResolvedSolutionRoot $resolvedSolutionRoot `
            -ResolvedSolutionName $resolvedSolutionName `
            -NewModuleName $ModuleName
    }

    Save-SolutionManifest -ResolvedSolutionRoot $resolvedSolutionRoot

    Write-Host ""
    Write-Host "Module added successfully and registered in solution manifest: $ModuleName" -ForegroundColor Green
}

function Invoke-AddWorkerHost {
    Assert-NotNullOrWhiteSpace -Value $WorkerName -ParameterName 'WorkerName'
    Assert-ValidName -Name $WorkerName -ParameterName 'WorkerName'

    $resolvedSolutionRoot = Get-SolutionRootPath
    Assert-SolutionExists -ResolvedSolutionRoot $resolvedSolutionRoot

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $resolvedSolutionRoot

    New-WorkerHost `
        -ResolvedSolutionRoot $resolvedSolutionRoot `
        -ResolvedSolutionName $resolvedSolutionName `
        -NewWorkerName $WorkerName `
        -ReferencedModules $Modules `
        -CreateTests:$WithTests

    Save-SolutionManifest -ResolvedSolutionRoot $resolvedSolutionRoot

    Write-Host ""
    Write-Host "WorkerHost added successfully and registered in solution manifest: $resolvedSolutionName.$WorkerName.WorkerHost" -ForegroundColor Green
}

function Invoke-AddSharedProject {
    Assert-NotNullOrWhiteSpace -Value $ProjectName -ParameterName 'ProjectName'
    Assert-ValidName -Name $ProjectName -ParameterName 'ProjectName'

    $resolvedSolutionRoot = Get-SolutionRootPath
    Assert-SolutionExists -ResolvedSolutionRoot $resolvedSolutionRoot

    $resolvedSolutionName = Get-SolutionNameFromRoot -ResolvedSolutionRoot $resolvedSolutionRoot

    New-SharedProject `
        -ResolvedSolutionRoot $resolvedSolutionRoot `
        -ResolvedSolutionName $resolvedSolutionName `
        -SharedProjectShortName $ProjectName `
        -CreateTests:$WithTests

    Save-SolutionManifest -ResolvedSolutionRoot $resolvedSolutionRoot

    Write-Host ""
    Write-Host "Shared project added successfully and registered in solution manifest: $resolvedSolutionName.$ProjectName" -ForegroundColor Green
}

switch ($Command) {
    'InitSolution' { Invoke-InitSolution }
    'AddModule' { Invoke-AddModule }
    'AddWorkerHost' { Invoke-AddWorkerHost }
    'AddSharedProject' { Invoke-AddSharedProject }
    default { throw "Unsupported command: $Command" }
}