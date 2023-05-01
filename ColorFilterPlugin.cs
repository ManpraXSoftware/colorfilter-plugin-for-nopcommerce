using Nop.Plugin.Widgets.ColorFilter.Components;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.ColorFilter
{
    public class ColorFilterPlugin : BasePlugin, IWidgetPlugin
    {
        public bool HideInWidgetList => false;

        public Type GetWidgetViewComponent(string widgetZone)
        {
            if (widgetZone is null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals("category_details_filter"))
            {
                return typeof(ColorFilterViewComponent);
            }

            if (widgetZone.Equals("product_details_filter"))
            {
                return typeof(ProductDetailsViewComponent);
            }
            if (widgetZone.Equals("RelatedProductsViewComponent_In_ProductDetails"))
            {

                return typeof(RelatedProductsViewComponent);
            }
            return typeof(ColorFilterViewComponent);
        }

        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {

                "category_details_filter",
                "product_details_filter",
                "RelatedProductsViewComponent_In_ProductDetails"
            });
        }

        public override async Task InstallAsync()
        {
            await base.InstallAsync();
        }

        public override Task PreparePluginToUninstallAsync()
        {
            throw new NotImplementedException();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }
    }
}