

## SampleApp

Console��������ע�룬Serilog��־

����Ҫ��

- Scriban
- HTTP Request
- Serilog
- Humanizer
- HtmlAgilityPack
- FreeSql
- Newtonsoft.Json
- Json Web Token

xUnit��������ע��

- Xunit.DependencyInjection

## �½�һ��console��Ŀ

���ð�
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="7.0.1" />
```

��ʼ��Host
```csharp
static IHost AppStartup()
{
    var host = Host.CreateDefaultBuilder() // Initialising the Host 
                .ConfigureServices((context, services) =>
                {
                    // Adding the DI container for configuration
                    services.AddTransient<App>(); // Add transiant mean give me an instance each it is being requested
                })
                .ConfigureAppConfiguration((host, config) =>
                {
                    //config.AddJsonFile($"settings.json", optional: true, reloadOnChange: true);
                })
                .Build(); // Build the Host

    return host;
}
```

һ���򵥵ķ���û�нӿ�
```csharp
public class App
{
    private readonly ILogger<App> _logger;
    public App(ILogger<App> logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        _logger.LogInformation("App Run Start");
        await Task.FromResult(0);
        _logger.LogInformation("App Run End!");
    }
}
```

���� 
```csharp
static async Task Main(string[] args)
{
    var host = AppStartup();

    var app = host.Services.GetService<App>();

    await app.RunAsync(args);
}
```


### ���� Serilog

���ð�
```xml
<PackageReference Include="Serilog.Extensions.Hosting" Version="4.1.2" />
<PackageReference Include="Serilog.Settings.Configuration" Version="3.2.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="4.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```


��Build�������� ֮ǰ���� `UseSerilog`
```diff
static IHost AppStartup()
{
    var host = Host.CreateDefaultBuilder() // Initialising the Host 
                .ConfigureServices((context, services) =>
                {
                    // Adding the DI container for configuration
                    ConfigureServices(context, services);
                    services.AddTransient<App>(); // Add transiant mean give me an instance each it is being requested
                })
                .ConfigureAppConfiguration((host, config) =>
                {
                    config.AddJsonFile($"settings.json", optional: true, reloadOnChange: true);
                })
+                .UseSerilog() // Add Serilog
                .Build(); // Build the Host

    return host;
}
```


����һ�������ķ������÷���
```csharp
static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    var configuration = context.Configuration;

    Log.Logger = new LoggerConfiguration() // initiate the logger configuration
                    .ReadFrom.Configuration(configuration) // connect serilog to our configuration folder
                    .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog 
                    .CreateLogger(); //initialise the logger

    Log.Logger.Information("ConfigureServices Starting");

}
```  

appsettings.json������
```json
   "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File"
    ],
    "MinimalLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log.txt",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        }
      }
    ]
  },
```


## ����HTTP����

���Ӱ�

```xml
    <PackageReference Include="Microsoft.Extensions.Http" Version="5.0.0" />
```
 
��ConfigureServices�������÷�����

```csharp
    services.AddHttpClient();
```    
       
��App.cs�оͿ��Ե�����

```csharp
public class App
{
    private readonly ILogger<App> _logger;
    private readonly IHttpClientFactory httpClientFactory;
    public App(ILogger<App> loggerIHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        this.httpClientFactory = httpClientFactory;
    }
    public async Task<T> GetAsync<T>(string url)
    {
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
{"accept", "application/json, text/plain, */*"},
{"Accept-Language", "zh-CN,zh;q=0.9" },
{"Cookie", ""},
{ "Proxy-Connection"," keep-alive"},
{"Upgrade-Insecure-Requests", "1"},
{"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36"}
        };

        using (HttpClient client = httpClientFactory.CreateClient())
        {
            if (headers != null)
            {
                foreach (var header in headers)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            T result = default(T);
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<T>();
            }

            return result;
        }
    }
}
```

## ���� FreeSql

���ð�
```xml
	<PackageReference Include="FreeSql" Version="2.6.100" />
	<PackageReference Include="FreeSql.Provider.Sqlite" Version="2.6.100" />
	<PackageReference Include="FreeSql.Provider.MySqlConnector" Version="2.6.100" />
	<PackageReference Include="FreeSql.Provider.SqlServer" Version="2.6.100" />
	<PackageReference Include="FreeSql.Repository" Version="2.6.100" />
```

 
�ڷ���ConfigureServices����FreeSql�ķ���
```csharp
static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                        //.UseConnectionString(FreeSql.DataType.Sqlite, configuration["ConnectionStrings:DefaultConnection"])
                        .UseConnectionString(FreeSql.DataType.MySql, configuration["ConnectionStrings:MySql"])
                        //.UseConnectionString(FreeSql.DataType.SqlServer, configuration["ConnectionStrings:SqlServer"])
                        .UseAutoSyncStructure(true)
                        //.UseNoneCommandParameter(true)
                        //.UseGenerateCommandParameterWithLambda(true)
                        .UseLazyLoading(true)
                        .UseMonitorCommand(
                            cmd => Trace.WriteLine("\r\n�߳�" + Thread.CurrentThread.ManagedThreadId + ": " + cmd.CommandText)
                            )
                        .Build();
    fsql.Aop.ConfigEntityProperty += (s, e) =>
    {
        if (e.Property.PropertyType == typeof(decimal) || e.Property.PropertyType == typeof(decimal?))
        {
            e.ModifyResult.Precision = 18;
            e.ModifyResult.Scale = 6;
            e.ModifyResult.DbType = "decimal";
        }
    };

    services.AddSingleton(fsql);
    services.AddFreeRepository();
    services.AddScoped<UnitOfWorkManager>();
}
```

appsettings.json�������ݿ�����
```json
 "ConnectionStrings": {
    "DefaultConnection": "Data Source=|DataDirectory|\\SampleApp.db;",
    "MySql": "Data Source=localhost;Port=3306;User ID=root;Password=root;Initial Catalog=sampleapp;Charset=utf8mb4;SslMode=none;Max pool size=1;Connection LifeTime=20",
    "SqlServer": "Data Source=192.168.1.19;User Id=sa;Password=123456;Initial Catalog=freesqlTest;Pooling=true;Min Pool Size=1"
  },
```


ǿ���Ͱ󶨶���
```csharp
    services.Configure<AppOption>(configuration.GetSection(nameof(AppOption)));
```

ʵ��
```csharp
public class AppOption
{
    public string TemplatesPath { get; set; }
    public string OutputDirectory { get; set; }
}
```

appsettings.json����json����
```json
 "AppOption": {
    "TemplatesPath": "./Templates", //���·������ǰ��Ŀ�µ�TemplatesĿ¼
    "OutputDirectory": "../../../Output" //���������·����Ҳ�����Ǿ���·��
  }
```


ʹ��
```csharp
public class App
{
    private readonly ILogger<App> _logger;
    private readonly AppOption _appOption;
    public App(ILogger<App> logger, IOptions<AppOption> appOption)
    {
        _logger = logger;
        _appOption = appOption.Value;
    }
}
```


����`HtmlAgilityPack`��Josn���л�`Newtonsoft.Json`���Ѻõİ�����`Humanizer`��ģ������`Scriban`
```xml
	<PackageReference Include="HtmlAgilityPack" Version="1.11.36" />
	<PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
	<PackageReference Include="Humanizer.Core" Version="2.11.10" />
	<PackageReference Include="Scriban" Version="4.0.1" />
```