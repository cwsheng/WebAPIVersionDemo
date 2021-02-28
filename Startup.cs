using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace WebAPIVersionDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Api版本提者信息
        /// </summary>
        private IApiVersionDescriptionProvider provider;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //需要从加载配置文件appsettings.json
            services.AddOptions();
            //需要存储速率限制计算器和ip规则
            services.AddMemoryCache();

            //从appsettings.json中加载常规配置
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //从appsettings.json中加载Ip规则
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //注入计数器和规则存储
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddControllers().AddJsonOptions(cfg =>
            {
                cfg.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //配置（解析器、计数器密钥生成器）
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            //根据需要设置，以下内容
            services.AddApiVersioning(apiOtions =>
            {
                //返回响应标头中支持的版本信息
                apiOtions.ReportApiVersions = true;
                //此选项将用于不提供版本的请求。默认情况下, 假定的 API 版本为1.0
                apiOtions.AssumeDefaultVersionWhenUnspecified = true;
                //缺省api版本号，支持时间或数字版本号
                apiOtions.DefaultApiVersion = new ApiVersion(1, 0);
                //支持MediaType、Header、QueryString 设置版本号;缺省为QueryString设置版本号
                apiOtions.ApiVersionReader = ApiVersionReader.Combine(
                                new MediaTypeApiVersionReader("api-version"),
                                new HeaderApiVersionReader("api-version"),
                                new QueryStringApiVersionReader("api-version"),
                                new UrlSegmentApiVersionReader());
            });


            services.AddVersionedApiExplorer(option =>
            {
                option.GroupNameFormat = "接口：'v'VVV";
                option.AssumeDefaultVersionWhenUnspecified = true;
            });

            this.provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
            services.AddSwaggerGen(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName,
                            new Microsoft.OpenApi.Models.OpenApiInfo()
                            {
                                Title = $"接口 v{description.ApiVersion}",
                                Version = description.ApiVersion.ToString(),
                                Description = "切换版本请点右上角版本切换"
                            }
                    );
                }
                options.IncludeXmlComments(this.GetType().Assembly.Location.Replace(".dll", ".xml"), true);
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            //使用ApiVersioning
            app.UseApiVersioning();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseIpRateLimiting();

            //app.UseClientRateLimiting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

}
