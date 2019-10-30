using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLayersApp.Helpers
{
    public class IoC
    {
        static IoC innerInstance;
        static IServiceProvider innerProvider;
        static object _lock;
        private IoC()
        {

        }
        public static IoC Container
        {
            get
            {
                if (innerInstance is null)
                {
                    lock (_lock = new object())
                    {
                        innerInstance = new IoC();
                    }
                }
                return innerInstance;
            }
        }

        public static IServiceProvider ServiceProvider
        {
            get
            {
                return innerProvider ?? (innerProvider = Container.Services.BuildServiceProvider());
            }
        }

        public ServiceCollection Services { get; } = new ServiceCollection();
        public void RegisterServices(Action<ServiceCollection> registrations)
        {
            registrations(Services);
            innerProvider = Container.Services.BuildServiceProvider();
        }
    }
}
