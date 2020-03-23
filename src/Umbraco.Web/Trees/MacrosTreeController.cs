﻿using System.Linq;
using System.Net.Http.Formatting;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi.Filters;
using Umbraco.Web.Actions;
using Umbraco.Web.Routing;
using Constants = Umbraco.Core.Constants;

namespace Umbraco.Web.Trees
{
    [UmbracoTreeAuthorize(Constants.Trees.Macros)]
    [Tree(Constants.Applications.Settings, Constants.Trees.Macros, TreeTitle = "Macros", SortOrder = 4, TreeGroup = Constants.Trees.Groups.Settings)]
    [PluginController("UmbracoTrees")]
    [CoreTree]
    public class MacrosTreeController : TreeController
    {
        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;

        public MacrosTreeController(
            IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger logger,
            IRuntimeState runtimeState,
            UmbracoMapper umbracoMapper,
            IPublishedUrlProvider publishedUrlProvider,
            IMenuItemCollectionFactory menuItemCollectionFactory)
            : base(globalSettings, umbracoContextAccessor, sqlContext, services, appCaches, logger, runtimeState, umbracoMapper, publishedUrlProvider)
        {
            _menuItemCollectionFactory = menuItemCollectionFactory;
        }

        protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
        {
            var root = base.CreateRootNode(queryStrings);
            //check if there are any macros
            root.HasChildren = Services.MacroService.GetAll().Any();
            return root;
        }

        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            var nodes = new TreeNodeCollection();

            if (id == Constants.System.RootString)
            {
                foreach (var macro in Services.MacroService.GetAll().OrderBy(m => m.Name))
                {
                    nodes.Add(CreateTreeNode(
                        macro.Id.ToString(),
                        id,
                        queryStrings,
                        macro.Name,
                        Constants.Icons.Macro,
                        false));
                }
            }

            return nodes;
        }

        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            var menu = _menuItemCollectionFactory.Create();

            if (id == Constants.System.RootString)
            {
                //Create the normal create action
                menu.Items.Add<ActionNew>(Services.TextService);

                //refresh action
                menu.Items.Add(new RefreshNode(Services.TextService, true));

                return menu;
            }

            var macro = Services.MacroService.GetById(int.Parse(id));
            if (macro == null) return menu;

            //add delete option for all macros
            menu.Items.Add<ActionDelete>(Services.TextService, opensDialog: true);

            return menu;
        }
    }
}