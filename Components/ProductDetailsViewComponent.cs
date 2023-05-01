using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using LinqToDB.Extensions;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
//using Nop.Plugin.Widgets.ColorFilter.Models;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Plugin.Widgets.ColorFilter.Components
{
    public class ProductDetailsViewComponent : NopViewComponent
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IWidgetPluginManager _widgetPluginManager;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly MediaSettings _mediaSettings;
        private readonly IWebHelper _webHelper;
        private readonly CatalogSettings _catalogSettings;
        private readonly IProductService _productService;
        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAtrributeOptionRepository;
        private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
        private readonly IPictureService _pictureService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IProductAttributeService _productAttributeService;

        // private readonly IPictu _pictureService;

        #endregion

        #region Ctor

        public ProductDetailsViewComponent(ILogger logger,
            IWidgetPluginManager widgetPluginManager,
            IWorkContext workContext,
            ILocalizationService localizationService,
            IStaticCacheManager staticCacheManager,
            MediaSettings mediaSettings,
            IWebHelper webHelper,
            CatalogSettings catalogSettings,
            IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            IRepository<SpecificationAttributeOption> specificationAtrributeOptionRepository,
            IRepository<ProductAttributeValue> productAttributeValueRepository,
            IPictureService pictureService,
            ISpecificationAttributeService specificationAttributeService,
            IProductAttributeService productAttributeService)
        {
            _logger = logger;
            _widgetPluginManager = widgetPluginManager;
            _workContext = workContext;
            _localizationService = localizationService;
            _staticCacheManager = staticCacheManager;
            _mediaSettings = mediaSettings;
            _webHelper = webHelper;
            _catalogSettings = catalogSettings;
            _pictureRepository = pictureRepository;
            _productPictureRepository = productPictureRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _specificationAtrributeOptionRepository = specificationAtrributeOptionRepository;
            _productAttributeValueRepository = productAttributeValueRepository;
            _pictureService = pictureService;
            _specificationAttributeService = specificationAttributeService;
            _productAttributeService = productAttributeService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke the widget view component
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="additionalData">Additional parameters</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var routeData = Url.ActionContext.RouteData;
           
            string specs = "";
            string color = "";
            var model = additionalData as ProductDetailsModel;
            if (Request.Query.ContainsKey("specs"))
            {
                // only now actually retrieve the value
                specs = Request.Query["specs"];
            }


            int productId = Convert.ToInt32(model.Id);
            
            

            ProductDetailsModel.ProductAttributeValueModel attributeValueModel = new ProductDetailsModel.ProductAttributeValueModel();
            ProductDetailsModel.ProductAttributeModel attributeModel = new ProductDetailsModel.ProductAttributeModel();
            ProductDetailsModel.ProductAttributeModel item = model.ProductAttributes.FirstOrDefault(x => x.Name.ToLower() == "color");


            if (!string.IsNullOrEmpty(specs))
            {

                SpecificationAttributeOption sao = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(Convert.ToInt32(specs));
                color = sao.Name;

            }
            //Checking if color is not null or empty then get product image based on color else
            //    default image
            if (item != null && item.Values.Where(x => x.Name.ToLower() == color.ToLower()).Count() > 0)
            {
                if (!string.IsNullOrEmpty(color) && color != "no-color")
                {
                    attributeModel = item;

                    foreach (ProductDetailsModel.ProductAttributeValueModel atrrVal in item.Values)
                    {
                        if (atrrVal != null)
                        {
                            attributeValueModel = atrrVal;
                            if (attributeValueModel.Name.ToLower() == color.ToLower())
                            {
                                attributeValueModel.IsPreSelected = true;
                                attributeModel.TextPrompt = attributeValueModel.Name;
                            }
                            else
                                attributeValueModel.IsPreSelected = false;
                        }

                    }
                    IList<ProductAttributeMapping> productAttributeMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(productId);
                    foreach (var attitem in attributeModel.Values)
                    {
                        if (attitem.Name.ToLower() == color.ToLower())
                        {
                            foreach (var attMapitem in productAttributeMappings)
                            {
                                if (attMapitem.Id == item.Id)
                                {
                                    model.DefaultPictureModel.ImageUrl = await _pictureService.GetPictureUrlAsync(attitem.PictureId);
                                }
                            }
                        }
                    }

                }
            }

            return View("~/Plugins/Widgets.ColorFilter/Views/ProductDetailsView.cshtml", model);

        }
        #endregion
    }
}