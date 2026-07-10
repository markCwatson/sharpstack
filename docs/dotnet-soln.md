The cleanest setup is a standard multi-project .NET solution:

```text
csharp-http-server/
├── ScratchHttpServer.sln
├── src/
│   └── ScratchHttpServer/
│       ├── ScratchHttpServer.csproj
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
│   └── ScratchHttpServer.Tests/
│       ├── ScratchHttpServer.Tests.csproj
│       ├── Network/
│       ├── Http/
│       └── Fixtures/
└── docs/
```

**Create the solution and projects**

```bash
cd /Users/mark/repos/csharp-http-server

dotnet new sln \
  --name ScratchHttpServer \
  --format sln

dotnet new console \
  --name ScratchHttpServer \
  --output src/ScratchHttpServer \
  --framework net10.0 \
  --use-program-main

dotnet new xunit \
  --name ScratchHttpServer.Tests \
  --output tests/ScratchHttpServer.Tests \
  --framework net10.0
```

`--format sln` is useful with .NET 10 because newer SDKs may otherwise create the newer `.slnx` format.

**Add projects to the solution**

```bash
dotnet sln ScratchHttpServer.sln add \
  src/ScratchHttpServer/ScratchHttpServer.csproj \
  --solution-folder src

dotnet sln ScratchHttpServer.sln add \
  tests/ScratchHttpServer.Tests/ScratchHttpServer.Tests.csproj \
  --solution-folder tests
```

The `--solution-folder` values are Visual Studio solution folders. They do not have to correspond to physical directories, although here they naturally do.

**Add the test project reference**

```bash
dotnet add tests/ScratchHttpServer.Tests/ScratchHttpServer.Tests.csproj \
  reference src/ScratchHttpServer/ScratchHttpServer.csproj
```

This produces the relationship:

```xml
<ProjectReference Include="..\..\src\ScratchHttpServer\ScratchHttpServer.csproj" />
```

**Create your physical source folders**

```bash
mkdir -p \
  src/ScratchHttpServer/Application \
  src/ScratchHttpServer/Network/Ethernet \
  src/ScratchHttpServer/Network/Arp \
  src/ScratchHttpServer/Network/Ipv4 \
  src/ScratchHttpServer/Network/Icmp \
  src/ScratchHttpServer/Network/Tcp \
  src/ScratchHttpServer/Http \
  src/ScratchHttpServer/Devices \
  src/ScratchHttpServer/wwwroot \
  tests/ScratchHttpServer.Tests/Network \
  tests/ScratchHttpServer.Tests/Http \
  tests/ScratchHttpServer.Tests/Fixtures
```

The .NET SDK automatically includes `.cs` files recursively, so you do not need to add every new file to the project manually. For example, a file at:

```text
src/ScratchHttpServer/Network/Tcp/TcpSegment.cs
```

is automatically compiled by ScratchHttpServer.csproj.

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
    <RootNamespace>ScratchHttpServer</RootNamespace>
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
    <ProjectReference Include="..\..\src\ScratchHttpServer\ScratchHttpServer.csproj" />
  </ItemGroup>

</Project>
```

The `dotnet new xunit` template will already add the xUnit package references.

**Create initial placeholder files**

```bash
touch \
  src/ScratchHttpServer/Application/HttpApplication.cs \
  src/ScratchHttpServer/Network/RawNetworkStack.cs \
  src/ScratchHttpServer/Network/Ethernet/EthernetFrame.cs \
  src/ScratchHttpServer/Network/Arp/ArpPacket.cs \
  src/ScratchHttpServer/Network/Ipv4/Ipv4Packet.cs \
  src/ScratchHttpServer/Network/Icmp/IcmpPacket.cs \
  src/ScratchHttpServer/Network/Tcp/TcpSegment.cs \
  src/ScratchHttpServer/Devices/LinuxTapDevice.cs
```

A namespace layout matching those folders would be:

```csharp
namespace ScratchHttpServer.Network.Tcp;

public sealed class TcpSegment
{
}
```

Or, if you prefer file-scoped namespaces grouped by subsystem:

```text
ScratchHttpServer.Network.Tcp
ScratchHttpServer.Network.Ipv4
ScratchHttpServer.Http
ScratchHttpServer.Devices
```

**Verify the generated structure**

```bash
dotnet restore
dotnet build
dotnet test
dotnet sln ScratchHttpServer.sln list
```

These commands restore dependencies, compile both projects, run the tests, and confirm that both projects are registered in the solution.
