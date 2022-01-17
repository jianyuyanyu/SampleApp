﻿using FreeSql;
using IGeekFan.AspNetCore.RapiDoc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SampleApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }


    public void ConfigureServices(IServiceCollection services)
    {
        //IFreeSql fsql = new FreeSql.FreeSqlBuilder()
        ////.UseConnectionString(FreeSql.DataType.Sqlite, Configuration["ConnectionStrings:DefaultConnection"])
        //.UseConnectionString(FreeSql.DataType.MySql, Configuration["ConnectionStrings:MySql"])
        //.UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
        //.UseAutoSyncStructure(true)
        ////.UseGenerateCommandParameterWithLambda(true)
        //.UseLazyLoading(true)
        //.UseMonitorCommand(
        //    cmd => Trace.WriteLine("\r\n线程" + Thread.CurrentThread.ManagedThreadId + ": " + cmd.CommandText)
        //    )
        //.Build();

        //db 是一个静态类，并非实例化
        //事先或运行中注册 IFreeSql

        #region 静态类的注册方式
        var db = StaticDB.Instance;

        db.Register("db1", () =>
        {
            return new FreeSqlBuilder()
                .UseAutoSyncStructure(true)
                .UseMonitorCommand(
                cmd => Trace.WriteLine("\r\n线程" + Thread.CurrentThread.ManagedThreadId + ": " + cmd.CommandText)
                )
                .UseConnectionString(DataType.Sqlite, "Data Source=|DataDirectory|\\SampleApp1.db;")
                .Build();
        });
        db.Register("db2", () =>
        {
            return new FreeSqlBuilder()
                .UseAutoSyncStructure(true)
                .UseMonitorCommand(
                cmd => Trace.WriteLine("\r\n线程" + Thread.CurrentThread.ManagedThreadId + ": " + cmd.CommandText)
                )
                .UseConnectionString(DataType.Sqlite, "Data Source=|DataDirectory|\\SampleApp2.db;")
                .Build();
        });
        #endregion

        #region 依赖注入的使用方式
        var fsql2 = new MultiFreeSql();

        fsql2.Register("db1", () => new FreeSqlBuilder().UseAutoSyncStructure(true).UseConnectionString(DataType.Sqlite, "Data Source=|DataDirectory|\\SampleApp1.db;").Build());
        fsql2.Register("db2", () => new FreeSqlBuilder().UseAutoSyncStructure(true).UseConnectionString(DataType.Sqlite, "Data Source=|DataDirectory|\\SampleApp2.db;").Build());

        services.AddSingleton<IFreeSql>(fsql2);
        #endregion

        //services.AddSingleton(fsql);
        //services.AddFreeRepository();
        //services.AddScoped<UnitOfWorkManager>();

        //fsql.CodeFirst.Entity<SysUser>(eb =>
        //{
        //    eb.HasData(new List<SysUser>() { new SysUser() { Name = "ceshi" } });
        //});
        //fsql.CodeFirst.SyncStructure<SysUser>();

        services.AddJwt(Configuration);
        services.AddControllers();
        services.AddSwagger(Configuration);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleApi v1"));
        }

        var d = JsonConvert.SerializeObject(new { code = true });

        app.UseRapiDocUI(c =>
        {
            c.RoutePrefix = ""; // serve the UI at root
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleApi v1");
        });

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();
        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
