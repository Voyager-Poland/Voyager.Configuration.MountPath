# Voyager.Configuration.MountPath
 
 ---
The extension for AspNetCore to organize JSON configuration.

 ## About
 The nuget allows reading the JSON configuration files from a path. The path can be used by an environment like Linux, Docker, or Kubernetes to update the content by a mounting mechanism. In providing the library by docker images you will gain the possibility to avoid publishing your sensitive data in an image repository.

 
## 🏁 Getting Started 

### Prerequisites

The library menage configuration is supplied by the JSON files. Cooperate with the WebApplicationBuilder or HostApplicationBuilder. Linux mount mechanism can only work with folders so the config files have to be in that folder.

## 🔧 How to start

The best metod to see how it worsk is look to the thest project. 

The default configuration

```.NET CLI 
builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
{
  config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProvider());
});
```

The default configuration will work with the files structures:

``` cmd
YourAppFiles
|--bin
   |--config
      |--appsettings.json
      |--appsettings.Development.json
```

It works in the way that from the folder config will load appsettings.json. The dotnet from the ASPNETCORE_ENVIRONMENT set his HostEnviroment variable, and if is setted with value: Development the program finds the appsettings.Development.json file and will use it to override previous settings.

## 🔧 How to update default settings

The library is open for extensions so it is possible to modify behavior. At the first, it is possible to make the obligation to possess a hosting file:

```.NET CLI 
builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
{
  config.AddMountConfiguration(hostingConfiguration.HostingEnvironment.GetSettingsProviderForce());
});
```
 
 The second possibility is to override the class Voyager.Configuration.MountPath.SettingsProvider and use the instance in the AddMountConfiguration method.

Also is possible to use the AddMountConfiguration with the action that is can change all settings.

```.NET CLI 
builder.ConfigureAppConfiguration((hostingConfiguration, config) =>
{
  config.AddMountConfiguration(settings =>
  {
    settings.HostingName = "MyEnv";
    settings.Optional = false;
  });
});
```

## ✍️ Authors 

- [@andrzejswistowski](https://github.com/AndrzejSwistowski) - Idea & work. Please let me know if you find out an error or suggestions.

[contributors](https://github.com/Voyager-Poland).

## 🎉 Acknowledgements 

- Przemysław Wróbel - for the icon.
