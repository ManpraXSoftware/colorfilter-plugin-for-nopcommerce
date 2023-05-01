using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Widgets.ColorFilter.Services;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Plugin.Widgets.ColorFilter.Components
{
    public class RelatedProductsViewComponent : NopViewComponent
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
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IColorFilterService _colorFilterService;

        // private readonly IPictu _pictureService;

        #endregion

        #region Ctor

        public RelatedProductsViewComponent(ILogger logger,
            IWidgetPluginManager widgetPluginManager,
            IWorkContext workContext,
            ILocalizationService localizationService,
            IStaticCacheManager staticCacheManager,
            MediaSettings mediaSettings,
            IWebHelper webHelper,
            CatalogSettings catalogSettings,
            IProductService productService,
            IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            IRepository<SpecificationAttributeOption> specificationAtrributeOptionRepository,
            IRepository<ProductAttributeValue> productAttributeValueRepository,
            IPictureService pictureService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IProductModelFactory productModelFactory,
            IColorFilterService colorFilterService)
        {
            _logger = logger;
            _widgetPluginManager = widgetPluginManager;
            _workContext = workContext;
            _localizationService = localizationService;
            _staticCacheManager = staticCacheManager;
            _mediaSettings = mediaSettings;
            _webHelper = webHelper;
            _catalogSettings = catalogSettings;
            _productService = productService;
            _pictureRepository = pictureRepository;
            _productPictureRepository = productPictureRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _specificationAtrributeOptionRepository = specificationAtrributeOptionRepository;
            _productAttributeValueRepository = productAttributeValueRepository;
            _pictureService = pictureService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _productModelFactory = productModelFactory;
            _colorFilterService = colorFilterService;
        }

        #endregion

        #region Methods

       
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {

            if (!widgetZone.Equals("RelatedProductsViewComponent_In_ProductDetails"))
                return Content(string.Empty);

            var summaryModel = additionalData as ProductDetailsModel;
            if (summaryModel is null)
                return Content(string.Empty);

            var productId = summaryModel.Id;
            var color = await _colorFilterService.GetColorByPictureId(productId);

            if (color != null && color.Count > 0)
            {
                string colorString = color.FirstOrDefault().ToString();

                //-Related product Ids with Same color
                var productIds_Color = (await _colorFilterService.GetRelatedProductsByProductId1Async(productId, false, colorString)).Select(x => x.ProductId2).ToArray();

                //load products for productIds_Color
                var products_Color = await (await _productService.GetProductsByIdsAsync(productIds_Color))
                //ACL and store mapping
                .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
                //availability dates
                .Where(p => _productService.ProductIsAvailable(p))
                //visible individually
                .Where(p => p.VisibleIndividually).ToListAsync();


                //ProductOverViewModel for Related Products of Same color
                var model2 = (await _productModelFactory.PrepareProductOverviewModelsAsync(products_Color)).ToList();
                foreach (var i in model2)
                {
                    i.PictureModels = await _colorFilterService.PrepareProductOverviewPicturesModelAsync(i, color);
                }


                //all related product Ids
                var productIds = (await _colorFilterService.GetRelatedProductsByProductId1Async(productId, false)).Select(x => x.ProductId2).ToArray();


                //shows values of productIds which is not in the second array productIdss
                var ids = (productIds.Except(productIds_Color)).ToArray();


                //load products
                var products = await (await _productService.GetProductsByIdsAsync(ids))
                //ACL and store mapping
                .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
                //availability dates
                .Where(p => _productService.ProductIsAvailable(p))
                //visible individually
                .Where(p => p.VisibleIndividually).ToListAsync();
                if (products.Any())
                {
                    //ProductOverViewModel for all Related Products
                    var model1 = (await _productModelFactory.PrepareProductOverviewModelsAsync(products)).ToList();
                    
                    //concat both models
                    var model = model2.Concat(model1).ToList();


                    return View("~/Plugins/Widgets.ColorFilter/Views/RelatedProductView.cshtml", model);
                }

                return View("~/Plugins/Widgets.ColorFilter/Views/RelatedProductView.cshtml", model2);
            }
                       
            return Content(string.Empty);

        }

        protected virtual async Task<IList<PictureModel>> PreparePictureModel(ProductOverviewModel product, IList<PictureModel> pictureModels)
        {            
            var color = await _colorFilterService.GetColorByPictureId(product.Id);
            var pictureModel = await _colorFilterService.PrepareProductOverviewPicturesModelAsync(product, color);
            return pictureModel;
        }    
         #endregion
    }
}