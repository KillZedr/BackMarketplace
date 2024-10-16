using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paymant_Module_NEOXONLINE.Contract.Exeptions;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Application.Payment_DAL.RealisationInterfaces;
using Serilog;
using System.Data.SqlClient;
using ILogger = Serilog.ILogger;

namespace Paymant_Module_NEOXONLINE
{
    public class Startup
    { 
        public static void RegisterDAL(IServiceCollection services)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
          /*  builder.Password = '';
            builder.UserID = '';
            builder.DataSource = 'localhost, 5432';

            builder.InitialCatalog = 'Payment_Modul';*/
            var connectionString = builder.ConnectionString;

            services.AddNpgsql<DbContext>("host=localhost; port=5432; database=Payment_Module; Username=...; Passsword=123321");

            if (!TestConnection(services))
            {
                throw new DbConnectionExeption("Test Db connection failed");
            }
            services.AddScoped<IUnitOfWork>(prov =>
            {
                var context = prov.GetRequiredService<DbContext>();
                return new UnitOfWork(context);
            });
        }

        internal static void AddSerilog(WebApplicationBuilder builder)
        {

            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Month,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            if (builder.Environment.IsDevelopment())
            {
                loggerConfig = loggerConfig.MinimumLevel.Debug();
            }
            else
            {
                loggerConfig = loggerConfig.MinimumLevel.Warning();
            }
            var logger = loggerConfig.CreateLogger();
            builder.Services.AddSingleton<ILogger>(logger);
        }

        private static bool TestConnection(IServiceCollection services)
        {
            var provider =  services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger>();
            var context = provider.GetRequiredService<DbContext>();
            logger.Information("Testing the DB connection....");
            try
            {
                 var createdAnew = context.Database.EnsureCreated();
                if (createdAnew)
                {
                    logger.Information("Successfully created the DB");
                }
                else
                {
                    logger.Information("The DB is already there");
                }
            }
            catch (Exception ex)
            {
                logger.Information("EnsureCreated failed");
                logger.Information(ex.ToString());
                return false;
            }
            return true;
        }
    }
}
