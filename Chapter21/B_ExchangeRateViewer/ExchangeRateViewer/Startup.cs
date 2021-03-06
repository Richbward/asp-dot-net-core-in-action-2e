using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Polly;

namespace ExchangeRateViewer
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
            services.AddControllers();
            services.AddHttpClient("rates", (HttpClient client) =>
            {
                client.BaseAddress = new Uri("https://api.exchangeratesapi.io");
                client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "ExchangeRateViewer");
            })
            .ConfigureHttpClient((IServiceProvider provider, HttpClient client) => { }); // additional configuration

            services.AddHttpClient<ExchangeRatesClient>()
                .AddHttpMessageHandler<ApiKeyMessageHandler>()
                .AddTransientHttpErrorPolicy(policy =>
                    policy.WaitAndRetryAsync(new[] {
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5)
                    })
                );

            services.AddTransient<ApiKeyMessageHandler>();
            services.Configure<ExchangeRateApiSettings>(Configuration.GetSection("ExchangeRateApiSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
