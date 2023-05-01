using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
//using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;
//using Nop.Plugin.Tax.FixedOrByCountryStateZip.Infrastructure.Cache;

namespace Nop.Plugin.Widgets.ColorFilter.Services
{
    /// <summary>
    /// Tax rate service
    /// </summary>
    public class ColorFilterService : IColorFilterService
    {
        private readonly MediaSettings _mediaSettings;
        private readonly IProductService _productService;
        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;
        private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
        private readonly IPictureService _pictureService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IRepository<RelatedProduct> _relatedProductRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        public ColorFilterService(MediaSettings mediaSettings, 
            IProductService productService, 
            IRepository<Picture> pictureRepository, 
            IRepository<ProductPicture> productPictureRepository, 
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository, 
            IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository, 
            IRepository<ProductAttributeValue> productAttributeValueRepository, 
            IPictureService pictureService, 
            ISpecificationAttributeService specificationAttributeService,
            ILocalizationService localizationService,
            IRepository<RelatedProduct> relatedProductRepository,
            IRepository<Product> productRepository,
            IStaticCacheManager staticCacheManager)
        {
            _mediaSettings = mediaSettings;
            _productService = productService;
            _pictureRepository = pictureRepository;
            _productPictureRepository = productPictureRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
            _productAttributeValueRepository = productAttributeValueRepository;
            _pictureService = pictureService;
            _specificationAttributeService = specificationAttributeService;
            _localizationService = localizationService;
            _productRepository= productRepository;  
            _relatedProductRepository= relatedProductRepository;
            _staticCacheManager = staticCacheManager;
        }

        public async Task<IList<PictureModel>> PrepareProductOverviewPicturesModelAsync(ProductOverviewModel product, IList<string> color)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // var productName = product.Name;// await _localizationService.GetLocalizedAsync(product, x => x.Name);
            //If a size has been set in the view, we use it in priority
            var pictureSize = _mediaSettings.ProductThumbPictureSize;
            
            var pictures = (await GetPicturesByProductIdAsync(product.Id, color))
                     .DefaultIfEmpty(null);


            var pictureModels = await pictures
                .SelectAwait(async picture => await preparePictureModelAsync(picture, pictureSize, product.Name))
                .ToListAsync();
            // pictureModels.PictureId = pictures.Where(x => x.Id);

            return pictureModels;
        }

        public virtual async Task<IList<Picture>> GetPicturesByProductIdAsync(int productId, IList<string> colors)
        {
            if (productId == 0)
                return new List<Picture>();
            var pics1 = new List<Picture>();
            if (colors != null && colors.Count > 0)
            {
                var colorString = colors.FirstOrDefault();
                var query1 = from p in _pictureRepository.Table
                             join pp in _productPictureRepository.Table on p.Id equals pp.PictureId
                             join ps in _productSpecificationAttributeRepository.Table on pp.ProductId equals ps.ProductId
                             //on new { productid = pp.ProductId, displayorder = pp.DisplayOrder }
                             // equals new { productid = ps.ProductId, displayorder = ps.DisplayOrder }
                             join sa in _specificationAttributeOptionRepository.Table on ps.SpecificationAttributeOptionId equals sa.Id
                             join pa in _productAttributeValueRepository.Table
                             on new { pictureid = pp.PictureId, name = sa.Name }
                             equals new { pictureid = pa.PictureId, name = pa.Name }
                             orderby ps.SpecificationAttributeOptionId, pp.DisplayOrder, pp.Id
                             where pp.ProductId == productId && (sa.Name.ToLower() == colorString.ToLower())// == specIds //blu
                             select p;

                query1 = query1.Take(1);
                pics1 = await query1.ToListAsync();


                return pics1;
            }


            return new List<Picture>();
        }

        public async Task<PictureModel> preparePictureModelAsync(Picture picture, int pictureSize, string productName)
        {
            if (picture == null)
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
                //"title" attribute
                Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute))
                    ? picture.TitleAttribute
                    : string.Format(await _localizationService.GetResourceAsync("Media.Product.ImageLinkTitleFormat"),
                        productName),
                //"alt" attribute
                AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute))
                    ? picture.AltAttribute
                    : string.Format(await _localizationService.GetResourceAsync("Media.Product.ImageAlternateTextFormat"),
                        productName)
                //PictureId = picture.Id
            };
        }
        public virtual async Task<IList<RelatedProduct>> GetRelatedProductsByProductId1Async(int productId, bool showHidden = false, string color = "")
        {
            if (!string.IsNullOrEmpty(color))
            {
                var query1 = (from rp in _relatedProductRepository.Table
                              join p in _productRepository.Table on rp.ProductId2 equals p.Id
                              join ps in _productSpecificationAttributeRepository.Table on rp.ProductId2 equals ps.ProductId
                              join sp in _specificationAttributeOptionRepository.Table on ps.SpecificationAttributeOptionId equals sp.Id
                              where rp.ProductId1 == productId &&
                             !p.Deleted &&
                             (showHidden || p.Published) && sp.Name.ToLower() == color.ToLower()
                              orderby rp.DisplayOrder, rp.Id
                              select rp).Distinct();
               
                //var relatedProducts1 = await _staticCacheManager.GetAsync(_staticCacheManager.PrepareKeyForDefaultCache(NopCatalogDefaults.RelatedProductsCacheKey, productId, showHidden,color), async () => await query1.ToListAsync());
                var relatedProducts1 = await query1.ToListAsync();
                return relatedProducts1;
            }
            var query = from rp in _relatedProductRepository.Table
                        join p in _productRepository.Table on rp.ProductId2 equals p.Id
                        where rp.ProductId1 == productId &&
                        !p.Deleted &&
                        (showHidden || p.Published)
                        orderby rp.DisplayOrder, rp.Id
                        select rp;

            var relatedProducts = await _staticCacheManager.GetAsync(_staticCacheManager.PrepareKeyForDefaultCache(NopCatalogDefaults.RelatedProductsCacheKey, productId, showHidden), async () => await query.ToListAsync());

            return relatedProducts;
        }
        public virtual async Task<IList<string>> GetColorByPictureId(int productId)
        {
            bool showHidden = false;
            var query = from ps in _productSpecificationAttributeRepository.Table 
                        join p in _productRepository.Table on ps.ProductId equals p.Id
                        join sa in _specificationAttributeOptionRepository.Table on ps.SpecificationAttributeOptionId equals sa.Id
                        where ps.ProductId == productId &&
                        !p.Deleted &&
                        (showHidden || p.Published)
                        orderby ps.DisplayOrder, ps.Id
                        select sa.Name;

            var result = await query.ToListAsync();
            return result;
        }

    }
}