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
        /// Api�汾������Ϣ
        /// </summary>
        private IApiVersionDescriptionProvider provider;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //��Ҫ�Ӽ��������ļ�appsettings.json
            services.AddOptions();
            //��Ҫ�洢�������Ƽ�������ip����
            services.AddMemoryCache();

            //��appsettings.json�м��س�������
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //��appsettings.json�м���Ip����
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //ע��������͹���洢
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddControllers().AddJsonOptions(cfg =>
            {
                cfg.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //���ã�����������������Կ��������
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            //������Ҫ���ã���������
            services.AddApiVersioning(apiOtions =>
            {
                //������Ӧ��ͷ��֧�ֵİ汾��Ϣ
                apiOtions.ReportApiVersions = true;
                //��ѡ����ڲ��ṩ�汾������Ĭ�������, �ٶ��� API �汾Ϊ1.0
                apiOtions.AssumeDefaultVersionWhenUnspecified = true;
                //ȱʡapi�汾�ţ�֧��ʱ������ְ汾��
                apiOtions.DefaultApiVersion = new ApiVersion(1, 0);
                //֧��MediaType��Header��QueryString ���ð汾��;ȱʡΪQueryString���ð汾��
                apiOtions.ApiVersionReader = ApiVersionReader.Combine(
                                new MediaTypeApiVersionReader("api-version"),
                                new HeaderApiVersionReader("api-version"),
                                new QueryStringApiVersionReader("api-version"),
                                new UrlSegmentApiVersionReader());
            });


            services.AddVersionedApiExplorer(option =>
            {
                option.GroupNameFormat = "�ӿڣ�'v'VVV";
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
                                Title = $"�ӿ� v{description.ApiVersion}",
                                Version = description.ApiVersion.ToString(),
                                Description = "�л��汾������Ͻǰ汾�л�"
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

            //ʹ��ApiVersioning
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
