# Orleans.Providers.MongoDB
> This project is in progress and is not ready for production yet

A MongoDb implementation of the Orleans Provider model. Currently the Membership(IMembershipTable & IGatewayListProvider) and Reminder(IReminderTable) providers have been implemented.

## Usage
###Host Configuration


```ps
Add reference to Orleans.Providers.MongoDB.dll
```
Update OrleansConfiguration.xml in the Host application.
```xml
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <!--
    There is currently a known issue with the "Custom" membership provider OrleansConfiguration.xml configuration file that will fail to parse correctly. For this reason you have to provide a placeholder SystemStore in the xml and then configure the provider in code before starting the Silo.
    -->
    <SystemStore SystemStoreType="None" DataConnectionString="mongodb://admin:pass123@localhost:27017/Orleans?authSource=admin" DeploymentId="OrleansTest" />
  </Globals>
  <Defaults>
    <Networking Address="" Port="11111"/>
    <ProxyingGateway Address="" Port="30000"/>
  </Defaults>
</OrleansConfiguration>
```
Add the following to the Host startup

```cs
var config = ClusterConfiguration.LocalhostPrimarySilo();
config.LoadFromFile(@".\OrleansConfiguration.xml");

using (var silo = new SiloHost("primary", config))
{
    // Init Mongo Membership
    silo.Config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.Custom;
    silo.Config.Globals.MembershipTableAssembly = "Orleans.Providers.MongoDB";

    // Disable Reminder Service
    //silo.Config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Disabled;
    
    // Enable Reminder Service
    silo.Config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.Custom;
    silo.Config.Globals.ReminderTableAssembly = "Orleans.Providers.MongoDB";

    silo.InitializeOrleansSilo();
    var result = silo.StartOrleansSilo();
}
```
###Client Configuration


```ps
Add reference to Orleans.Providers.MongoDB.dll
```
Update ClientConfiguration.xml in the Client application.
```xml
<ClientConfiguration xmlns="urn:orleans">
  <SystemStore SystemStoreType="Custom" CustomGatewayProviderAssemblyName="Orleans.Providers.MongoDB" DataConnectionString="mongodb://admin:pass123@localhost:27017/Orleans?authSource=admin" DeploymentId="OrleansTest" />
</ClientConfiguration>```
Add the following to the Client startup

```cs
GrainClient.Initialize(ClientConfiguration.LoadFromFile(@".\ClientConfiguration.xml"));
initialized = GrainClient.IsInitialized;
```

## Todo

- IStatisticsPublisher (Runtime Statistics)
- ISiloMetricsDataPublisher/IClientMetricsDataPublisher (Silo/Client Metrics)
- Create Nuget Package
