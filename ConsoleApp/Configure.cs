
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;




namespace ConsoleConfigurationTest
{
    internal class Configure
    {
        static async Task Main(string[] args)
        {
            
            var builder = Host.CreateApplicationBuilder();

            //builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("Application"));
            // or :
            builder.Services.AddOptions<ApplicationOptions>()
                .Bind(builder.Configuration.GetSection("Application"));

            builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    
    .ValidateOnStart();

            builder.Services
                .AddOptions<WorkerOptions>()
                .Bind(builder.Configuration.GetSection("Worker"))
              
                .ValidateOnStart();

            builder.Services
                .AddOptions<FeatureOptions>()
                .Bind(builder.Configuration.GetSection("Features"));

            builder.Configuration
    .AddJsonFile(
        "appsettings.json",
        optional: false,
        reloadOnChange: true);

            var host = builder.Build();

            await host.RunAsync();

          
            var c = new configUser();
        }
    }

    public class configUser
    {
        private readonly IOptionsMonitor<ApplicationOptions> _optionsMonitor;
        private readonly IConfiguration _conf;
        private readonly IOptions<WorkerOptions> _options;

        public configUser()
        {
        }

        public configUser(IOptionsMonitor<ApplicationOptions> optionsMonitor, IOptions<WorkerOptions> options, IConfiguration conf)
        {
            _optionsMonitor = optionsMonitor;
            _options = options;
            _conf = conf;
        }
        void useConf()
        {
          
           var s= _optionsMonitor.CurrentValue ;
           var t= _options.Value ;
            
        }
    }

}
