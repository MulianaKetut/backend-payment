using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentAPI.Configurations;
using PaymentAPI.Contexts;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentAPI
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
            services
                .AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling =
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

            services
                .Configure<JwtConfig>(Configuration.GetSection("JwtConfig"));

            string myConn =
                Configuration.GetConnectionString("DefaultConnection");

            services
                .AddDbContextPool<AppDbContext>(options =>
                    options.UseMySql(myConn, ServerVersion.AutoDetect(myConn)));

            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling =
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services
                .AddSwaggerGen(swagger =>
                {
                    swagger
                        .SwaggerDoc("v1",
                        new OpenApiInfo {
                            Title = "PaymentAPI",
                            Version = "v1",
                            Description =
                                "Authentication and Authorization in ASP.NET 5 with JWT and Swagger"
                        });

                    // To Enable authorization using Swagger (JWT)
                    swagger
                        .AddSecurityDefinition("BearerAuth",
                        new OpenApiSecurityScheme()
                        {
                            Name = "Authorization",
                            Type = SecuritySchemeType.Http,
                            Scheme =
                                JwtBearerDefaults
                                    .AuthenticationScheme
                                    .ToLowerInvariant(),
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description =
                                "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
                        });

                    swagger.OperationFilter<AuthResponsesOperationFilter>();
                    swagger
                        .AddSecurityRequirement(new OpenApiSecurityRequirement {
                            {
                                new OpenApiSecurityScheme {
                                    Reference =
                                        new OpenApiReference {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        }
                                },
                                new string[] { }
                            }
                        });
                });

            services
                .AddCors(options =>
                {
                    options
                        .AddPolicy("Open",
                        builder =>
                            builder
                                .AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod());
                });

            var key =
                Encoding.ASCII.GetBytes(Configuration["JwtConfig:Secret"]);

            var tokenValidationParameters =
                new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero
                };

            services.AddSingleton (tokenValidationParameters);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(jwt =>
                {
                    jwt.SaveToken = true;
                    jwt.TokenValidationParameters = tokenValidationParameters;
                });

            services
                .AddDefaultIdentity<IdentityUser>(options =>
                    options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<AppDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app
                    .UseSwaggerUI(c =>
                        c
                            .SwaggerEndpoint("/swagger/v1/swagger.json",
                            "TodoApp v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }

    internal class AuthResponsesOperationFilter : IOperationFilter
    {
        public void Apply(
            OpenApiOperation operation,
            OperationFilterContext context
        )
        {
            var attributes =
                context
                    .MethodInfo
                    .DeclaringType
                    .GetCustomAttributes(true)
                    .Union(context.MethodInfo.GetCustomAttributes(true));

            if (attributes.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            var authAttributes = attributes.OfType<IAuthorizeData>();

            if (authAttributes.Any())
            {
                operation.Responses["401"] =
                    new OpenApiResponse { Description = "Unauthorized" };

                if (
                    authAttributes
                        .Any(att =>
                            !String.IsNullOrWhiteSpace(att.Roles) ||
                            !String.IsNullOrWhiteSpace(att.Policy))
                )
                {
                    operation.Responses["403"] =
                        new OpenApiResponse { Description = "Forbidden" };
                }

                operation.Security =
                    new List<OpenApiSecurityRequirement> {
                        new OpenApiSecurityRequirement {
                            {
                                new OpenApiSecurityScheme {
                                    Reference =
                                        new OpenApiReference {
                                            Id = "BearerAuth",
                                            Type = ReferenceType.SecurityScheme
                                        }
                                },
                                Array.Empty<string>()
                            }
                        }
                    };
            }
        }
    }
}
