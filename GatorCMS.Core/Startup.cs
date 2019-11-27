using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GatorCMS.Core.Connectors.MongoDB;
using GatorCMS.Core.Models;
using GatorCMS.Core.Services.GatorService;
using GatorCMS.Core.Wrappers.DBSettings;
using GraphiQl;
using GraphQL;
using GraphQL.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace GatorCMS.Core {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {

            //services.Configure<IISServerOptions>(options =>
            //{
            //    options.AllowSynchronousIO = false;
            //});

            services.Configure<GatorDBSettings> (Configuration.GetSection (nameof (GatorDBSettings)));

            services.AddSingleton<IGatorDBSettings> (sp => sp.GetRequiredService<IOptions<GatorDBSettings>> ().Value);

            services.AddSwaggerGen (c => {
                c.SwaggerDoc ("v1", new OpenApiInfo {
                    Version = "v1",
                        Title = "GatorCMS",
                        Description = "An *epic* headless CMS. Name change pending...",
                });
            });

            services.AddSingleton<IGatorService, GatorService> ();
            services.AddSingleton<IMongoDBConnector, MongoDBConnector> ();
            services.AddSingleton<IGatorDBSettings, GatorDBSettings> ();

            // graphql
            services.AddTransient<IDependencyResolver>(servicesProvider =>
            {
                return new FuncDependencyResolver(servicesProvider.GetService);
            });
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
            services.AddTransient<QueryType>();
            services.AddTransient<LemonSchema>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization ();

            app.UseSwagger();
            app.UseSwaggerUI (c => {
                c.SwaggerEndpoint ("/swagger/v1/swagger.json", "GatorCMS V1");
                c.RoutePrefix = string.Empty;
            });

            // graphql
            app.UseGraphiQl();
            app.UseGraphQLMiddleware<LemonSchema>();
            app.UseFileServer(new FileServerOptions
            {
                RequestPath = "/playground",
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "playground"
                    )
                )
            });

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }
}
