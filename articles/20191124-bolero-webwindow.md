---
title: Desktop applications with Bolero and WebWindow
tags: [fsbolero]
---

Steve Sanderson [recently published WebWindow](https://blog.stevensanderson.com/2019/11/18/2019-11-18-webwindow-a-cross-platform-webview-for-dotnet-core/): a library that runs a web page in a desktop window, piloted from .NET.
In particular, it can run [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) applications natively on the desktop with minimal changes to their code.
Unlike client-side Blazor, this doesn't involve any WebAssembly: the Blazor code runs in .NET and interacts directly with the web page.

![The Blazor sample app running on WebWindows](/assets/blazor-macos.jpg)

This is pretty cool.
Although it is contained in a web window like an Electron application, it runs with the speed of a native .NET application and comes in a much smaller package.

Obviously, as soon as I saw it, I had to try to use it with [Bolero](https://fsbolero.io), my own F# layer for Blazor.
As it turns out, it runs quite well!
[Here's a simple working application](https://github.com/Tarmil/Bolero.WebWindowTest); let's see how to create it from scratch.

## Creating a Bolero app on WebWindow, step by step

First, if you don't have it yet, install [the .NET Core 3.0 SDK](https://dotnet.microsoft.com/download) and the Bolero project template:

```
dotnet new -i Bolero.Templates
```

We can now create a Bolero application.

```
dotnet new bolero-app --minimal --server=false -o MyBoleroWebWindowApp
cd MyBoleroWebWindowApp
```

The full template contains a few pages and uses things like remoting that we would need to untangle for this example, so we'll go for the `--minimal` template instead.
Also, we don't want to create an ASP.NET Core host application, so we use `--server=false`.

We now have a solution with a single project, `src/MyBoleroWebWindowApp.Client`, which will directly be our executable.
Let's fixup the project file `MyBoleroWebWindowApp.Client.fsproj`.

* First, this is not a web project:
    ```diff
     <?xml version="1.0" encoding="utf-8"?>
    -<Project Sdk="Microsoft.NET.Sdk.Web">
    +<Project Sdk="Microsoft.NET.Sdk">
    ```
* Second, we need to target .NET Core 3.0 and create an executable:
    ```diff
       <PropertyGroup>
    -    <TargetFramework>netstandard2.0</TargetFramework>
    +    <TargetFramework>netcoreapp3.0</TargetFramework>
    +    <OutputType>WinExe</OutputType>
       </PropertyGroup>
    ```

* Now that we removed the `Web` SDK, the `wwwroot` will not automatically included in the published application anymore.
    But we still need our assets!

    ```diff
       <ItemGroup>
    +    <Content Include="wwwroot\**">
    +      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    +    </Content>
         <Compile Include="Main.fs" />
         <Compile Include="Startup.fs" />
       </ItemGroup>
    ```

* Finally, the NuGet references. We need to remove the Blazor build packages that compile our project into a WebAssembly application, and instead add WebWindow.

    ```diff
       <ItemGroup>
         <PackageReference Include="Bolero" Version="0.10.1-preview9" />
    -    <PackageReference Include="Bolero.Build" Version="0.10.1-preview9" />
    -    <PackageReference Include="Microsoft.AspNetCore.Blazor.Build" Version="3.0-preview9.*" />
    -    <PackageReference Include="Microsoft.AspNetCore.Blazor.DevServer" Version="3.0-preview9.*" />
    +    <PackageReference Include="WebWindow.Blazor" Version="0.1.0-20191120.6" />
       </ItemGroup>
     </Project>
    ```

The main program, `Startup.fs`, needs a bit of change to start as a WebWindow application rather than a WebAssembly one.
Luckily, Steve made this very easy:

```diff
 module Program =
+    open WebWindows.Blazor
 
     [<EntryPoint>]
     let Main args =
-        BlazorWebAssemblyHost.CreateDefaultBuilder()
-            .UseBlazorStartup<Startup>()
-            .Build()
-            .Run()
+        ComponentsDesktop.Run<Startup>("My Bolero app", "wwwroot/index.html")
         0
```

And finally, the small JavaScript script that boots Bolero is in a different location, so we need to touch `wwwroot/index.html`:

```diff
-    <script src="_framework/blazor.webassembly.js"></script>
+    <script src="framework://blazor.desktop.js"></script>
```

And with this, we're all set!
Run the application using your IDE or from the command line:

```
dotnet run -p src/MyBoleroWebWindowApp.Client
```

> Note: if you're using Visual Studio, make sure to remove the file `Properties/launchSettings.json` it may have created while the SDK was still `Web`; otherwise, it will try (and fail) to run your project with IIS Express.

![The Bolero minimal app running on WebWindow](/assets/webwindow-empty.png)

We're on our way!
Although since we created a project using the `--minimal` template, this is pretty empty.
We're only seeing the banner that is present statically in `wwwroot/index.html`.
Let's make sure that Bolero is indeed running by implementing the "hello world" of the Elmish world, the Counter app, in `Main.fs`:

```fsharp
type Model =
    { counter: int }

let initModel =
    { counter = 0 }

type Message =
    | Increment
    | Decrement

let update message model =
    match message with
    | Increment -> { model with counter = model.counter + 1 }
    | Decrement -> { model with counter = model.counter - 1 }

let view model dispatch =
    concat [
        button [on.click (fun _ -> dispatch Decrement)] [text "-"]
        textf " %i " model.counter
        button [on.click (fun _ -> dispatch Increment)] [text "+"]
    ]

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        Program.mkSimple (fun _ -> initModel) update view
```

And now, if we run again:

![The Bolero counter app running on WebWindow](/assets/webwindow-counter.png)

Hurray!

## What next?

This is just an experiment to see if Bolero would "just work" with WebWindow and, well, it pretty much does.
As Steve said in his blog article, WebWindow itself is an experiment with no promises of developing it into a proper product.
But it is still pretty cool, and I want to see how far we can combine it with Bolero.
What about remoting with an ASP.NET Core server?
Or HTML template hot reloading?
These will probably need some adjustments to work nicely with WebWindow, and I think I'll experiment some more with these.