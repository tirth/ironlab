﻿// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Container class implements the IServiceProvider interface. This is used
    /// to pass shared services between different components, for instance the
    /// ContentManager uses it to locate the IGraphicsDeviceService implementation.
    /// </summary>
    public class ServiceContainer : IServiceProvider
    {
        readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();


        /// <summary>
        /// Adds a new service to the collection.
        /// </summary>
        public void AddService<T>(T service)
        {
            _services.Add(typeof(T), service);
        }


        /// <summary>
        /// Looks up the specified service.
        /// </summary>
        public object GetService(Type serviceType)
        {
            object service;

            _services.TryGetValue(serviceType, out service);

            return service;
        }
    }
}
