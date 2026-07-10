The cleanest setup is a standard multi-project .NET solution:

```text
csharp-http-server/
├── App.sln
├── src/
│   └── App/
│       ├── App.csproj
│       ├── Program.cs
│       ├── Application/
│       ├── Network/
│       │   ├── Ethernet/
│       │   ├── Arp/
│       │   ├── Ipv4/
│       │   ├── Icmp/
│       │   └── Tcp/
│       ├── Http/
│       ├── Devices/
│       └── wwwroot/
├── tests/
│   └── App.Tests/
│       ├── App.Tests.csproj
│       ├── Network/
│       ├── Http/
│       └── Fixtures/
└── docs/
```

**Create the solution and projects**

```bash
cd /Users/mark/repos/csharp-http-server

dotnet new sln \
  --name App \
  --format sln

dotnet new console \
  --name App \
  --output src/App \
  --framework net10.0 \
  --use-program-main

dotnet new xunit \
  --name App.Tests \
  --output tests/App.Tests \
  --framework net10.0
```

`--format sln` is useful with .NET 10 because newer SDKs may otherwise create the newer `.slnx` format.

**Add projects to the solution**

```bash
dotnet sln App.sln add \
  src/App/App.csproj \
  --solution-folder src

dotnet sln App.sln add \
  tests/App.Tests/App.Tests.csproj \
  --solution-folder tests
```

The `--solution-folder` values are Visual Studio solution folders. They do not have to correspond to physical directories, although here they naturally do.

**Add the test project reference**

```bash
dotnet add tests/App.Tests/App.Tests.csproj \
  reference src/App/App.csproj
```

This produces the relationship:

```xml
<ProjectReference Include="..\..\src\App\App.csproj" />
```

**Create your physical source folders**

```bash
mkdir -p \
  src/App/Application \
  src/App/Network/Ethernet \
  src/App/Network/Arp \
  src/App/Network/Ipv4 \
  src/App/Network/Icmp \
  src/App/Network/Tcp \
  src/App/Http \
  src/App/Devices \
  src/App/wwwroot \
  tests/App.Tests/Network \
  tests/App.Tests/Http \
  tests/App.Tests/Fixtures
```

The .NET SDK automatically includes `.cs` files recursively, so you do not need to add every new file to the project manually. For example, a file at:

```text
src/App/Network/Tcp/TcpSegment.cs
```

is automatically compiled by App.csproj.

**Configure the application project**

The generated console project will use top-level statements unless `--use-program-main` is supported by your installed SDK.

Your application `.csproj` should contain the equivalent of:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>App</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="wwwroot\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

`ImplicitUsings` is worth disabling here because your project is intentionally avoiding convenience networking APIs. It makes every dependency visible in each source file.

The test project can keep implicit usings enabled:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\App\App.csproj" />
  </ItemGroup>

</Project>
```

The `dotnet new xunit` template will already add the xUnit package references.

**Create initial placeholder files**

```bash
touch \
  src/App/Application/HttpApplication.cs \
  src/App/Network/RawNetworkStack.cs \
  src/App/Network/Ethernet/EthernetFrame.cs \
  src/App/Network/Arp/ArpPacket.cs \
  src/App/Network/Ipv4/Ipv4Packet.cs \
  src/App/Network/Icmp/IcmpPacket.cs \
  src/App/Network/Tcp/TcpSegment.cs \
  src/App/Devices/LinuxTapDevice.cs
```

A namespace layout matching those folders would be:

```csharp
namespace App.Network.Tcp;

public sealed class TcpSegment
{
}
```

Or, if you prefer file-scoped namespaces grouped by subsystem:

```text
App.Network.Tcp
App.Network.Ipv4
App.Http
App.Devices
```

**Verify the generated structure**

```bash
dotnet restore
dotnet build
dotnet test
dotnet sln App.sln list
```

These commands restore dependencies, compile both projects, run the tests, and confirm that both projects are registered in the solution.
