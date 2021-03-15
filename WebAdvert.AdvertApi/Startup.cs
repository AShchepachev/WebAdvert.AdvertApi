using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using AutoMapper;
using WebAdvert.AdvertApi.Services;
using WebAdvert.AdvertApi.HealthChecks;
using System;
using Amazon.Util;
using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAdvert.AdvertApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper();
            services.AddTransient<IAdvertStorageService, DynamoDBAdvertStorage>();
            services.AddTransient<StorageHealthCheck>();

            services.AddHealthChecks().AddCheck<StorageHealthCheck>("Storage", timeout: TimeSpan.FromMinutes(1));

            services.AddCors(options =>
            {
                options.AddPolicy("AllOrigin", policy => policy.WithOrigins("*").AllowAnyHeader());
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAdvert.AdvertApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async Task Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAdvert.AdvertApi v1"));
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseHealthChecks("/health");
            app.UseCors();

            await RegisterToCloudMap();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private async Task<string> RegisterToCloudMap()
        {
            // https://github.com/aussiearef/AWS.CloudMap.RegisterMe

            const string serviceId = "srv-zujwx7satb3fup5b"; // aws cloudmap service id

            var instanceId = EC2InstanceMetadata.InstanceId;

            if (!string.IsNullOrEmpty(instanceId))
            {
                var ipv4 = EC2InstanceMetadata.PrivateIpAddress;
                var client = new AmazonServiceDiscoveryClient();

                var request = new RegisterInstanceRequest
                {
                    InstanceId = instanceId,
                    ServiceId = serviceId,
                    Attributes = new Dictionary<string, string>
                    {
                        { "AWS_INSTANCE_IPV4", ipv4 },
                        { "AWS_INSTANCE_PORT", "80" } 
                    }
                };
                var response = await client.RegisterInstanceAsync(request);
                return response.OperationId;
            }
            
            return "";
        }
    }
}
