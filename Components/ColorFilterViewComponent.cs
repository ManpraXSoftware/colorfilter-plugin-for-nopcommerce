
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Plugin.Widgets.ColorFilter.Components
{
    public class ColorFilterViewComponent : NopViewComponent
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
        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAtrributeOptionRepository;
        private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
        private readonly IPictureService _pictureService;
        private readonly ISpecificationAttributeService _specificationAttributeService;      

        #endregion

        #region Ctor

        public ColorFilterViewComponent(ILogger logger,
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
            ISpecificationAttributeService specificationAttributeService)
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
            if (!widgetZone.Equals("category_details_filter"))
                return Content(string.Empty);

            var summaryModel = additionalData as CatalogProductsModel;
            if (summaryModel is null)
                return Content(string.Empty);
         
            var color = new List<string>();
            
            var specificationAttributeFilterModel = summaryModel.SpecificationFilter.Attributes.FirstOrDefault(x => x.Name == "Color");
            if (specificationAttributeFilterModel != null)
            {
                foreach (var specificationAttributeValue in specificationAttributeFilterModel.Values) 
                {                    
                    if (specificationAttributeValue.Selected == true)
                    {
                        color.Add(specificationAttributeValue.Name.ToString());
                    }
                }

                if (color != null && color.Count != 0)
                {
                    ViewBag.colorvalue = color[0].ToString();
                    foreach (var product in summaryModel.Products)
                    {
                        //Prepare PictureModel for Each Product in the Catalog details
                        var pictureModel = await PrepareProductOverviewPicturesModelAsync(product, color);
                        if(pictureModel.FirstOrDefault() != null)
                        {
                            product.PictureModels = pictureModel;
                        }
    
                    }

                }
            }

            return View("~/Plugins/Widgets.ColorFilter/Views/PublicInfo.cshtml", summaryModel);

        }

        protected virtual async Task<IList<PictureModel>> PrepareProductOverviewPicturesModelAsync(ProductOverviewModel product, IList<string> colors)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //If a size has been set in the view, we use it in priority
            var pictureSize = _mediaSettings.ProductThumbPictureSize;

              //return cachedPictures;
                var pictures = (await GetPicturesByProductIdAsync(product.Id, colors))
                         .DefaultIfEmpty(null);


                var pictureModels = await pictures
                    .SelectAwait(async picture => await preparePictureModelAsync(picture, pictureSize, product.Name))
                    .ToListAsync();
            
            return pictureModels;

        }

        //Prepare PictureModel for each product in Catalog details
        public async Task<PictureModel> preparePictureModelAsync(Picture picture, int pictureSize, string productName)
        {
            if(picture == null)
            {
                return null;
            }

            //we use the Task.WhenAll method to control that both image thumbs was created in same time.
            //without this method, sometimes there were situations when one of the pictures was not generated on time
            //this section of code requires detailed analysis in the future
            var picResultTasks = await Task.WhenAll(_pictureService.GetPictureUrlAsync(picture, pictureSize), _pictureService.GetPictureUrlAsync(picture));

            var (imageUrl, _) = picResultTasks[0];
            var (fullSizeImageUrl, _) = picResultTasks[1];

            return new PictureModel
            {
                ImageUrl = imageUrl,
                FullSizeImageUrl = fullSizeImageUrl,
                Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute))
                    ? picture.TitleAttribute
                    : string.Format(await _localizationService.GetResourceAsync("Media.Product.ImageLinkTitleFormat"),
                        productName),
                AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute))
                    ? picture.AltAttribute
                    : string.Format(await _localizationService.GetResourceAsync("Media.Product.ImageAlternateTextFormat"),
                        productName)
            };
        }

        public virtual async Task<IList<Picture>> GetPicturesByProductIdAsync(int productId, IList<string> colors)
        {
            if (productId == 0)
                return new List<Picture>();
            var pics1 = new List<Picture>();
            if (colors != null)
            {
               
                    var query1 = from p in _pictureRepository.Table
                                 join pp in _productPictureRepository.Table on p.Id equals pp.PictureId
                                 join ps in _productSpecificationAttributeRepository.Table on pp.ProductId equals ps.ProductId
                                 join sa in _specificationAtrributeOptionRepository.Table on ps.SpecificationAttributeOptionId equals sa.Id
                                 join pa in _productAttributeValueRepository.Table
                                 on new { pictureid = pp.PictureId, name = sa.Name }
                                 equals new { pictureid = pa.PictureId, name = pa.Name }
                                 orderby ps.SpecificationAttributeOptionId, pp.DisplayOrder, pp.Id
                                 where pp.ProductId == productId && colors.Contains(sa.Name)
                                 select p;

                query1 = query1.Take(1);
                pics1 = await query1.ToListAsync();
                    
                    
                return pics1;
            }


            return new List<Picture>();
        }


        #endregion
    }
}