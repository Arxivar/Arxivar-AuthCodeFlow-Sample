[![Arxivar](http://portal.arxivar.it/download/resources/loghi/Logo-ARXivar_orizzontale-nero.png)](http://www.arxivar.it/)

# ARXivar Authentication code flow integration
 > Integrate your .Net AspNet authentication with ARXivar


## Requirements

> - Microsoft Visual Studio 2022 or later (or any IDE that supports .Net Framework 4.8 and .Net 8)
> - ARXivar Next 2.9 or later

### Integration with AspNet .Net 6 or later
See solution example [here](src/AuthCodeFlow)
 
Configure connection with ARXivar in [appsettings.json](src/AuthCodeFlow/MvcClient/appsettings.json)

> - **AuthServiceBaseUrl** is the base address of Authentication service
> - **ClientId**
> - **ClientSecret**

```json
{
   "ArxivarAuthSettings": {
    "AuthServiceBaseUrl": "https://localhost/ARXivarNextAuthentication",
    "ClientId": "--- CLIENT ID ---",
    "ClientSecret": "--- SECRET ---"    
  }
}
```

From [Project folder](src/AuthCodeFlow/MvcClient) run

```bash
dotnet run 
```



### Integration with AspNet .Net Framework 4.6.1 or later 
See solution example [here](src/AuthCodeFlowNetFx)

Configure connection with ARXivar in [Web.config](src/AuthCodeFlowNetFx/MvcClientNetFx/Web.config)

> - **AuthServiceBaseUrl** is the base address of Authentication service
> - **ClientId**
> - **ClientSecret**

```xml
 <appSettings>
    <add key="AuthServiceBaseUrl" value="https://localhost/ARXivarNextAuthentication" />
    <add key="ClientId" value="--- CLIENT ID ---" />
    <add key="ClientSecret" value="--- SECRET ---" />    
  </appSettings>
```


## License

© [Abletech S.r.l.](http://www.arxivar.it/)
