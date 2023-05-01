using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Nop.Core.Infrastructure;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Web.Framework.Themes;
using Nop.Web.Framework;

namespace Nop.Plugin.Widgets.ColorFilter.ViewEngines
{
    public class ColorViewEngine : IViewLocationExpander
    {
        public string StoreTheme = "DefaultClean";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.AreaName == null)
            {
                if (!context.Values.TryGetValue(StoreTheme, out string? theme))
                    return viewLocations;

                if (context.ViewName == "_ProductBox" &&
                   (context.ControllerName == "Catalog" || context.ControllerName == "Product"))
                {
                    viewLocations = new[] { $"~/Plugins/Widgets.ColorFilter/Views/ProdBoxView.cshtml" }.Concat(viewLocations);
                }
                if (context.ViewName == "_ProductsInGridOrLines" &&
                   (context.    ControllerName == "Catalog" || context.ControllerName == "WidgetsColorFilter"))
                {
                    viewLocations = new[] { $"~/Plugins/Widgets.ColorFilter/Views/ProdInGridOrLinesView.cshtml" }.Concat(viewLocations);
                }

                if (context.ViewName == "ProductTemplate.Simple" &&
                                   (context.ControllerName == "Product" || context.ControllerName == "WidgetsColorFilter"))
                {
                    viewLocations = new[] { $"~/Plugins/Widgets.ColorFilter/Views/ProdTemplateSimpleView.cshtml" }.Concat(viewLocations);
                }

                if (context.ViewName == "_ProductAttributes" &&
                                   (context.ControllerName == "Product" || context.ControllerName == "WidgetsColorFilter"))
                {
                    viewLocations = new[] { $"~/Plugins/Widgets.ColorFilter/Views/ProdAttributeView.cshtml" }.Concat(viewLocations);
                }

            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            //no need to add the themeable view locations at all as the administration should not be themeable anyway
            if (context.AreaName?.Equals(AreaNames.Admin) ?? false)
                return;

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();

            StoreTheme = settingService.GetSettingByKeyAsync("storeinformationsettings.defaultstoretheme", "DefaultClean", storeContext.GetCurrentStore().Id, true).Result;

            // context.Values[THEME_KEY] = EngineContext.Current.Resolve<IThemeContext>().GetWorkingThemeNameAsync().Result;

            context.Values[StoreTheme] = EngineContext.Current.Resolve<IThemeContext>().GetWorkingThemeNameAsync().Result;
        }
    }
}
