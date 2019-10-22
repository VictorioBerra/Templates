namespace GraphQLTemplate
{
    using System;
    using Boxed.AspNetCore;
#if CorrelationId
    using CorrelationId;
#endif
#if CORS
    using GraphQLTemplate.Constants;
#endif
    using GraphQL.Server;
    using GraphQL.Server.Ui.Playground;
    using GraphQL.Server.Ui.Voyager;
    using GraphQLTemplate.Schemas;
    using Microsoft.AspNetCore.Builder;
#if HealthCheck
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#endif
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// The main start-up class for the application.
    /// </summary>
    public class Startup : StartupBase
    {
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment hostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration, where key value pair settings are stored. See
        /// http://docs.asp.net/en/latest/fundamentals/configuration.html</param>
        /// <param name="hostEnvironment">The environment the application is running under. This can be Development,
        /// Staging or Production by default. See http://docs.asp.net/en/latest/fundamentals/environments.html</param>
        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            this.configuration = configuration;
            this.hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Configures the services to add to the ASP.NET Core Injection of Control (IoC) container. This method gets
        /// called by the ASP.NET runtime. See
        /// http://blogs.msdn.com/b/webdev/archive/2014/06/17/dependency-injection-in-asp-net-vnext.aspx
        /// </summary>
        public override void ConfigureServices(IServiceCollection services) =>
            services
#if ApplicationInsights
                // Add Azure Application Insights data collection services to the services container.
                .AddApplicationInsightsTelemetry(this.configuration)
#endif
#if CorrelationId
                .AddCorrelationIdFluent()
#endif
                .AddCustomCaching()
                .AddCustomOptions(this.configuration)
                .AddCustomRouting()
#if ResponseCompression
                .AddCustomResponseCompression()
#endif
#if HttpsEverywhere
                .AddCustomStrictTransportSecurity()
#endif
#if HealthCheck
                .AddCustomHealthChecks()
#endif
                .AddHttpContextAccessor()
                .AddMvcCore()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                    .AddAuthorization()
                    .AddJsonFormatters()
                    .AddCustomJsonOptions(this.hostEnvironment)
#if CORS
                    .AddCustomCors()
#endif
                    .AddCustomMvcOptions()
                .Services
                .AddCustomGraphQL(this.hostEnvironment)
#if Authorization
                .AddCustomGraphQLAuthorization()
#endif
                .AddProjectRepositories()
                .AddProjectSchemas();

        /// <summary>
        /// Configures the application and HTTP request pipeline. Configure is called after ConfigureServices is
        /// called by the ASP.NET runtime.
        /// </summary>
        public override void Configure(IApplicationBuilder application) =>
            application
#if CorrelationId
                // Pass a GUID in a X-Correlation-ID HTTP header to set the HttpContext.TraceIdentifier.
                .UseCorrelationId()
#endif
#if ForwardedHeaders
                .UseForwardedHeaders()
#elif HostFiltering
                .UseHostFiltering()
#endif
#if ResponseCompression
                .UseResponseCompression()
#endif
#if CORS
                .UseCors(CorsPolicyName.AllowAny)
#endif
#if HttpsEverywhere
                .UseIf(
                    !this.hostEnvironment.IsDevelopment(),
                    x => x.UseHsts())
#endif
                .UseIf(
                    this.hostEnvironment.IsDevelopment(),
                    x => x.UseDeveloperErrorPages())
#if HealthCheck
                .UseHealthChecks("/status")
                .UseHealthChecks("/status/self", new HealthCheckOptions() { Predicate = _ => false })
#endif
                .UseStaticFilesWithCacheControl()
#if Subscriptions
                .UseWebSockets()
                // Use the GraphQL subscriptions in the specified schema and make them available at /graphql.
                .UseGraphQLWebSockets<MainSchema>()
#endif
                // Use the specified GraphQL schema and make them available at /graphql.
                .UseGraphQL<MainSchema>()
                .UseIf(
                    this.hostEnvironment.IsDevelopment(),
                    x => x
                        // Add the GraphQL Playground UI to try out the GraphQL API at /.
                        .UseGraphQLPlayground(new GraphQLPlaygroundOptions() { Path = "/" })
                        // Add the GraphQL Voyager UI to let you navigate your GraphQL API as a spider graph at /voyager.
                        .UseGraphQLVoyager(new GraphQLVoyagerOptions() { Path = "/voyager" }));
    }
}
