using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Widgets.ColorFilter.Services;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.ColorFilter.Controllers
{
    public class WidgetsColorFilterController : BasePluginController
    {
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IColorFilterService _colorFilterService;

        public WidgetsColorFilterController(IProductService productService,
            IProductAttributeParser productAttributeParser,
            ISpecificationAttributeService specificationAttributeService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IProductModelFactory productModelFactory,
            IColorFilterService colorFilterService)
        {
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _specificationAttributeService = specificationAttributeService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _productModelFactory = productModelFactory;
            _colorFilterService = colorFilterService;
        }

        [HttpPost]
        public virtual async Task<IActionResult> Related_Products(int productId, IFormCollection form)
        {
            
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return new NullJsonResult();

            var errors = new List<string>();
            var attributeXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, errors);
            var attributeValues = await _productAttributeParser.ParseProductAttributeValuesAsync(attributeXml);
            List<string> color = new List<string>();
            foreach (var i in attributeValues)
            {
                color.Add(i.Name.ToLower());

            }
            if (color != null && color.Count>0)
            {
                string colorString = color.FirstOrDefault().ToString();

                //-Related product Ids with Same color
                var productIds_Color = (await _colorFilterService.GetRelatedProductsByProductId1Async(productId, false,colorString)).Select(x => x.ProductId2).ToArray();

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
                  
                    //concat both model
                    var model = model2.Concat(model1).ToList();


                    return PartialView("~/Plugins/Widgets.ColorFilter/Views/RelatedProductView.cshtml", model);
                }

                return PartialView("~/Plugins/Widgets.ColorFilter/Views/RelatedProductView.cshtml", model2);
            }
            return Content(string.Empty);
        }      
        
    }
}