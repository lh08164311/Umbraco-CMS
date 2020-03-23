﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using ClientDependency.Core.CompositeFiles.Providers;
using ClientDependency.Core.Config;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Hosting;
using Umbraco.Core.Strings;
using Umbraco.Core.IO;
using Umbraco.Web.Install;
using Umbraco.Web.JavaScript;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Constants = Umbraco.Core.Constants;
using Current = Umbraco.Web.Composing.Current;

namespace Umbraco.Web.Runtime
{
    public sealed class WebInitialComponent : IComponent
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly SurfaceControllerTypeCollection _surfaceControllerTypes;
        private readonly UmbracoApiControllerTypeCollection _apiControllerTypes;
        private readonly IHostingSettings _hostingSettings;
        private readonly IGlobalSettings _globalSettings;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IIOHelper _ioHelper;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IRuntimeSettings _settings;

        public WebInitialComponent(
            IUmbracoContextAccessor umbracoContextAccessor,
            SurfaceControllerTypeCollection surfaceControllerTypes,
            UmbracoApiControllerTypeCollection apiControllerTypes,
            IHostingSettings hostingSettings,
            IGlobalSettings globalSettings,
            IHostingEnvironment hostingEnvironment,
            IIOHelper ioHelper,
            IShortStringHelper shortStringHelper,
            IRuntimeSettings settings)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _surfaceControllerTypes = surfaceControllerTypes;
            _apiControllerTypes = apiControllerTypes;
            _hostingSettings = hostingSettings;
            _globalSettings = globalSettings;
            _hostingEnvironment = hostingEnvironment;
            _ioHelper = ioHelper;
            _shortStringHelper = shortStringHelper;
            _settings = settings;
        }

        public void Initialize()
        {
            // setup mvc and webapi services
            SetupMvcAndWebApi();

            // When using a non-web runtime and this component is loaded ClientDependency explodes because it'll
            // want to access HttpContext.Current, which doesn't exist
            if (_hostingEnvironment.IsHosted)
            {
                ConfigureClientDependency();
            }

            // Disable the X-AspNetMvc-Version HTTP Header
            MvcHandler.DisableMvcResponseHeader = true;

            // wrap view engines in the profiling engine
            WrapViewEngines(ViewEngines.Engines);

            // add global filters
            ConfigureGlobalFilters();

            // set routes
            CreateRoutes(_umbracoContextAccessor, _globalSettings, _shortStringHelper, _surfaceControllerTypes, _apiControllerTypes, _ioHelper);
        }

        public void Terminate()
        { }

        private static void ConfigureGlobalFilters()
        {
            GlobalFilters.Filters.Add(new EnsurePartialViewMacroViewContextFilterAttribute());
        }

        // internal for tests
        internal static void WrapViewEngines(IList<IViewEngine> viewEngines)
        {
            if (viewEngines == null || viewEngines.Count == 0) return;

            var originalEngines = viewEngines.ToList();
            viewEngines.Clear();
            foreach (var engine in originalEngines)
            {
                var wrappedEngine = engine is ProfilingViewEngine ? engine : new ProfilingViewEngine(engine);
                viewEngines.Add(wrappedEngine);
            }
        }

        private void SetupMvcAndWebApi()
        {
            //don't output the MVC version header (security)
            MvcHandler.DisableMvcResponseHeader = true;

            // set master controller factory
            var controllerFactory = new MasterControllerFactory(() => Current.FilteredControllerFactories);
            ControllerBuilder.Current.SetControllerFactory(controllerFactory);

            // set the render & plugin view engines
            ViewEngines.Engines.Add(new RenderViewEngine(_ioHelper));
            ViewEngines.Engines.Add(new PluginViewEngine());

            //set model binder
            ModelBinderProviders.BinderProviders.Add(ContentModelBinder.Instance); // is a provider

            ////add the profiling action filter
            //GlobalFilters.Filters.Add(new ProfilingActionFilter());

            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerSelector),
                new NamespaceHttpControllerSelector(GlobalConfiguration.Configuration));
        }

        private void ConfigureClientDependency()
        {
            // Backwards compatibility - set the path and URL type for ClientDependency 1.5.1 [LK]
            XmlFileMapper.FileMapDefaultFolder = Core.Constants.SystemDirectories.TempData.EnsureEndsWith('/') + "ClientDependency";
            BaseCompositeFileProcessingProvider.UrlTypeDefault = CompositeUrlType.Base64QueryStrings;

            // Now we need to detect if we are running 'Umbraco.Core.LocalTempStorage' as EnvironmentTemp and in that case we want to change the CDF file
            // location to be there
            if (_hostingSettings.LocalTempStorageLocation == LocalTempStorage.EnvironmentTemp)
            {
                var cachePath = _hostingEnvironment.LocalTempPath;

                //set the file map and composite file default location to the %temp% location
                BaseCompositeFileProcessingProvider.CompositeFilePathDefaultFolder
                    = XmlFileMapper.FileMapDefaultFolder
                    = Path.Combine(cachePath, "ClientDependency");
            }

            if (_settings.MaxQueryStringLength.HasValue || _settings.MaxRequestLength.HasValue)
            {
                //set the max url length for CDF to be the smallest of the max query length, max request length
                ClientDependency.Core.CompositeFiles.CompositeDependencyHandler.MaxHandlerUrlLength = Math.Min(_settings.MaxQueryStringLength.GetValueOrDefault(), _settings.MaxRequestLength.GetValueOrDefault());
            }

            //Register a custom renderer - used to process property editor dependencies
            var renderer = new DependencyPathRenderer();
            renderer.Initialize("Umbraco.DependencyPathRenderer", new NameValueCollection
            {
                { "compositeFileHandlerPath", ClientDependencySettings.Instance.CompositeFileHandlerPath }
            });

            ClientDependencySettings.Instance.MvcRendererCollection.Add(renderer);
        }

        // internal for tests
        internal static void CreateRoutes(
            IUmbracoContextAccessor umbracoContextAccessor,
            IGlobalSettings globalSettings,
            IShortStringHelper shortStringHelper,
            SurfaceControllerTypeCollection surfaceControllerTypes,
            UmbracoApiControllerTypeCollection apiControllerTypes,
            IIOHelper ioHelper)
        {
            var umbracoPath = ioHelper.GetUmbracoMvcArea();

            // create the front-end route
            var defaultRoute = RouteTable.Routes.MapRoute(
                "Umbraco_default",
                umbracoPath + "/RenderMvc/{action}/{id}",
                new { controller = "RenderMvc", action = "Index", id = UrlParameter.Optional }
            );
            defaultRoute.RouteHandler = new RenderRouteHandler(umbracoContextAccessor, ControllerBuilder.Current.GetControllerFactory(), shortStringHelper);

            // register no content route
            RouteNoContentController(umbracoPath);

            // register install routes
            RouteTable.Routes.RegisterArea<UmbracoInstallArea>();

            // register all back office routes
            RouteTable.Routes.RegisterArea(new BackOfficeArea(ioHelper));

            // plugin controllers must come first because the next route will catch many things
            RoutePluginControllers(globalSettings, surfaceControllerTypes, apiControllerTypes, ioHelper);
        }

        private static void RouteNoContentController(string umbracoPath)
        {
            RouteTable.Routes.MapRoute(
                Constants.Web.NoContentRouteName,
                umbracoPath + "/UmbNoContent",
                new { controller = "RenderNoContent", action = "Index" });
        }

        private static void RoutePluginControllers(
            IGlobalSettings globalSettings,
            SurfaceControllerTypeCollection surfaceControllerTypes,
            UmbracoApiControllerTypeCollection apiControllerTypes,
            IIOHelper ioHelper)
        {
            var umbracoPath = ioHelper.GetUmbracoMvcArea();

            // need to find the plugin controllers and route them
            var pluginControllers = surfaceControllerTypes.Concat(apiControllerTypes).ToArray();

            // local controllers do not contain the attribute
            var localControllers = pluginControllers.Where(x => PluginController.GetMetadata(x).AreaName.IsNullOrWhiteSpace());
            foreach (var s in localControllers)
            {
                if (TypeHelper.IsTypeAssignableFrom<SurfaceController>(s))
                    RouteLocalSurfaceController(s, umbracoPath);
                else if (TypeHelper.IsTypeAssignableFrom<UmbracoApiController>(s))
                    RouteLocalApiController(s, umbracoPath);
            }

            // get the plugin controllers that are unique to each area (group by)
            var pluginSurfaceControlleres = pluginControllers.Where(x => PluginController.GetMetadata(x).AreaName.IsNullOrWhiteSpace() == false);
            var groupedAreas = pluginSurfaceControlleres.GroupBy(controller => PluginController.GetMetadata(controller).AreaName);
            // loop through each area defined amongst the controllers
            foreach (var g in groupedAreas)
            {
                // create & register an area for the controllers (this will throw an exception if all controllers are not in the same area)
                var pluginControllerArea = new PluginControllerArea(globalSettings, ioHelper, g.Select(PluginController.GetMetadata));
                RouteTable.Routes.RegisterArea(pluginControllerArea);
            }
        }

        private static void RouteLocalApiController(Type controller, string umbracoPath)
        {
            var meta = PluginController.GetMetadata(controller);
            var url = umbracoPath + (meta.IsBackOffice ? "/BackOffice" : "") + "/Api/" + meta.ControllerName + "/{action}/{id}";
            var route = RouteTable.Routes.MapHttpRoute(
                $"umbraco-api-{meta.ControllerName}",
                url, // url to match
                new { controller = meta.ControllerName, id = UrlParameter.Optional },
                new[] { meta.ControllerNamespace });
            if (route.DataTokens == null) // web api routes don't set the data tokens object
                route.DataTokens = new RouteValueDictionary();
            route.DataTokens.Add(Core.Constants.Web.UmbracoDataToken, "api"); //ensure the umbraco token is set
        }

        private static void RouteLocalSurfaceController(Type controller, string umbracoPath)
        {
            var meta = PluginController.GetMetadata(controller);
            var url = umbracoPath + "/Surface/" + meta.ControllerName + "/{action}/{id}";
            var route = RouteTable.Routes.MapRoute(
                $"umbraco-surface-{meta.ControllerName}",
                url, // url to match
                new { controller = meta.ControllerName, action = "Index", id = UrlParameter.Optional },
                new[] { meta.ControllerNamespace }); // look in this namespace to create the controller
            route.DataTokens.Add(Core.Constants.Web.UmbracoDataToken, "surface"); // ensure the umbraco token is set
            route.DataTokens.Add("UseNamespaceFallback", false); // don't look anywhere else except this namespace!
            // make it use our custom/special SurfaceMvcHandler
            route.RouteHandler = new SurfaceRouteHandler();
        }
    }
}