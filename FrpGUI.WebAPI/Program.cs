using FrpGUI.Configs;
using FrpGUI.Models;
using FrpGUI.Services;
using FrpGUI.WebAPI.Models;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.OpenApi.Models;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace FrpGUI.WebAPI;

internal class Program
{
    private static bool swagger = true;

    private static WebApplication app;
    private static string cors = "cors";

    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = CreateBuilder(args);

        app = builder.Build();

        SettingApp(app);

        app.Run();
    }

    private static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
        });

        // Add services to the container.

        builder.Services.AddControllers(o =>
        {
            //�������������Ĳ�����null������token���ͻ�400 Bad Request
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            o.Filters.Add(app.Services.GetRequiredService<FrpGUIActionFilter>());
        })
            .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            o.JsonSerializerOptions.Converters.Add(new FrpConfigJsonConverter());
        });

        builder.Services.AddTransient<FrpGUIActionFilter>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(p =>
        {
            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//��ȡӦ�ó�������Ŀ¼�����ԣ����ܹ���Ŀ¼Ӱ�죬������ô˷�����ȡ·����
            var xmlPath = Path.Combine(basePath, "FrpGUI.WebAPI.xml");
            p.IncludeXmlComments(xmlPath);

            var scheme = new OpenApiSecurityScheme()
            {
                Description = "Authorization header",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Authorization"
                },
                Scheme = "oauth2",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
            };
            p.AddSecurityDefinition("Authorization", scheme);
            var requirement = new OpenApiSecurityRequirement();
            requirement[scheme] = new List<string>();
            p.AddSecurityRequirement(requirement);
        });
        builder.Services.AddDbContext<FrpDbContext>(ServiceLifetime.Transient);
        builder.Services.AddSingleton<LoggerBase, Logger>();
        builder.Services.AddSingleton<FrpProcessCollection>();
        builder.Services.AddHostedService<AppLifetimeService>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: cors,
                              policy =>
                              {
                                  policy.AllowAnyMethod()
                                  .AllowAnyHeader()
                                  .AllowAnyOrigin();
                              });
        });

        builder.Host.UseWindowsService(c =>
        {
            c.ServiceName = "FrpGUI";
        });
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        AppConfig config = AppConfigBase.Get<AppConfig>();
        builder.Services.AddSingleton(config);

        return builder;
    }

    private static void SettingApp(WebApplication app)
    {
        app.Services.GetRequiredService<LoggerBase>().Info("��������");
        if (swagger || app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        //app.UseWebSockets();
        app.UseHttpsRedirection();
        app.UseCors(cors);
        //app.UseAuthorization();
        app.MapControllers();
    }
}