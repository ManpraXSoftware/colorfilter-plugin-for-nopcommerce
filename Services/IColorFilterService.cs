using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
//using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;

namespace Nop.Plugin.Widgets.ColorFilter.Services
{
    /// <summary>
    /// Tax rate service interface
    /// </summary>
    public interface IColorFilterService
    {
        Task<IList<PictureModel>> PrepareProductOverviewPicturesModelAsync(ProductOverviewModel product, IList<string> color);
        Task<IList<RelatedProduct>> GetRelatedProductsByProductId1Async(int productId, bool showHidden = false, string color = "");
        Task<IList<string>> GetColorByPictureId(int productId);
    }
}