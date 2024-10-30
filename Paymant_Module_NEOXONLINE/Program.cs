
using Payment.BLL;
using Payment.BLL.PayPalSetting;
using Serilog;

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
            builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection("PayPal"));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            Startup.AddServices(builder);
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


            app.MapControllers();

            app.Run();
        }
    }
}
