
using Microsoft.OpenApi.Models;
using Payment.BLL;
using Serilog;
using System.Reflection;

namespace Paymant_Module_NEOXONLINE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });
            builder.Services.AddHttpClient();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "API Documentation",
                    Description = "API for managing payments, donations, and notifications",
                    Contact = new OpenApiContact
                    {
                        Name = "Support",
                        Email = "support@example.com"
                    }
                });

                // Add comments for XML documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            Startup.AddServices(builder);
            Startup.ConfigureStripe(builder);
            ModuleHead.RegisterModule(builder.Services);

            var app = builder.Build();

            DbInitializer.InitializeDb(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("htmlpage.html"); // ��������� index.html ��� ��������� URL
            });
             

            app.MapControllers();

            app.Run(); 
        }
    }
}
