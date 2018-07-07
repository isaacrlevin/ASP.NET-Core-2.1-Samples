using System;
using System.Net.Http;
using IHttpClientFactorySample.GitHub;
using IHttpClientFactorySample.Handlers;
using IHttpClientFactorySample.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace IHttpClientFactorySample
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
            // named client
            services.AddHttpClient("github", c =>
            {
                c.BaseAddress = new Uri("https://api.github.com/");
                c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json"); // Github API versioning
                c.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample"); // Github requires a user-agent
            });
            
            // typed client with configuration in constructor
            services.AddHttpClient<GitHubService>();

            services.AddTransient<ValidateHeaderHandler>();

            // named client with "outgoing middleware"
            services.AddHttpClient("externalservice", c =>
            {
                c.BaseAddress = new Uri("https://localhost:5000/"); // assume this is an "external" service which requires an API KEY
            })
            .AddHttpMessageHandler<ValidateHeaderHandler>();
            
            // typed client with retry logic via Polly
            services.AddHttpClient<UnreliableEndpointCallerService>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600)));

            // apply a conditional policy based on the request
            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            var longTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

            services.AddHttpClient("conditionalpolicy")
                .AddPolicyHandler(request => request.Method == HttpMethod.Get ? timeout : longTimeout);
           
            // applying multiple policies
            services.AddHttpClient("multiplepolicies")
                .AddTransientHttpErrorPolicy(p => p.RetryAsync(3))
                .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
 
            // using policies from a regsitry
            var registry = services.AddPolicyRegistry();

            registry.Add("regular", timeout);
            registry.Add("long", longTimeout);
            
            services.AddHttpClient("regulartimeouthandler")
                .AddPolicyHandlerFromRegistry("regular");

            // configuring the inner handler
            services.AddHttpClient("configured-inner-handler")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseDefaultCredentials = true
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
