using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public interface IPlugin
    {
        string Name { get; }

        void Execute();
    }

    public class EmailPlugin : IPlugin
    {
        public string Name => "Email Plugin";

        public void Execute()
        {
            Console.WriteLine("Sending Email...");
        }
    }


  public main(string[] args) {
            string folder = "Plugins";

            foreach (var file in Directory.GetFiles(folder, "*.dll"))
            {
                Assembly assembly = Assembly.LoadFrom(file);

                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type)
                        && !type.IsInterface)
                    {
                        var plugin =
                            (IPlugin)Activator.CreateInstance(type);

                        Console.WriteLine(plugin.Name);

                        plugin.Execute();
                    }
                }
            }

        }
    }
}
